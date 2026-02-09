namespace AdoKt07Booking.Dto;

public sealed record CreateHotelBookingDto(
	int RoomId,
	DateTimeOffset StartTime,
	DateTimeOffset EndTime
);
