using System.Data;
using AdoKt07Booking.Data;
using AdoKt07Booking.Data.Models;
using AdoKt07Booking.Dto;
using Microsoft.EntityFrameworkCore;

namespace AdoKt07Booking.Services;

public sealed class HotelService(AppDbContext dbContext) : IHotelService
{
	public async Task<IReadOnlyList<HotelAvailabilityDto>> GetAvailabilityAsync(
		DateTimeOffset startTime, DateTimeOffset endTime,
		CancellationToken cancellationToken = default
	)
	{
		BookingValidation.EnsureValidRange(startTime, endTime);

		var activeRooms = await dbContext.HotelRooms
			.AsNoTracking()
			.Where(r => r.IsActive)
			.OrderBy(r => r.Name)
			.ToListAsync(cancellationToken);

		var bookings = await dbContext.Bookings.AsNoTracking().ToListAsync(cancellationToken);
		var bookedRoomIds = bookings
			.Where(b => b.ResourceType == ResourceType.HotelRoom && b.Status == BookingStatus.Confirmed)
			.Where(b => BookingOverlap.IsOverlapping(startTime, endTime, b.StartTime, b.EndTime))
			.Select(b => b.ResourceId)
			.Distinct()
			.ToList();

		var bookedSet = bookedRoomIds.ToHashSet();

		return activeRooms
			.Select(r => new HotelAvailabilityDto(r.Id, r.Name, r.RoomType, r.MaxOccupancy, bookedSet.Contains(r.Id)))
			.ToList();
	}

	public async Task<BookingEntity> CreateBookingAsync(
		CreateHotelBookingDto request,
		CancellationToken cancellationToken = default
	)
	{
		BookingValidation.EnsureValidRange(request.StartTime, request.EndTime);

		if (request.EndTime - request.StartTime < TimeSpan.FromDays(1))
		{
			throw new ArgumentException("Hotel booking must be at least 1 night.");
		}

		await using var transaction =
			await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

		var roomExists = await dbContext.HotelRooms
			.Where(r => r.IsActive)
			.AnyAsync(r => r.Id == request.RoomId, cancellationToken);

		if (!roomExists)
		{
			throw new InvalidOperationException($"Hotel room {request.RoomId} was not found.");
		}

		const int capacity = 1;
		var overlapCount = (await dbContext.Bookings
				.ToListAsync(cancellationToken))
			.Count(b =>
				b.ResourceType == ResourceType.HotelRoom &&
				b.ResourceId == request.RoomId &&
				b.Status == BookingStatus.Confirmed &&
				BookingOverlap.IsOverlapping(request.StartTime, request.EndTime, b.StartTime, b.EndTime));

		if (overlapCount >= capacity)
		{
			throw new InvalidOperationException("The room is not available in the requested time window.");
		}

		var booking = new BookingEntity
		{
			ResourceType = ResourceType.HotelRoom,
			ResourceId = request.RoomId,
			StartTime = request.StartTime,
			EndTime = request.EndTime,
			Status = BookingStatus.Confirmed,
			CreatedAt = DateTimeOffset.UtcNow,
		};

		dbContext.Bookings.Add(booking);
		await dbContext.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return booking;
	}

	public async Task CancelBookingAsync(long bookingId, CancellationToken cancellationToken = default)
	{
		var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

		if (booking is null || booking.ResourceType != ResourceType.HotelRoom)
		{
			throw new InvalidOperationException($"Hotel booking {bookingId} was not found.");
		}

		if (booking.Status == BookingStatus.Cancelled)
		{
			return;
		}

		booking.Status = BookingStatus.Cancelled;
		booking.CancelledAt = DateTimeOffset.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);
	}
}
