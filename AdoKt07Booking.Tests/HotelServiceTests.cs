using AdoKt07Booking.Data.Models;
using AdoKt07Booking.Dto;
using AdoKt07Booking.Services;
using Microsoft.EntityFrameworkCore;

namespace AdoKt07Booking.Tests;

public class HotelServiceTests
{
	private static readonly DateTimeOffset BaseDate = new(2026, 2, 1, 14, 0, 0, TimeSpan.Zero);
	private static readonly int[] Expected = [1, 2];

	[Test]
	public async Task GetAvailabilityAsync_ReturnsOnlyActiveRoomsAndMarksOverlaps()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.HotelRooms.AddRange(
			new HotelRoomEntity { Id = 1, Name = "Room A", RoomType = RoomType.Single, MaxOccupancy = 1, IsActive = true },
			new HotelRoomEntity { Id = 2, Name = "Room B", RoomType = RoomType.Double, MaxOccupancy = 2, IsActive = true },
			new HotelRoomEntity { Id = 3, Name = "Room C", RoomType = RoomType.Suite, MaxOccupancy = 4, IsActive = false });

		fixture.DbContext.Bookings.AddRange(
			new BookingEntity
			{
				ResourceType = ResourceType.HotelRoom,
				ResourceId = 1,
				StartTime = BaseDate,
				EndTime = BaseDate.AddDays(2),
				Status = BookingStatus.Confirmed,
			},
			new BookingEntity
			{
				ResourceType = ResourceType.HotelRoom,
				ResourceId = 2,
				StartTime = BaseDate,
				EndTime = BaseDate.AddDays(2),
				Status = BookingStatus.Cancelled,
			});

		await fixture.DbContext.SaveChangesAsync();

		var service = new HotelService(fixture.DbContext);

