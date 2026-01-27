using Microsoft.IdentityModel.Tokens;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public AuthService(IUserService userService, IPasswordService passwordService, IConfiguration configuration)
    {
        _userService = userService;
        _passwordService = passwordService;
        _configuration = configuration;
        }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
    {
        try
        {
            var user = await _userService.GetUserByUsernameAsync(loginRequest.Username);
            
            if (user == null || !user.IsActive)
            {
                
                return null;
            }

            // Check if user is locked out
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                
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
                    
                }

                return null;
            }

            // Reset failed login attempts on successful login
            await _userService.ResetFailedLoginAttemptsAsync(user.Id);

            // Update last login timestamp
            await _userService.UpdateLastLoginAsync(user.Id);

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
                LastLogin = DateTime.UtcNow,
                Roles = user.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description,
                    Permissions = ur.Role.RolePermissions?.Select(rp => (object)rp.PermissionId).ToList() ?? new List<object>()
                }).ToArray()
            };

            return new LoginResponseDto
            {
                Token = token,
                User = userDto,
                Permissions = permissions
            };
        }
        catch (Exception)
        {
            
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
        catch (Exception)
        {
            
            return null;
        }
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var principal = ValidateJwtToken(token);
            return Task.FromResult(principal != null);
        }
        catch
        {
            return Task.FromResult(false);
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

        // Add role claims so [Authorize(Roles = "...")] works
        if (user.UserRoles != null)
        {
            foreach (var ur in user.UserRoles)
            {
                if (!string.IsNullOrWhiteSpace(ur.Role?.Name))
                {
                    claims.Add(new Claim(ClaimTypes.Role, ur.Role.Name));
                }
            }
        }

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
                
                return false;
            }

            // Validate new password
            if (!_passwordService.IsPasswordValid(newPassword))
            {
                
                return false;
            }

            // Hash new password and update user
            var newPasswordHash = _passwordService.HashPassword(newPassword);
            await _userService.UpdatePasswordAsync(userId, newPasswordHash);

            return true;
        }
        catch (Exception)
        {
            
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
                
                return false;
            }

            // Hash new password and update user
            var newPasswordHash = _passwordService.HashPassword(newPassword);
            await _userService.UpdatePasswordAsync(user.Id, newPasswordHash);

            // Reset failed login attempts and unlock account
            await _userService.ResetFailedLoginAttemptsAsync(user.Id);

            return true;
        }
        catch (Exception)
        {
            
            return false;
        }
    }
}