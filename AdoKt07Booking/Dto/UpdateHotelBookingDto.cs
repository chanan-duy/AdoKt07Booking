namespace AdoKt07Booking.Dto;

public sealed record UpdateHotelBookingDto(
	DateTimeOffset StartTime,
	DateTimeOffset EndTime
);
