import 'package:dio/dio.dart';
import 'package:get_it/get_it.dart';
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

  //sl.registerSingleton<ThemeBloc>(ThemeBloc());

  // Dio
  getIt.registerSingleton<Dio>(Client().init());

  // Dependencies
  getIt.registerLazySingleton<MovieRepo>(() =>MovieRepoImpl());
  getIt.registerLazySingleton<MovieSessionRepo>(() =>MovieSessionRepoImpl());
  getIt.registerLazySingleton<ShoppingCartRepo>(() =>ShoppingCartRepoImpl());
  getIt.registerLazySingleton<SeatRepo>(() =>SeatRepoImpl());

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