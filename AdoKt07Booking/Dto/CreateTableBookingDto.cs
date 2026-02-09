namespace AdoKt07Booking.Dto;

public sealed record CreateTableBookingDto(
	int TableId,
	DateTimeOffset StartTime,
	DateTimeOffset EndTime
);
