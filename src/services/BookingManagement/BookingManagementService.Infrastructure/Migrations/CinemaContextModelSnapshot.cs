﻿// <auto-generated />
using System;
using CinemaTicketBooking.Infrastructure;
using CinemaTicketBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CinemaTicketBooking.Infrastructure.Migrations
{
    [DbContext(typeof(CinemaContext))]
    partial class CinemaContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0-rc.2.23480.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CinemaTicketBooking.Domain.CinemaHalls.CinemaHall", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_cinema_hall");

                    b.ToTable("cinema_hall", (string)null);
                });

            modelBuilder.Entity("CinemaTicketBooking.Domain.CinemaHalls.ShowTimeSeatEntity", b =>
                {
                    b.Property<Guid>("CinemaHallId")
                        .HasColumnType("uuid")
                        .HasColumnName("cinema_hall_id");

                    b.Property<short>("Row")
                        .HasColumnType("smallint")
                        .HasColumnName("row");

                    b.Property<short>("SeatNumber")
                        .HasColumnType("smallint")
                        .HasColumnName("seat_number");

                    b.HasKey("CinemaHallId", "Row", "SeatNumber")
                        .HasName("kp_show_time_seat");

                    b.ToTable("show_time_seat", (string)null);
                });

            modelBuilder.Entity("CinemaTicketBooking.Domain.MovieSessions.MovieSession", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("CinemaHallId")
                        .HasColumnType("uuid")
                        .HasColumnName("cinema_hall_id");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("boolean")
                        .HasColumnName("is_enabled");

                    b.Property<Guid>("MovieId")
                        .HasColumnType("uuid")
                        .HasColumnName("movie_id");

                    b.Property<DateTime>("SessionDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("session_date");

                    b.Property<int>("SoldTickets")
                        .HasColumnType("integer")
                        .HasColumnName("sold_tickets");

                    b.Property<int>("TicketsForSale")
                        .HasColumnType("integer")
                        .HasColumnName("tickets_for_sale");

                    b.HasKey("Id")
                        .HasName("pk_movie_session_id");

                    b.ToTable("movie_session", (string)null);
                });

            modelBuilder.Entity("CinemaTicketBooking.Domain.Movies.Movie", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("ImdbId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("imdb_id");

                    b.Property<DateTime>("ReleaseDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("release_date");

                    b.Property<string>("Stars")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("stars");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("title");

                    b.HasKey("Id")
                        .HasName("pk_movie_id");

                    b.ToTable("movie", (string)null);
                });

            modelBuilder.Entity("CinemaTicketBooking.Domain.Seats.MovieSessionSeat", b =>
                {
                    b.Property<Guid>("MovieSessionId")
                        .HasColumnType("uuid")
                        .HasColumnName("showtime");

                    b.Property<short>("SeatRow")
                        .HasColumnType("smallint")
                        .HasColumnName("seat_row");

                    b.Property<short>("SeatNumber")
                        .HasColumnType("smallint")
                        .HasColumnName("seat_number");

                    b.Property<string>("HashId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("hash_id");

                    b.Property<decimal>("Price")
                        .HasColumnType("numeric")
                        .HasColumnName("price");

                    b.Property<Guid>("ShoppingCartId")
                        .HasColumnType("uuid")
                        .HasColumnName("shopping_cart_id");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.HasKey("MovieSessionId", "SeatRow", "SeatNumber")
                        .HasName("pk_movie_session_seat");

                    b.ToTable("movie_session_seat", (string)null);
                });

            modelBuilder.Entity("CinemaTicketBooking.Infrastructure.Services.IdempotentRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on_utc");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_idempotent_request");

                    b.ToTable("idempotent_request", (string)null);
                });

            modelBuilder.Entity("CinemaTicketBooking.Domain.CinemaHalls.CinemaHall", b =>
                {
                    b.OwnsMany("CinemaTicketBooking.Domain.CinemaHalls.SeatEntity", "Seats", b1 =>
                        {
                            b1.Property<Guid>("CinemaHallId")
                                .HasColumnType("uuid")
                                .HasColumnName("cinema_hall_id");

                            b1.Property<short>("Row")
                                .HasColumnType("smallint")
                                .HasColumnName("row");

                            b1.Property<short>("SeatNumber")
                                .HasColumnType("smallint")
                                .HasColumnName("seat_number");

                            b1.HasKey("CinemaHallId", "Row", "SeatNumber")
                                .HasName("pk_seat");

                            b1.ToTable("seat", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("CinemaHallId")
                                .HasConstraintName("fk_seat_cinema_hall_cinema_hall_id");
                        });

                    b.Navigation("Seats");
                });
#pragma warning restore 612, 618
        }
    }
}