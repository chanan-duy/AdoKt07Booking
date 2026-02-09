using AdoKt07Booking.Data.Models;
using AdoKt07Booking.Dto;
using AdoKt07Booking.Services;

namespace AdoKt07Booking.Tests;

public class RestaurantServiceTests
{
	private static readonly DateTimeOffset BaseDate = new(2026, 2, 1, 18, 0, 0, TimeSpan.Zero);
	private static readonly int[] Expected = [1, 2];

	[Test]
	public async Task GetAvailabilityAsync_ReturnsOnlyActiveTablesAndMarksOverlaps()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.RestaurantTables.AddRange(
			new RestaurantTableEntity { Id = 1, Name = "Table A", SeatCapacity = 2, Section = "Main", IsActive = true },
			new RestaurantTableEntity { Id = 2, Name = "Table B", SeatCapacity = 4, Section = "Main", IsActive = true },
			new RestaurantTableEntity { Id = 3, Name = "Table C", SeatCapacity = 6, Section = "Patio", IsActive = false });

		fixture.DbContext.Bookings.AddRange(
			new BookingEntity
			{
				ResourceType = ResourceType.RestaurantTable,
				ResourceId = 1,
				StartTime = BaseDate,
				EndTime = BaseDate.AddHours(2),
				Status = BookingStatus.Confirmed,
			},
			new BookingEntity
			{
				ResourceType = ResourceType.RestaurantTable,
				ResourceId = 2,
				StartTime = BaseDate,
				EndTime = BaseDate.AddHours(2),
				Status = BookingStatus.Cancelled,
			});

		await fixture.DbContext.SaveChangesAsync();

		var service = new RestaurantService(fixture.DbContext);

		var availability = await service.GetAvailabilityAsync(BaseDate.AddMinutes(30), BaseDate.AddHours(1));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(availability.Select(a => a.TableId), Is.EqualTo(Expected));
			Assert.That(availability.Single(a => a.TableId == 1).IsBooked, Is.True);
			Assert.That(availability.Single(a => a.TableId == 2).IsBooked, Is.False);
		}
	}

	[Test]
	public async Task CreateBookingAsync_WhenDurationExceedsTwoHours_ThrowsArgumentException()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.RestaurantTables.Add(new RestaurantTableEntity
			{ Id = 5, Name = "Table D", SeatCapacity = 4, Section = "Booth", IsActive = true });
		await fixture.DbContext.SaveChangesAsync();

		var service = new RestaurantService(fixture.DbContext);
		var request = new CreateTableBookingDto(5, BaseDate, BaseDate.AddHours(3));

		Assert.That(async () => await service.CreateBookingAsync(request),
			Throws.TypeOf<ArgumentException>().With.Message.Contain("cannot exceed 2 hours"));
	}

	[Test]
	public async Task CreateBookingAsync_WhenOverlappingConfirmedBookingExists_ThrowsInvalidOperationException()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.RestaurantTables.Add(new RestaurantTableEntity
			{ Id = 7, Name = "Table E", SeatCapacity = 2, Section = "Main", IsActive = true });
		fixture.DbContext.Bookings.Add(new BookingEntity
		{
			ResourceType = ResourceType.RestaurantTable,
			ResourceId = 7,
			StartTime = BaseDate,
			EndTime = BaseDate.AddHours(2),
			Status = BookingStatus.Confirmed,
		});
		await fixture.DbContext.SaveChangesAsync();

		var service = new RestaurantService(fixture.DbContext);
		var request = new CreateTableBookingDto(7, BaseDate.AddMinutes(15), BaseDate.AddHours(1));

		Assert.That(async () => await service.CreateBookingAsync(request),
			Throws.TypeOf<InvalidOperationException>().With.Message.Contain("not available"));
	}

	[Test]
	public async Task CancelBookingAsync_WhenBookingResourceTypeIsNotTable_ThrowsInvalidOperationException()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		var booking = new BookingEntity
		{
			ResourceType = ResourceType.HotelRoom,
			ResourceId = 1,
			StartTime = BaseDate,
			EndTime = BaseDate.AddDays(1),
			Status = BookingStatus.Confirmed,
		};

		fixture.DbContext.Bookings.Add(booking);
		await fixture.DbContext.SaveChangesAsync();

		var service = new RestaurantService(fixture.DbContext);

		Assert.That(async () => await service.CancelBookingAsync(booking.Id),
			Throws.TypeOf<InvalidOperationException>().With.Message.Contain("was not found"));
	}

	[Test]
	public async Task UpdateBookingAsync_WhenRequestValid_UpdatesBookingDates()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.RestaurantTables.Add(new RestaurantTableEntity
			{ Id = 8, Name = "Table F", SeatCapacity = 4, Section = "Main", IsActive = true });
		var booking = new BookingEntity
		{
			ResourceType = ResourceType.RestaurantTable,
			ResourceId = 8,
			StartTime = BaseDate,
			EndTime = BaseDate.AddHours(1),
			Status = BookingStatus.Confirmed,
		};

		fixture.DbContext.Bookings.Add(booking);
		await fixture.DbContext.SaveChangesAsync();

		var service = new RestaurantService(fixture.DbContext);
		var updateRequest = new UpdateTableBookingDto(BaseDate.AddHours(3), BaseDate.AddHours(4));

		var updated = await service.UpdateBookingAsync(booking.Id, updateRequest);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(updated.StartTime, Is.EqualTo(updateRequest.StartTime));
			Assert.That(updated.EndTime, Is.EqualTo(updateRequest.EndTime));
		}
	}

	[Test]
	public async Task UpdateBookingAsync_WhenOverlappingConfirmedBookingExists_ThrowsInvalidOperationException()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.RestaurantTables.Add(new RestaurantTableEntity
			{ Id = 9, Name = "Table G", SeatCapacity = 2, Section = "Patio", IsActive = true });

		var bookingToUpdate = new BookingEntity
		{
			ResourceType = ResourceType.RestaurantTable,
			ResourceId = 9,
			StartTime = BaseDate,
			EndTime = BaseDate.AddMinutes(30),
			Status = BookingStatus.Confirmed,
		};

		var conflictingBooking = new BookingEntity
		{
			ResourceType = ResourceType.RestaurantTable,
			ResourceId = 9,
			StartTime = BaseDate.AddHours(1),
			EndTime = BaseDate.AddHours(2),
			Status = BookingStatus.Confirmed,
		};

		fixture.DbContext.Bookings.AddRange(bookingToUpdate, conflictingBooking);
		await fixture.DbContext.SaveChangesAsync();

		var service = new RestaurantService(fixture.DbContext);

		Assert.That(async () =>
				await service.UpdateBookingAsync(
					bookingToUpdate.Id,
					new UpdateTableBookingDto(BaseDate.AddHours(1).AddMinutes(15), BaseDate.AddHours(1).AddMinutes(45))),
			Throws.TypeOf<InvalidOperationException>().With.Message.Contain("not available"));
	}
}
