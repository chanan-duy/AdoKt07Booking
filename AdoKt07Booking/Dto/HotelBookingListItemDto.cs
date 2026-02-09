using AdoKt07Booking.Data.Models;

namespace AdoKt07Booking.Dto;

public sealed record HotelBookingListItemDto(
	long BookingId,
	int RoomId,
	string RoomName,
	DateTimeOffset StartTime,
	DateTimeOffset EndTime,
	BookingStatus Status,
	DateTimeOffset CreatedAt,
	DateTimeOffset? CancelledAt
);
