using AdoKt07Booking.Data.Models;

namespace AdoKt07Booking.Dto;

public sealed record HotelAvailabilityDto(
	int RoomId,
	string RoomName,
	RoomType RoomType,
	int MaxOccupancy,
	bool IsBooked
);
