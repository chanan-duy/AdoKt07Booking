using AdoKt07Booking.Data.Models;

namespace AdoKt07Booking.Services;

public static class BookingOverlap
{
	public static bool IsOverlapping(
		DateTimeOffset requestedStart, DateTimeOffset requestedEnd, DateTimeOffset existingStart,
		DateTimeOffset existingEnd
	)
	{
		return requestedStart < existingEnd && requestedEnd > existingStart;
	}

	public static IQueryable<BookingEntity> Apply(
		IQueryable<BookingEntity> query, DateTimeOffset requestedStart,
		DateTimeOffset requestedEnd
	)
	{
		return query.Where(b => requestedStart < b.EndTime && requestedEnd > b.StartTime);
	}
}
