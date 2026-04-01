using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using UserManagementApi.Data;
using UserManagementApi.Models;
using UserManagementApi.DTOs;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// deploy EF Core SQLite
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=users.db"));

// deploy Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// deploy JWT
var jwtkey = builder.Configuration["JwtKey"]?? "ThisIsASecretKeyForDemo";
var keyBytes = Encoding.ASCII.GetBytes(jwtkey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Register
app.MapPost("/api/register", async (RegisterRequest request, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Email == request.Email))
         return Results.BadRequest("Email already exists");

    var user = new User
    {
        Username = request.Username,
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
    };
         
    db.Users.Add(user);
    await db.SaveChangesAsync();

    var response = new UserResponse(
        user.Id,
        user.Username,
        user.Email
    );
    return Results.Ok(response);
});

//Login
app.MapPost("/api/Login", async (LoginRequest login, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == login.Email);
    if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
        return Results.Unauthorized();
        
    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    var jwt = tokenHandler.WriteToken(token);

   return Results.Ok(new
   {
       Token = jwt,
       User = new UserResponse(user.Id, user.Username, user.Email)
   });
});

// Get users profile (need JWT validation)
app.MapGet("/api/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .Select(u => new UserResponse(u.Id, u.Username, u.Email))
        .ToListAsync();
    return Results.Ok(users);
}).RequireAuthorization();

// Get a user profile 
app.MapGet("/api/users/{id}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if(user ==null)
        return Results.NotFound();

    var response = new UserResponse(user.Id, user.Username, user.Email);
    return Results.Ok(response);
}).RequireAuthorization();

// Update user profile
app.MapPut("/api/users/{id}", async (int id, User updateUser, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if(user == null) return Results.NotFound();

    user.Username = updateUser.Username;
    user.Email = updateUser.Email;
    if (!string.IsNullOrEmpty(updateUser.PasswordHash))
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUser.PasswordHash);

        await db.SaveChangesAsync();
        return Results.NoContent();
}).RequireAuthorization();

// Delete user account
app.MapDelete("/api/users/{id}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if(user == null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.Run();



