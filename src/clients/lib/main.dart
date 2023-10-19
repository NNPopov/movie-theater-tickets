import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:movie_theater_tickets/src/cinema_halls/presentation/cubit/cinema_hall_cubit.dart';
import 'package:movie_theater_tickets/src/movie_sessions/presentation/cubit/movie_session_cubit.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_cubit.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import 'core/services/router.main.dart';
import 'injection_container.dart';
import 'package:get_it/get_it.dart';
import 'src/movies/presentation/app/movie_theater_cubit.dart';
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
    return
      // MultiBlocProvider(
      //   providers: [
      //     BlocProvider<MovieTheaterCubit>(
      //         create: (context) => MovieTheaterCubit()),
      //     BlocProvider<MovieSessionCubit>(
      //         create: (context) => MovieSessionCubit()),
      //     BlocProvider<CinemaHallCubit>(create: (context) => CinemaHallCubit()),
      //     BlocProvider<SeatCubit>(
      //         create: (context) => SeatCubit(
      //             shoppingCartCubit:
      //                 BlocProvider.of<ShoppingCartCubit>(context))),
      //     BlocProvider<ShoppingCartCubit>(
      //         create: (context) => ShoppingCartCubit())
      //   ],
       // child:
    MaterialApp(
          title: 'Flutter Demo',
          theme: ThemeData(
            colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
            useMaterial3: true,
            textTheme: const TextTheme(
                bodyLarge: TextStyle(fontSize: 8.0, color: Colors.black)),
          ),
          onGenerateRoute: generateRoute,
        //)
    );
  }
}
