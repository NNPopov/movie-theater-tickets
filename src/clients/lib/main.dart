import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:dartz/dartz.dart' as d;
import 'core/errors/failures.dart';
import 'injection_container.dart';
import 'src/movies/domain/repos/movie_repo.dart';
import 'package:get_it/get_it.dart';
import 'package:collection/collection.dart';

import 'src/seats/domain/entities/seat.dart';
import 'src/seats/domain/repos/seat_repo.dart';

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
    return MaterialApp(
      title: 'Flutter Demo',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
        useMaterial3: true,
      ),
      home: const MyHomePage(title: 'Flutter Demo Home Page'),
    );
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

  Future<void> _incrementCounter() async {
    //Client client = Client();

    MovieRepo movieRepo = sl.get<MovieRepo>();

    // MovieRepo movieRepo =   MovieRepoImpl(client.init());
    var movies = await movieRepo.getMovies();
  }


  Future<void> pressSeat(Seat seat) async {
    //Client client = Client();
var tt = seat;
    MovieRepo movieRepo = sl.get<MovieRepo>();

    // MovieRepo movieRepo =   MovieRepoImpl(client.init());
    var movies = await movieRepo.getMovies();
  }

  @override
  Widget build(BuildContext context) {
    SeatRepo seatRepo = sl.get<SeatRepo>();
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

  Scaffold BuildSeat(List<Seat> seats, BuildContext context) {

    List<List<Seat>> rows = groupBy(seats, (seat) => seat.row)
        .values
        .map((seats) =>
            seats.toList()..sort((a, b) => a.seatNumber - b.seatNumber))
        .toList();

    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        title: Text('Tets'),
      ),
      body:Column( children:[
        SizedBox(
            height: 100,
            width: 60,

            child:Text('SCREEN')),
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
                child:Row(children:[SizedBox(
                    height: 20,
                    width: 60,

                    child:Text('Row: ${rowSeats[0].row}')),

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
            ])
            );
          })
      ]),


      floatingActionButton: FloatingActionButton(
        onPressed: _incrementCounter,
        tooltip: 'Increment',
        child: const Icon(Icons.add),
      ), // This trailing comma makes auto-formatting nicer for build methods.
    );
  }
}
