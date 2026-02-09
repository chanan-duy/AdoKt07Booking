using AdoKt07Booking.Services;

namespace AdoKt07Booking.Tests;

public class BookingValidationTests
{
	[Test]
	public void EnsureValidRange_WhenStartBeforeEnd_DoesNotThrow()
	{
		var start = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
		var end = start.AddMinutes(30);

		Assert.DoesNotThrow(() => BookingValidation.EnsureValidRange(start, end));
	}

	[Test]
	public void EnsureValidRange_WhenStartEqualsEnd_ThrowsArgumentException()
	{
		var instant = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);

		var exception = Assert.Throws<ArgumentException>(() => BookingValidation.EnsureValidRange(instant, instant));

		Assert.That(exception!.Message, Does.Contain("Start time must be earlier"));
	}

	[Test]
	public void EnsureValidRange_WhenStartAfterEnd_ThrowsArgumentException()
	{
		var end = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
		var start = end.AddMinutes(1);

		Assert.Throws<ArgumentException>(() => BookingValidation.EnsureValidRange(start, end));
	}
}
