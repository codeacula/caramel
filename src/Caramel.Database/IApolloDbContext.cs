namespace Caramel.Database;

public interface ICaramelDbContext
{
  Task MigrateAsync(CancellationToken cancellationToken = default);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
