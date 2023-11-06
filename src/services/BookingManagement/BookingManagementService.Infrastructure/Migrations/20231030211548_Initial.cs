using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaTicketBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cinema_hall",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cinema_hall", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotent_request",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotent_request", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movie",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    imdb_id = table.Column<string>(type: "text", nullable: false),
                    stars = table.Column<string>(type: "text", nullable: false),
                    release_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movie_id", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movie_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    movie_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cinema_hall_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tickets_for_sale = table.Column<int>(type: "integer", nullable: false),
                    sold_tickets = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movie_session_id", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movie_session_seat",
                columns: table => new
                {
                    showtime = table.Column<Guid>(type: "uuid", nullable: false),
                    seat_number = table.Column<short>(type: "smallint", nullable: false),
                    seat_row = table.Column<short>(type: "smallint", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    shopping_cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movie_session_seat", x => new { x.showtime, x.seat_row, x.seat_number });
                });

            migrationBuilder.CreateTable(
                name: "show_time_seat",
                columns: table => new
                {
                    row = table.Column<short>(type: "smallint", nullable: false),
                    seat_number = table.Column<short>(type: "smallint", nullable: false),
                    cinema_hall_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("kp_show_time_seat", x => new { x.cinema_hall_id, x.row, x.seat_number });
                });

            migrationBuilder.CreateTable(
                name: "seat",
                columns: table => new
                {
                    row = table.Column<short>(type: "smallint", nullable: false),
                    seat_number = table.Column<short>(type: "smallint", nullable: false),
                    cinema_hall_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seat", x => new { x.cinema_hall_id, x.row, x.seat_number });
                    table.ForeignKey(
                        name: "fk_seat_cinema_hall_cinema_hall_id",
                        column: x => x.cinema_hall_id,
                        principalTable: "cinema_hall",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotent_request");

            migrationBuilder.DropTable(
                name: "movie");

            migrationBuilder.DropTable(
                name: "movie_session");

            migrationBuilder.DropTable(
                name: "movie_session_seat");

            migrationBuilder.DropTable(
                name: "seat");

            migrationBuilder.DropTable(
                name: "show_time_seat");

            migrationBuilder.DropTable(
                name: "cinema_hall");
        }
    }
}
