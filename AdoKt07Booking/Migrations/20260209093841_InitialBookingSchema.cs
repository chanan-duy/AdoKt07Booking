using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdoKt07Booking.Migrations
{
    /// <inheritdoc />
    public partial class InitialBookingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    resource_type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    resource_id = table.Column<int>(type: "INTEGER", nullable: false),
                    start_time = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    end_time = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    cancelled_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                    table.CheckConstraint("CK_bookings_time_range", "start_time < end_time");
                });

            migrationBuilder.CreateTable(
                name: "hotel_rooms",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    room_type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    max_occupancy = table.Column<int>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_rooms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "restaurant_tables",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    seat_capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    section = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurant_tables", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_resource_time_status",
                table: "bookings",
                columns: new[] { "resource_type", "resource_id", "start_time", "end_time", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_hotel_rooms_name",
                table: "hotel_rooms",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_restaurant_tables_name",
                table: "restaurant_tables",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "hotel_rooms");

            migrationBuilder.DropTable(
                name: "restaurant_tables");
        }
    }
}
