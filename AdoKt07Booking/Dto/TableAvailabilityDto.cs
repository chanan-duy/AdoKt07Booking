namespace AdoKt07Booking.Dto;

public sealed record TableAvailabilityDto(
	int TableId,
	string TableName,
	int SeatCapacity,
	string? Section,
	bool IsBooked
);
