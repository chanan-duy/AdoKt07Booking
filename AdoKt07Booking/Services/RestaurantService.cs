using System.Data;
using AdoKt07Booking.Data;
using AdoKt07Booking.Data.Models;
using AdoKt07Booking.Dto;
using Microsoft.EntityFrameworkCore;

namespace AdoKt07Booking.Services;

public sealed class RestaurantService(AppDbContext dbContext) : IRestaurantService
{
	private static readonly TimeSpan MaxReservationDuration = TimeSpan.FromHours(2);

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

		var bookedTableIds = await BookingOverlap.Apply(
				dbContext.Bookings.AsNoTracking().Where(b =>
					b.ResourceType == ResourceType.RestaurantTable && b.Status == BookingStatus.Confirmed),
				startTime,
				endTime)
			.Select(b => b.ResourceId)
			.Distinct()
			.ToListAsync(cancellationToken);

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

		if (request.EndTime - request.StartTime > MaxReservationDuration)
		{
			throw new ArgumentException("Restaurant booking cannot exceed 2 hours.");
		}

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
		var overlapCount = await BookingOverlap.Apply(
				dbContext.Bookings.Where(b =>
					b.ResourceType == ResourceType.RestaurantTable &&
					b.ResourceId == request.TableId &&
					b.Status == BookingStatus.Confirmed),
				request.StartTime,
				request.EndTime)
			.CountAsync(cancellationToken);

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

	public async Task CancelBookingAsync(long bookingId, CancellationToken cancellationToken = default)
	{
		var booking = await dbContext.Bookings.FirstOrDefaultAsync(
			b => b.Id == bookingId && b.ResourceType == ResourceType.RestaurantTable,
			cancellationToken);

		if (booking is null)
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
}