		var availability = await service.GetAvailabilityAsync(BaseDate.AddHours(6), BaseDate.AddDays(1));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(availability.Select(a => a.RoomId), Is.EqualTo(Expected));
			Assert.That(availability.Single(a => a.RoomId == 1).IsBooked, Is.True);
			Assert.That(availability.Single(a => a.RoomId == 2).IsBooked, Is.False);
		}
	}

	[Test]
	public async Task CreateBookingAsync_WhenDurationLessThanOneNight_ThrowsArgumentException()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.HotelRooms.Add(new HotelRoomEntity
			{ Id = 5, Name = "Room D", RoomType = RoomType.Single, MaxOccupancy = 1, IsActive = true });
		await fixture.DbContext.SaveChangesAsync();

		var service = new HotelService(fixture.DbContext);
		var request = new CreateHotelBookingDto(5, BaseDate, BaseDate.AddHours(23));

		var exception = Assert.ThrowsAsync<ArgumentException>(async () => await service.CreateBookingAsync(request));

		Assert.That(exception!.Message, Does.Contain("at least 1 night"));
	}

	[Test]
	public void CreateBookingAsync_WhenRoomDoesNotExist_ThrowsInvalidOperationException()
	{
		Assert.That(async () =>
		{
			await using var fixture = await TestDbFixture.CreateAsync();
			var service = new HotelService(fixture.DbContext);
			var request = new CreateHotelBookingDto(404, BaseDate, BaseDate.AddDays(1));

			await service.CreateBookingAsync(request);
		}, Throws.TypeOf<InvalidOperationException>().With.Message.Contain("Hotel room 404 was not found."));
	}

	[Test]
	public async Task CreateBookingAsync_WhenOverlappingConfirmedBookingExists_ThrowsInvalidOperationException()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.HotelRooms.Add(new HotelRoomEntity
			{ Id = 7, Name = "Room E", RoomType = RoomType.Double, MaxOccupancy = 2, IsActive = true });
		fixture.DbContext.Bookings.Add(new BookingEntity
		{
			ResourceType = ResourceType.HotelRoom,
			ResourceId = 7,
			StartTime = BaseDate,
			EndTime = BaseDate.AddDays(2),
			Status = BookingStatus.Confirmed,
		});
		await fixture.DbContext.SaveChangesAsync();

		var service = new HotelService(fixture.DbContext);
		var request = new CreateHotelBookingDto(7, BaseDate.AddHours(12), BaseDate.AddDays(3));

		Assert.That(async () => await service.CreateBookingAsync(request),
			Throws.TypeOf<InvalidOperationException>().With.Message.Contain("not available"));
	}

	[Test]
	public async Task CreateBookingAsync_WhenRequestValid_PersistsConfirmedBooking()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.HotelRooms.Add(new HotelRoomEntity
			{ Id = 9, Name = "Room F", RoomType = RoomType.Suite, MaxOccupancy = 3, IsActive = true });
		await fixture.DbContext.SaveChangesAsync();

		var service = new HotelService(fixture.DbContext);
		var request = new CreateHotelBookingDto(9, BaseDate, BaseDate.AddDays(2));

		var booking = await service.CreateBookingAsync(request);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(booking.ResourceType, Is.EqualTo(ResourceType.HotelRoom));
			Assert.That(booking.ResourceId, Is.EqualTo(9));
			Assert.That(booking.Status, Is.EqualTo(BookingStatus.Confirmed));
			Assert.That(await fixture.DbContext.Bookings.CountAsync(), Is.EqualTo(1));
		}
	}

	[Test]
	public async Task CancelBookingAsync_WhenBookingExists_CancelsBooking()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		var booking = new BookingEntity
		{
			ResourceType = ResourceType.HotelRoom,
			ResourceId = 2,
			StartTime = BaseDate,
			EndTime = BaseDate.AddDays(1),
			Status = BookingStatus.Confirmed,
		};

		fixture.DbContext.Bookings.Add(booking);
		await fixture.DbContext.SaveChangesAsync();

		var service = new HotelService(fixture.DbContext);

		await service.CancelBookingAsync(booking.Id);

		var persisted = await fixture.DbContext.Bookings.FindAsync(booking.Id);
		Assert.That(persisted, Is.Not.Null);
		using (Assert.EnterMultipleScope())
		{
			Assert.That(persisted!.Status, Is.EqualTo(BookingStatus.Cancelled));
			Assert.That(persisted.CancelledAt, Is.Not.Null);
		}
	}

	[Test]
	public async Task UpdateBookingAsync_WhenRequestValid_UpdatesBookingDates()
	{
		await using var fixture = await TestDbFixture.CreateAsync();
		fixture.DbContext.HotelRooms.Add(new HotelRoomEntity
			{ Id = 11, Name = "Room G", RoomType = RoomType.Double, MaxOccupancy = 2, IsActive = true });
		var booking = new BookingEntity
		{
			ResourceType = ResourceType.HotelRoom,
			ResourceId = 11,
			StartTime = BaseDate,
			EndTime = BaseDate.AddDays(1),
			Status = BookingStatus.Confirmed,
		};

		fixture.DbContext.Bookings.Add(booking);
		await fixture.DbContext.SaveChangesAsync();

		var service = new HotelService(fixture.DbContext);
		var updateRequest = new UpdateHotelBookingDto(BaseDate.AddDays(2), BaseDate.AddDays(4));

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
		fixture.DbContext.HotelRooms.Add(new HotelRoomEntity
			{ Id = 12, Name = "Room H", RoomType = RoomType.Single, MaxOccupancy = 1, IsActive = true });

		var bookingToUpdate = new BookingEntity
		{
			ResourceType = ResourceType.HotelRoom,
			ResourceId = 12,
			StartTime = BaseDate,
			EndTime = BaseDate.AddDays(1),
			Status = BookingStatus.Confirmed,
		};

		var conflictingBooking = new BookingEntity
		{
			ResourceType = ResourceType.HotelRoom,
			ResourceId = 12,
			StartTime = BaseDate.AddDays(3),
			EndTime = BaseDate.AddDays(5),
			Status = BookingStatus.Confirmed,
		};

		fixture.DbContext.Bookings.AddRange(bookingToUpdate, conflictingBooking);
		await fixture.DbContext.SaveChangesAsync();

		var service = new HotelService(fixture.DbContext);

		Assert.That(async () =>
				await service.UpdateBookingAsync(
					bookingToUpdate.Id,
					new UpdateHotelBookingDto(BaseDate.AddDays(4), BaseDate.AddDays(6))),
			Throws.TypeOf<InvalidOperationException>().With.Message.Contain("not available"));
	}
}
