namespace UserManagementApi.DTOs;

public record RegisterRequest
(
    string Username,
    string Email,
    string Password
);