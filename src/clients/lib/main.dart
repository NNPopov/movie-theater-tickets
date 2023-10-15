import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:dartz/dartz.dart' as d;
import 'package:movie_theater_tickets/src/cinema_halls/presentation/cubit/cinema_hall_cubit.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/repos/movie_session_repo.dart';
import 'package:movie_theater_tickets/src/movie_sessions/presentation/cubit/movie_session_cubit.dart';
import 'package:movie_theater_tickets/src/movies/domain/entities/movie.dart';
import 'core/errors/failures.dart';
import 'core/services/router.main.dart';
import 'injection_container.dart';
import 'src/movies/domain/repos/movie_repo.dart';
import 'package:get_it/get_it.dart';
import 'package:collection/collection.dart';
import 'package:carousel_slider/carousel_slider.dart';
import 'src/movies/presentation/app/movie_theater_cubit.dart';
import 'src/seats/domain/entities/seat.dart';
import 'src/seats/domain/repos/seat_repo.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

final sl = GetIt.instance;

void main() async {
  await dotenv.load();

  await initializeDependencies();

  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
        providers: [
          BlocProvider<MovieTheaterCubit>(
              create: (context) => MovieTheaterCubit()),
          BlocProvider<MovieSessionCubit>(
              create: (context) => MovieSessionCubit()),
          BlocProvider<CinemaHallCubit>(create: (context) => CinemaHallCubit())
        ],
        child: MaterialApp(
          title: 'Flutter Demo',
          theme: ThemeData(
            colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
            useMaterial3: true,
            textTheme: const TextTheme(
                bodyLarge: TextStyle(fontSize: 8.0, color: Colors.black)),
          ),
          onGenerateRoute: generateRoute,
        ));
  }
}

class MyHomePage extends StatefulWidget {
  const MyHomePage({super.key, required this.title});

  final String title;

  @override
  State<MyHomePage> createState() => _MyHomePageState();
}

class _MyHomePageState extends State<MyHomePage> {
  int _counter = 0;

  List<MovieSession>? movieSessions;

  List<Seat>? seats;

  CarouselController buttonCarouselController = CarouselController();

  Future<void> _incrementCounter() async {
    MovieRepo movieRepo = sl.get<MovieRepo>();
    var movies = await movieRepo.getMovies();
  }

  Future<void> pressSeat(Seat seat) async {
    MovieRepo movieRepo = sl.get<MovieRepo>();

    var movies = await movieRepo.getMovies();
  }

  Future<void> movieSeat(Movie rowSeats) async {
    MovieSessionRepo movieSessionRepo = sl.get<MovieSessionRepo>();
    var movieSessionsResult =
        await movieSessionRepo.getMovieSessionByMovieId(rowSeats.id);
    setState(() {
      movieSessionsResult.fold((error) => {}, (data) => {movieSessions = data});
    });
  }

  Future<void> movieSession(MovieSession rowSeats) async {
    SeatRepo seatRepo = sl.get<SeatRepo>();

    var seatsResult = await seatRepo.getSeatsByMovieSessionId(rowSeats.id);
    setState(() {
      seatsResult.fold((error) => {}, (data) => {seats = data});
    });
  }

  @override
  Widget build(BuildContext context) {
    SeatRepo seatRepo = sl.get<SeatRepo>();

    MovieRepo movieRepo = sl.get<MovieRepo>();

    return FutureBuilder<d.Either<Failure, List<Movie>>>(
        future: movieRepo.getMovies(),
        builder: (context, snapshot) {
          if (snapshot.hasData) {
            return snapshot.data!.fold(
                (error) => Text(error.message),
                (post) => Scaffold(
                    appBar: AppBar(
                      backgroundColor:
                          Theme.of(context).colorScheme.inversePrimary,
                      title: Text('Tets'),
                    ),
                    body: Column(children: [
                      BuildMovies(post, context),
                      buildSinemaSessions(context),
                      buildSinemaSessionSeats(context)
                    ]),
                    floatingActionButton: FloatingActionButton(
                      onPressed: _incrementCounter,
                      tooltip: 'Increment',
                      child: const Icon(Icons.add),
                    )));
          }

          // return FutureBuilder<d.Either<Failure, List<Movie>>>(
          //     future: movieRepo.getMovies(),
          //     builder: (context, snapshot) {
          //       if (snapshot.hasData) {
          //         return snapshot.data!.fold(
          //           (error) => Text(error.message),
          //           (post) => BuildMovies(post, context),
          //         );
          //       }

          return CircularProgressIndicator();
        });

    return FutureBuilder<d.Either<Failure, List<Seat>>>(
        future: seatRepo
            .getSeatsByMovieSessionId('9ff3c08f-64df-4198-8004-44b93a031753'),
        builder: (context, snapshot) {
          if (snapshot.hasData) {
            return snapshot.data!.fold(
              (error) => Text(error.message),
              (post) => BuildSeat(post, context),
            );
          }

          return CircularProgressIndicator();
        });
  }

  Widget buildSinemaSessions(BuildContext context) {
    if (movieSessions != null) {
      return BuildMovieSessions(movieSessions!, context);
    }
    return const SizedBox(height: 40, width: 250, child: Text('Select'));
  }

  Widget buildSinemaSessionSeats(BuildContext context) {
    if (seats != null) {
      return BuildSeat(seats!, context);
    }
    return const SizedBox(height: 40, width: 350, child: Text('Seats'));
  }

