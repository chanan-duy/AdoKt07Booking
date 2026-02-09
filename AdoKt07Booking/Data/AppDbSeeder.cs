using AdoKt07Booking.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoKt07Booking.Data;

public static class AppDbSeeder
{
	public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
	{
		if (!await dbContext.HotelRooms.AnyAsync(cancellationToken))
		{
			dbContext.HotelRooms.AddRange(
				new HotelRoomEntity { Name = "Room 101", RoomType = RoomType.Single, MaxOccupancy = 1, IsActive = true },
				new HotelRoomEntity { Name = "Room 102", RoomType = RoomType.Double, MaxOccupancy = 2, IsActive = true },
				new HotelRoomEntity { Name = "Room 201", RoomType = RoomType.Suite, MaxOccupancy = 4, IsActive = true }
			);
		}

		if (!await dbContext.RestaurantTables.AnyAsync(cancellationToken))
		{
			dbContext.RestaurantTables.AddRange(
				new RestaurantTableEntity { Name = "Table A1", SeatCapacity = 2, Section = "Indoor", IsActive = true },
				new RestaurantTableEntity { Name = "Table A2", SeatCapacity = 4, Section = "Indoor", IsActive = true },
				new RestaurantTableEntity { Name = "Table P1", SeatCapacity = 4, Section = "Patio", IsActive = true },
				new RestaurantTableEntity { Name = "Table B1", SeatCapacity = 6, Section = "Booth", IsActive = true }
			);
		}

		await dbContext.SaveChangesAsync(cancellationToken);
	}
}
