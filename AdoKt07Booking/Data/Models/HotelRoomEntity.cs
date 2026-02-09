using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace AdoKt07Booking.Data.Models;

[Table("hotel_rooms")]
[Index(nameof(Name), IsUnique = true, Name = "ux_hotel_rooms_name")]
public class HotelRoomEntity
{
	[Key] [Column("id")] public int Id { get; set; }

	[Required] [MaxLength(100)] [Column("name")] public string Name { get; set; } = string.Empty;

	[Required] [MaxLength(32)] [Column("room_type")] public RoomType RoomType { get; set; }

	[Required] [Column("max_occupancy")] public int MaxOccupancy { get; set; }

	[Required] [Column("is_active")] public bool IsActive { get; set; } = true;
}
