using System.Data;
using AdoKt07Booking.Data;
using AdoKt07Booking.Data.Models;
using AdoKt07Booking.Dto;
using Microsoft.EntityFrameworkCore;

namespace AdoKt07Booking.Services;

public sealed class RestaurantService(AppDbContext dbContext) : IRestaurantService
{
	private static readonly TimeSpan MaxReservationDuration = TimeSpan.FromHours(2);

	public async Task<IReadOnlyList<TableBookingListItemDto>> GetBookingsAsync(CancellationToken cancellationToken = default)
	{
		var tables = await dbContext.RestaurantTables
			.AsNoTracking()
			.ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

		var bookings = await dbContext.Bookings
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		return bookings
			.Where(b => b.ResourceType == ResourceType.RestaurantTable)
			.OrderByDescending(b => b.StartTime)
			.Select(b => new TableBookingListItemDto(
				b.Id,
				b.ResourceId,
				tables.GetValueOrDefault(b.ResourceId, $"Table #{b.ResourceId}"),
				b.StartTime,
				b.EndTime,
				b.Status,
				b.CreatedAt,
				b.CancelledAt))
			.ToList();
	}

	public async Task<IReadOnlyList<TableAvailabilityDto>> GetAvailabilityAsync(
		DateTimeOffset startTime,
		DateTimeOffset endTime, CancellationToken cancellationToken = default
	)
	{
		BookingValidation.EnsureValidRange(startTime, endTime);

		var activeTables = await dbContext.RestaurantTables
			.AsNoTracking()
			.Where(t => t.IsActive)
			.OrderBy(t => t.Name)
			.ToListAsync(cancellationToken);

		var bookings = await dbContext.Bookings.AsNoTracking().ToListAsync(cancellationToken);
		var bookedTableIds = bookings
			.Where(b => b.ResourceType == ResourceType.RestaurantTable && b.Status == BookingStatus.Confirmed)
			.Where(b => BookingOverlap.IsOverlapping(startTime, endTime, b.StartTime, b.EndTime))
			.Select(b => b.ResourceId)
			.Distinct()
			.ToList();

		var bookedSet = bookedTableIds.ToHashSet();

		return activeTables
			.Select(t => new TableAvailabilityDto(t.Id, t.Name, t.SeatCapacity, t.Section, bookedSet.Contains(t.Id)))
			.ToList();
	}

	public async Task<BookingEntity> CreateBookingAsync(
		CreateTableBookingDto request,
		CancellationToken cancellationToken = default
	)
	{
		BookingValidation.EnsureValidRange(request.StartTime, request.EndTime);
		EnsureWithinMaxDuration(request.StartTime, request.EndTime);

		await using var transaction =
			await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

		var tableExists = await dbContext.RestaurantTables
			.Where(t => t.IsActive)
			.AnyAsync(t => t.Id == request.TableId, cancellationToken);

		if (!tableExists)
		{
			throw new InvalidOperationException($"Restaurant table {request.TableId} was not found.");
		}

		const int capacity = 1;
		var overlapCount = (await dbContext.Bookings
				.ToListAsync(cancellationToken))
			.Count(b =>
				b.ResourceType == ResourceType.RestaurantTable &&
				b.ResourceId == request.TableId &&
				b.Status == BookingStatus.Confirmed &&
				BookingOverlap.IsOverlapping(request.StartTime, request.EndTime, b.StartTime, b.EndTime));

		if (overlapCount >= capacity)
		{
			throw new InvalidOperationException("The table is not available in the requested time window.");
		}

		var booking = new BookingEntity
		{
			ResourceType = ResourceType.RestaurantTable,
			ResourceId = request.TableId,
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

	public async Task<BookingEntity> UpdateBookingAsync(
		long bookingId,
		UpdateTableBookingDto request,
		CancellationToken cancellationToken = default
	)
	{
		BookingValidation.EnsureValidRange(request.StartTime, request.EndTime);
		EnsureWithinMaxDuration(request.StartTime, request.EndTime);

		await using var transaction =
			await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

		var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

		if (booking is null || booking.ResourceType != ResourceType.RestaurantTable)
		{
			throw new InvalidOperationException($"Table booking {bookingId} was not found.");
		}

		if (booking.Status == BookingStatus.Cancelled)
		{
			throw new InvalidOperationException("Cancelled table booking cannot be updated.");
		}

		const int capacity = 1;
		var overlapCount = (await dbContext.Bookings
				.ToListAsync(cancellationToken))
			.Count(b =>
				b.Id != bookingId &&
				b.ResourceType == ResourceType.RestaurantTable &&
				b.ResourceId == booking.ResourceId &&
				b.Status == BookingStatus.Confirmed &&
				BookingOverlap.IsOverlapping(request.StartTime, request.EndTime, b.StartTime, b.EndTime));

		if (overlapCount >= capacity)
		{
			throw new InvalidOperationException("The table is not available in the requested time window.");
		}

		booking.StartTime = request.StartTime;
		booking.EndTime = request.EndTime;

		await dbContext.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return booking;
	}

	public async Task CancelBookingAsync(long bookingId, CancellationToken cancellationToken = default)
	{
		var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

		if (booking is null || booking.ResourceType != ResourceType.RestaurantTable)
		{
			throw new InvalidOperationException($"Table booking {bookingId} was not found.");
		}

		if (booking.Status == BookingStatus.Cancelled)
		{
			return;
		}

		booking.Status = BookingStatus.Cancelled;
		booking.CancelledAt = DateTimeOffset.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);
	}

	private static void EnsureWithinMaxDuration(DateTimeOffset startTime, DateTimeOffset endTime)
	{
		if (endTime - startTime > MaxReservationDuration)
		{
			throw new ArgumentException("Restaurant booking cannot exceed 2 hours.");
		}
	}
}
