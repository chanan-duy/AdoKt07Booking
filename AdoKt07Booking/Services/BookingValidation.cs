namespace AdoKt07Booking.Services;

public static class BookingValidation
{
	public static void EnsureValidRange(DateTimeOffset startTime, DateTimeOffset endTime)
	{
		if (startTime >= endTime)
		{
			throw new ArgumentException("Start time must be earlier than end time.");
		}
	}
}
