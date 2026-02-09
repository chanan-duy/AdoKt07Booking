using AdoKt07Booking.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoKt07Booking.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	public DbSet<HotelRoomEntity> HotelRooms { get; set; }
	public DbSet<RestaurantTableEntity> RestaurantTables { get; set; }
	public DbSet<BookingEntity> Bookings { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<HotelRoomEntity>()
			.Property(x => x.RoomType)
			.HasConversion<string>();

		modelBuilder.Entity<BookingEntity>(entity =>
		{
			entity.ToTable("bookings",
				tableBuilder => { tableBuilder.HasCheckConstraint("CK_bookings_time_range", "start_time < end_time"); });

			entity.Property(x => x.Status)
				.HasConversion<string>();

			entity.Property(x => x.ResourceType)
				.HasConversion<string>();
		});

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
