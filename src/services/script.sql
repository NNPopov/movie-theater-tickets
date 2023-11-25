CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE cinema_hall (
    id uuid NOT NULL,
    description text NOT NULL,
    name text NOT NULL,
    CONSTRAINT pk_cinema_hall PRIMARY KEY (id)
);

CREATE TABLE idempotent_request (
    id uuid NOT NULL,
    name text NOT NULL,
    created_on_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_idempotent_request PRIMARY KEY (id)
);

CREATE TABLE movie (
    id uuid NOT NULL,
    title text NOT NULL,
    imdb_id text NOT NULL,
    stars text NOT NULL,
    release_date timestamp with time zone NOT NULL,
    CONSTRAINT pk_movie_id PRIMARY KEY (id)
);

CREATE TABLE movie_session (
    id uuid NOT NULL,
    movie_id uuid NOT NULL,
    session_date timestamp with time zone NOT NULL,
    cinema_hall_id uuid NOT NULL,
    tickets_for_sale integer NOT NULL,
    sold_tickets integer NOT NULL,
    is_enabled boolean NOT NULL,
    CONSTRAINT pk_movie_session_id PRIMARY KEY (id)
);

CREATE TABLE movie_session_seat (
    showtime uuid NOT NULL,
    seat_number smallint NOT NULL,
    seat_row smallint NOT NULL,
    price numeric NOT NULL,
    status integer NOT NULL,
    shopping_cart_id uuid NOT NULL,
    hash_id text NOT NULL,
    CONSTRAINT pk_movie_session_seat PRIMARY KEY (showtime, seat_row, seat_number)
);

CREATE TABLE show_time_seat (
    row smallint NOT NULL,
    seat_number smallint NOT NULL,
    cinema_hall_id uuid NOT NULL,
    CONSTRAINT kp_show_time_seat PRIMARY KEY (cinema_hall_id, row, seat_number)
);

CREATE TABLE seat (
    row smallint NOT NULL,
    seat_number smallint NOT NULL,
    cinema_hall_id uuid NOT NULL,
    CONSTRAINT pk_seat PRIMARY KEY (cinema_hall_id, row, seat_number),
    CONSTRAINT fk_seat_cinema_hall_cinema_hall_id FOREIGN KEY (cinema_hall_id) REFERENCES cinema_hall (id) ON DELETE CASCADE
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231030211548_Initial', '8.0.0-rc.2.23480.1');

COMMIT;

