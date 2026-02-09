using AdoKt07Booking.Data.Models;
using AdoKt07Booking.Dto;

namespace AdoKt07Booking.Services;

public interface IHotelService
{
	Task<IReadOnlyList<HotelAvailabilityDto>> GetAvailabilityAsync(
		DateTimeOffset startTime, DateTimeOffset endTime,
		CancellationToken cancellationToken = default
	);

	Task<IReadOnlyList<HotelBookingListItemDto>> GetBookingsAsync(CancellationToken cancellationToken = default);

	Task<BookingEntity> CreateBookingAsync(CreateHotelBookingDto request, CancellationToken cancellationToken = default);

	Task<BookingEntity> UpdateBookingAsync(
		long bookingId,
		UpdateHotelBookingDto request,
		CancellationToken cancellationToken = default
	);

	Task CancelBookingAsync(long bookingId, CancellationToken cancellationToken = default);
}
