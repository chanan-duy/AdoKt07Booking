using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace AdoKt07Booking.Data.Models;

[Table("restaurant_tables")]
[Index(nameof(Name), IsUnique = true, Name = "ux_restaurant_tables_name")]
public class RestaurantTableEntity
{
	[Key] [Column("id")] public int Id { get; set; }

	[Required] [MaxLength(100)] [Column("name")] public string Name { get; set; } = string.Empty;

	[Required] [Column("seat_capacity")] public int SeatCapacity { get; set; }

	[MaxLength(64)] [Column("section")] public string? Section { get; set; }

	[Required] [Column("is_active")] public bool IsActive { get; set; } = true;
}
