using Microsoft.EntityFrameworkCore;

namespace AdoKt07Booking.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	// public DbSet<TaskEntity> Tasks { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// modelBuilder.Entity<TaskEntity>()
		// 	.Property(x => x.Id)
		// 	.ValueGeneratedOnAdd();

		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
				{
					property.SetValueConverter(new DateTimeUtcConverter());
				}
			}
		}
	}
}
