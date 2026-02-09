using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace AdoKt07Booking.Data.Models;

[Table("bookings")]
[Index(
	nameof(ResourceType), nameof(ResourceId),
	nameof(StartTime), nameof(EndTime),
	nameof(Status),
	Name = "ix_bookings_resource_time_status")]
public class BookingEntity
{
	[Key] [Column("id")] public long Id { get; set; }

	[Required]
	[MaxLength(32)]
	[Column("resource_type")]
	public ResourceType ResourceType { get; set; }

	[Required] [Column("resource_id")] public int ResourceId { get; set; }

	[Required] [Column("start_time")] public DateTimeOffset StartTime { get; set; }

	[Required] [Column("end_time")] public DateTimeOffset EndTime { get; set; }

	[Required]
	[MaxLength(32)]
	[Column("status")]
	public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

	[Required] [Column("created_at")] public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	[Column("cancelled_at")] public DateTimeOffset? CancelledAt { get; set; }
}
