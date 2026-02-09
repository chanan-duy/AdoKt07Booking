using AdoKt07Booking.Data.Models;
using AdoKt07Booking.Dto;

namespace AdoKt07Booking.Services;

public interface IRestaurantService
{
	Task<IReadOnlyList<TableAvailabilityDto>> GetAvailabilityAsync(
		DateTimeOffset startTime, DateTimeOffset endTime,
		CancellationToken cancellationToken = default
	);

	Task<IReadOnlyList<TableBookingListItemDto>> GetBookingsAsync(CancellationToken cancellationToken = default);

	Task<BookingEntity> CreateBookingAsync(CreateTableBookingDto request, CancellationToken cancellationToken = default);

	Task<BookingEntity> UpdateBookingAsync(
		long bookingId,
		UpdateTableBookingDto request,
		CancellationToken cancellationToken = default
	);

	Task CancelBookingAsync(long bookingId, CancellationToken cancellationToken = default);
}
