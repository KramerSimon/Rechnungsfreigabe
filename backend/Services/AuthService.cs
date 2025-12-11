using Microsoft.IdentityModel.Tokens;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace RechnungsfreigabeAPI.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest);
    Task<UserDto?> GetCurrentUserAsync(string token);
    Task<bool> ValidateTokenAsync(string token);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string username, string newPassword);
    string GenerateToken(User user, string[] permissions);
}

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public AuthService(IUserService userService, IPasswordService passwordService, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _userService = userService;
        _passwordService = passwordService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
    {
        try
        {
            var user = await _userService.GetUserByUsernameAsync(loginRequest.Username);
            
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login attempt failed for username: {Username} - User not found or inactive", loginRequest.Username);
                return null;
            }

            // Check if user is locked out
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt failed for username: {Username} - Account locked until {LockedUntil}", 
                    loginRequest.Username, user.LockedUntil.Value);
                return null;
            }

            // Verify password
            if (!_passwordService.VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                // Increment failed login attempts
                await _userService.IncrementFailedLoginAttemptsAsync(user.Id);
                
                // Check if we should lock the account
                if (user.FailedLoginAttempts + 1 >= MaxFailedAttempts)
                {
                    await _userService.LockUserAccountAsync(user.Id, DateTime.UtcNow.Add(LockoutDuration));
                    _logger.LogWarning("Account locked for username: {Username} due to too many failed attempts", loginRequest.Username);
                }
                
                _logger.LogWarning("Login attempt failed for username: {Username} - Invalid password", loginRequest.Username);
                return null;
            }

            // Reset failed login attempts on successful login
            await _userService.ResetFailedLoginAttemptsAsync(user.Id);

            // Get user permissions from roles
            var permissions = await _userService.GetUserPermissionsAsync(user.Id);
            
            // Generate JWT token
            var token = GenerateToken(user, permissions);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description,
                    Permissions = JsonSerializer.Deserialize<string[]>(ur.Role.Permissions) ?? Array.Empty<string>()
                }).ToArray()
            };

            return new LoginResponseDto
            {
                Token = token,
                User = userDto,
                Permissions = permissions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", loginRequest.Username);
            return null;
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync(string token)
    {
        try
        {
            var principal = ValidateJwtToken(token);
            if (principal == null) return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return null;

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user from token");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var principal = ValidateJwtToken(token);
            return principal != null;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateToken(User user, string[] permissions)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiration = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
        };

        // Add permission claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiration),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? ValidateJwtToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey!);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userService.GetUserEntityByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            // Verify current password
            if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed for user {UserId} - Invalid current password", userId);
                return false;
            }

            // Validate new password
            if (!_passwordService.IsPasswordValid(newPassword))
            {
                _logger.LogWarning("Password change failed for user {UserId} - New password doesn't meet requirements", userId);
                return false;
            }

            // Hash new password and update user
            var newPasswordHash = _passwordService.HashPassword(newPassword);
            await _userService.UpdatePasswordAsync(userId, newPasswordHash);

            _logger.LogInformation("Password successfully changed for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string username, string newPassword)
    {
        try
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null || !user.IsActive)
                return false;

            // Validate new password
            if (!_passwordService.IsPasswordValid(newPassword))
            {
                _logger.LogWarning("Password reset failed for user {Username} - New password doesn't meet requirements", username);
                return false;
            }

            // Hash new password and update user
            var newPasswordHash = _passwordService.HashPassword(newPassword);
            await _userService.UpdatePasswordAsync(user.Id, newPasswordHash);

            // Reset failed login attempts and unlock account
            await _userService.ResetFailedLoginAttemptsAsync(user.Id);

            _logger.LogInformation("Password successfully reset for user {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {Username}", username);
            return false;
        }
    }
}