using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using UserManagementApi.Data;
using UserManagementApi.Models;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// deploy EF Core SQLite
builder.Services.AddDbContext<APPDbContext>(options => options.UseSqlite("Data Source=users.db"));

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
