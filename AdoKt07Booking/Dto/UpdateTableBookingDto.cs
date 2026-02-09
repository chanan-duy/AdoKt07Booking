namespace AdoKt07Booking.Dto;

public sealed record UpdateTableBookingDto(
	DateTimeOffset StartTime,
	DateTimeOffset EndTime
);
