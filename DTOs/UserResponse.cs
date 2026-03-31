namespace UserManagementApi.DTOs;

public record UserResponse
(
    int Id,
    string Username,
    string Email
);