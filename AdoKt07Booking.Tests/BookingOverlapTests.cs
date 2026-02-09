using AdoKt07Booking.Data.Models;
using AdoKt07Booking.Services;

namespace AdoKt07Booking.Tests;

public class BookingOverlapTests
{
	[Test]
	public void IsOverlapping_WhenIntervalsOverlap_ReturnsTrue()
	{
		var existingStart = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
		var existingEnd = existingStart.AddHours(2);
		var requestedStart = existingStart.AddHours(1);
		var requestedEnd = existingEnd.AddHours(1);

		var result = BookingOverlap.IsOverlapping(requestedStart, requestedEnd, existingStart, existingEnd);

		Assert.That(result, Is.True);
	}

	[Test]
	public void IsOverlapping_WhenIntervalsTouchAtBoundary_ReturnsFalse()
	{
		var existingStart = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
		var existingEnd = existingStart.AddHours(2);
		var requestedStart = existingEnd;
		var requestedEnd = requestedStart.AddHours(1);

		var result = BookingOverlap.IsOverlapping(requestedStart, requestedEnd, existingStart, existingEnd);

		Assert.That(result, Is.False);
	}

	private static readonly long[] Expected = [2L];

	[Test]
	public void Apply_WhenQueryContainsMixedRanges_ReturnsOnlyOverlappingBookings()
	{
		var requestedStart = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);
		var requestedEnd = requestedStart.AddHours(2);

		var bookings = new[]
		{
			new BookingEntity { Id = 1, StartTime = requestedStart.AddHours(-3), EndTime = requestedStart.AddHours(-1) },
			new BookingEntity { Id = 2, StartTime = requestedStart.AddMinutes(-30), EndTime = requestedEnd.AddMinutes(30) },
			new BookingEntity { Id = 3, StartTime = requestedEnd, EndTime = requestedEnd.AddHours(1) },
		}.AsQueryable();

		var overlapping = BookingOverlap.Apply(bookings, requestedStart, requestedEnd).ToList();

		Assert.That(overlapping.Select(b => b.Id), Is.EqualTo(Expected));
	}
}
