import 'package:dio/dio.dart';
import 'package:get_it/get_it.dart';
import 'package:movie_theater_tickets/src/cinema_halls/data/repo/cinema_hall_repo_impl.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/repo/cinema_hall_repo.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/usecases/get_cinema_hall.dart';
import 'package:movie_theater_tickets/src/cinema_halls/presentation/cubit/cinema_hall_cubit.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/usecase/get_movie_sessions.dart';
import 'package:movie_theater_tickets/src/movie_sessions/presentation/cubit/movie_session_cubit.dart';
import 'package:movie_theater_tickets/src/movies/domain/usecases/get_movie_by_id.dart';
import 'package:movie_theater_tickets/src/movies/domain/usecases/get_movies.dart';
import 'package:movie_theater_tickets/src/movies/presentation/app/movie_theater_cubit.dart';
import 'package:movie_theater_tickets/src/seats/data/repos/seat_repo_impl.dart';
import 'package:movie_theater_tickets/src/seats/domain/repos/seat_repo.dart';
import 'apis/http_client.dart';
import 'src/movies/data/repos/movie_repo_impl.dart';
import 'src/movies/domain/repos/movie_repo.dart';
import 'src/movie_sessions/data/repos/movie_session_repo_impl.dart';
import 'src/movie_sessions/domain/repos/movie_session_repo.dart';
import 'src/shopping_carts/data/repos/shopping_cart_repo_impl.dart';
import 'src/shopping_carts/domain/repos/shopping_cart_repo.dart';

final getIt = GetIt.instance;

Future<void> initializeDependencies() async {
  // Dio
  getIt.registerSingleton<Dio>(Client().init());




  _initMovie();
  _initCinemaHall();
  _initMovieSession();

  // Dependencies

  getIt.registerLazySingleton<ShoppingCartRepo>(() => ShoppingCartRepoImpl());
  getIt.registerLazySingleton<SeatRepo>(() => SeatRepoImpl());

  // //UseCases
  // sl.registerSingleton<GetCharacterUseCase>(
  //     GetCharacterUseCase(sl())
  // );
  //
  // //Blocs
  // sl.registerFactory<RemoteCharactersBloc>(
  //         ()=> RemoteCharactersBloc(sl())
  // );
}
void _initCinemaHall() {
  getIt.registerFactory(() => CinemaHallCubit(getCinemaHall:  getIt()));
  getIt.registerLazySingleton<CinemaHallRepo>(() =>   CinemaHallRepoImpl());
  getIt.registerLazySingleton<GetCinemaHallById>(() => GetCinemaHallById());
}

void _initMovieSession() {
  getIt.registerFactory(() => MovieSessionCubit(getMovieSessions: getIt()));
  getIt.registerLazySingleton<GetMovieSessions>(() => GetMovieSessions());
  getIt.registerLazySingleton<MovieSessionRepo>(() => MovieSessionRepoImpl());
}

void _initMovie() {
  getIt.registerFactory(
      () => MovieTheaterCubit(getMovies: getIt(), getMovieById: getIt()));

  getIt.registerLazySingleton<GetMovieById>(() => GetMovieById());
  getIt.registerLazySingleton<GetMovies>(() => GetMovies());
  getIt.registerLazySingleton<MovieRepo>(() => MovieRepoImpl());
}
