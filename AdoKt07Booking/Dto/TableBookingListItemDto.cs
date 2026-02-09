using AdoKt07Booking.Data.Models;

namespace AdoKt07Booking.Dto;

public sealed record TableBookingListItemDto(
	long BookingId,
	int TableId,
	string TableName,
	DateTimeOffset StartTime,
	DateTimeOffset EndTime,
	BookingStatus Status,
	DateTimeOffset CreatedAt,
	DateTimeOffset? CancelledAt
);