  Widget BuildMovieSessions(List<MovieSession> movies, BuildContext context) {
    return Column(children: [
      SizedBox(height: 40, width: 100, child: Text('Movie Session')),
      SizedBox(
        height: 100,
        width: 900,
        child: ListView.builder(
            shrinkWrap: true,
            scrollDirection: Axis.horizontal,
            itemCount: movies.length,
            itemBuilder: (context, rowIndex) {
              var rowSeats = movies[rowIndex];
              return SizedBox(
                  height: 150,
                  width: 450,
                  child: Column(
                    children: [
                      Text('movieId: ${rowSeats.movieId}'),
                      Text('auditoriumId: ${rowSeats.auditoriumId}'),
                      Text('Session Date: ${rowSeats.sessionDate}'),
                      TextButton(
                          style: ButtonStyle(
                            padding: MaterialStateProperty.all(
                                const EdgeInsets.symmetric(
                                    vertical: 1, horizontal: 1)),
                            foregroundColor:
                                MaterialStateProperty.all<Color>(Colors.blue),
                          ),
                          onPressed: () {
                            movieSession(rowSeats);
                          },
                          child: const Text('Select'))
                    ],
                  ));
            }),
      )
    ]);
  }

  Widget BuildMovies(List<Movie> movies, BuildContext context) {
    return Column(children: [
      SizedBox(height: 40, width: 100, child: Text('Movies')),
      SizedBox(
          height: 100,
          width: 1000,
          child: CarouselSlider(
            items: movies.map((rowSeats) {
              return SizedBox(
                height: 100,
                width: 200,
                child: Column(children: [
                  Text('Title: ${rowSeats.title}'),
                  Text('Release Date: ${rowSeats.releaseDate}'),
                  TextButton(
                      style: ButtonStyle(
                        padding: MaterialStateProperty.all(
                            const EdgeInsets.symmetric(
                                vertical: 1, horizontal: 1)),
                        foregroundColor:
                            MaterialStateProperty.all<Color>(Colors.blue),
                      ),
                      onPressed: () {
                        movieSeat(rowSeats);
                      },
                      child: const Text('Select'))
                ]),
              );
            }).toList(),
            carouselController: buttonCarouselController,
            options: CarouselOptions(
                height: 300.0,
                enableInfiniteScroll: true,
                viewportFraction: 0.4,
                enlargeCenterPage: false,
                aspectRatio: 3.0),
          )
          // child:  ListView.builder(
          //     shrinkWrap: true,
          //     scrollDirection: Axis.horizontal,
          //     itemCount: movies.length,
          //     itemBuilder: (context, rowIndex) {
          //       var rowSeats = movies[rowIndex];
          //       return SizedBox(
          //           height: 100,
          //           width: 350,
          //           child: Column(
          //             children: [
          //               Text('Title: ${rowSeats.title}'),
          //               Text('Release Date: ${rowSeats.releaseDate}'),
          //               TextButton(
          //                   style: ButtonStyle(
          //                     padding: MaterialStateProperty.all(
          //                         const EdgeInsets.symmetric(
          //                             vertical: 1, horizontal: 1)),
          //                     foregroundColor:
          //                         MaterialStateProperty.all<Color>(Colors.blue),
          //                   ),
          //                   onPressed: () {
          //                     movieSeat(rowSeats);
          //                   },
          //                   child: const Text('Select'))
          //             ],
          //           ));
          //     }),
          ),
      ElevatedButton(
        onPressed: () => buttonCarouselController.nextPage(
            duration: Duration(milliseconds: 300), curve: Curves.linear),
        child: Text('→'),
      )
    ]);
  }

  Widget BuildSeat(List<Seat> seats, BuildContext context) {
    List<List<Seat>> rows = groupBy(seats, (seat) => seat.row)
        .values
        .map((seats) =>
            seats.toList()..sort((a, b) => a.seatNumber - b.seatNumber))
        .toList();

    return Column(children: [
      SizedBox(height: 40, width: 60, child: Text('SCREEN')),
      ListView.builder(
          shrinkWrap: true,
          scrollDirection: Axis.vertical,
          itemCount: rows.length,
          itemBuilder: (context, rowIndex) {
            var rowSeats = rows[rowIndex];
            return SizedBox(
                height: 22,
                width: 600,
                // color: Colors
                //     .primaries[seat.row % Colors.primaries.length],
                child: Row(children: [
                  SizedBox(
                      height: 20,
                      width: 60,
                      child: Text('Row: ${rowSeats[0].row}')),
                  ListView.builder(
                      shrinkWrap: true,
                      scrollDirection: Axis.horizontal,
                      itemCount: rowSeats.length,
                      itemBuilder: (context, index) {
                        var seat = rowSeats[index];
                        return SizedBox(
                            height: 20,
                            width: 30,
                            child: TextButton(
                                style: ButtonStyle(
                                  padding: MaterialStateProperty.all(
                                      const EdgeInsets.symmetric(
                                          vertical: 1, horizontal: 1)),
                                  foregroundColor: seat.blocked
                                      ? MaterialStateProperty.all<Color>(
                                          Colors.grey)
                                      : MaterialStateProperty.all<Color>(
                                          Colors.blue),
                                ),
                                onPressed: () {
                                  pressSeat(seat);
                                },
                                child: Text('${seat.seatNumber}')));
                      })
                ]));
          })
    ]);
  }
}
