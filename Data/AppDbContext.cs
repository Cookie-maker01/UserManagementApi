using Microsoft.EntityFrameworkCore;
using UserManagementApi.Models;

namespace UserManagementApi.Data;

public class APPDbContext : DbContext
{
  public APPDbContext(DbContextOptions<APPDbContext> options) : base(options) { }
  public DbSet<User> Users => Set<User>();
}