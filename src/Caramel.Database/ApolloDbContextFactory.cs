using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Caramel.Database;

/// <summary>
/// Factory for creating CaramelDbContext instances at design time (for migrations)
/// Do not delete this file, as it is referenced during migration running from the CLI
/// </summary>
public class CaramelDbContextFactory : IDesignTimeDbContextFactory<CaramelDbContext>
{
  public CaramelDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<CaramelDbContext>();

    // Use a default connection string for design-time operations
    // This won't be used in production - just for generating migrations
    _ = optionsBuilder.UseNpgsql("Host=localhost;Database=caramel_db;Username=caramel;Password=caramel");

    return new CaramelDbContext(optionsBuilder.Options);
  }
}
