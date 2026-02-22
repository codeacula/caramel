using Microsoft.EntityFrameworkCore;

namespace Caramel.Database;

public class CaramelDbContext(DbContextOptions<CaramelDbContext> options) : DbContext(options), ICaramelDbContext
{
  public async Task MigrateAsync(CancellationToken cancellationToken = default)
  {
    await Database.MigrateAsync(cancellationToken);
  }
}
