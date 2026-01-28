using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;


namespace RechnungsfreigabeAPI.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserEntityByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
    Task<bool> DeleteUserAsync(int id);
    Task<string[]> GetUserPermissionsAsync(int userId);
    Task<PagedResult<UserDto>> GetUsersPagedAsync(PageRequest pageRequest);
    Task IncrementFailedLoginAttemptsAsync(int userId);
    Task ResetFailedLoginAttemptsAsync(int userId);
    Task LockUserAccountAsync(int userId, DateTime lockedUntil);
    Task UpdatePasswordAsync(int userId, string passwordHash);
    Task UpdateLastLoginAsync(int userId);
}
