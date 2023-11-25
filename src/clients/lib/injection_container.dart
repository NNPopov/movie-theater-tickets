import 'package:dio/dio.dart';
import 'package:get_it/get_it.dart';
import 'package:movie_theater_tickets/src/auth/data/services/auth_service_impl.dart';
import 'package:movie_theater_tickets/src/auth/data/services/flutter_web_auth_2_authenticator.dart';
import 'package:movie_theater_tickets/src/auth/domain/abstraction/authenticator.dart';
import 'package:movie_theater_tickets/src/auth/domain/services/auth_service.dart';
import 'package:movie_theater_tickets/src/cinema_halls/data/repo/cinema_hall_repo_impl.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/repo/cinema_hall_repo.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/usecases/get_cinema_hall.dart';
import 'package:movie_theater_tickets/src/hub/data/signalr_event_hub.dart';
import 'package:movie_theater_tickets/src/hub/domain/event_hub.dart';
import 'package:movie_theater_tickets/core/buses/event_bus.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/usecase/get_movie_sessions.dart';
import 'package:movie_theater_tickets/src/movies/domain/usecases/get_movie_by_id.dart';
import 'package:movie_theater_tickets/src/movies/domain/usecases/get_movies.dart';
import 'package:movie_theater_tickets/src/seats/data/repos/seat_repo_impl.dart';
import 'package:movie_theater_tickets/src/seats/domain/repos/seat_repo.dart';
import 'package:movie_theater_tickets/src/seats/domain/usecases/get_cinema_hall_info.dart';
import 'package:movie_theater_tickets/src/seats/domain/usecases/get_seats_by_movie_session_id.dart';
import 'package:movie_theater_tickets/src/seats/domain/usecases/update_seats_sate_usecase.dart';
import 'package:movie_theater_tickets/src/shopping_carts/data/repos/shopping_cart_local_repo_impl.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/repos/shopping_cart_local_repo.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/services/shopping_cart_service.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/assign_client_use_case.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/create_shopping_cart.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/get_shopping_cart.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/reserve_seats.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/select_seat.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/shopping_cart_subscribe.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/unselect_seat.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/update_state_shopping_cart.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import 'core/apis/auth-interceptor.dart';
import 'core/apis/http_client.dart';
import 'src/movies/data/repos/movie_repo_impl.dart';
import 'src/movies/domain/repos/movie_repo.dart';
import 'src/movie_sessions/data/repos/movie_session_repo_impl.dart';
import 'src/movie_sessions/domain/repos/movie_session_repo.dart';
import 'src/shopping_carts/data/repos/shopping_cart_repo_impl.dart';
import 'src/shopping_carts/domain/repos/shopping_cart_repo.dart';

final getIt = GetIt.instance;

Future<void> initializeDependencies() async {
  // getIt
  //   ..registerFactory<AuthService>(
  //       () => AuthServiceImpl(authenticator: getIt()))
  //   ..registerLazySingleton<Authenticator>(
  //       () => FlutterWebAuth2Authenticator());


  getIt.registerLazySingleton<AuthService>(() => AuthServiceImpl(authenticator: getIt()));
  getIt.registerLazySingleton<Authenticator>(
          () => FlutterWebAuth2Authenticator());
  // getIt
  //   ..registerFactory<ShoppingCartCubit>(
  //           () => ShoppingCartCubit(
  //             getIt.get(),
  //             getIt.get(),
  //             getIt.get(),
  //             getIt.get(),
  //             getIt.get(),
  //             getIt.get(),
  //             getIt.get(),
  //             getIt.get(),
  //             getIt.get(),
  //           ));


  getIt.registerLazySingleton<EventBus>(() => EventBus());

  // Dio

  _initSeats();
  _initMovie();
  _initCinemaHall();
  _initMovieSession();
  _initShoppingCart();
  // Dependencies




  getIt.registerLazySingleton<EventHub>(() => SignalREventHub(
      updateShoppingCartState: getIt.get(), updateSeatsState: getIt.get()));

  //UseCases

  getIt.get<EventHub>().subscribe();

  getIt
    ..registerFactory<Dio>(() => Client(getIt.get()).init())
    ..registerLazySingleton<AuthInterceptor>(() => AuthInterceptor());

  getIt.get<ShoppingCartAuthListener>().init();
}

void _initShoppingCart() {
  getIt.registerLazySingleton<ShoppingCartAuthListener>(
      () => ShoppingCartAuthListener(getIt.get(), getIt.get(), getIt.get()));

  getIt.registerLazySingleton<ShoppingCartLocalRepo>(
      () => ShoppingCartLocalRepoImpl());

  getIt.registerLazySingleton<GetCinemaHallInfo>(
      () => GetCinemaHallInfo(getIt.get(), getIt.get()));
  getIt.registerLazySingleton<ReserveSeatsUseCase>(
      () => ReserveSeatsUseCase(getIt.get(), getIt.get(), getIt.get()));
  getIt.registerLazySingleton<ShoppingCartRepo>(() => ShoppingCartRepoImpl());
  getIt.registerLazySingleton<CreateShoppingCartUseCase>(() =>
      CreateShoppingCartUseCase(
          getIt.get(), getIt.get(), getIt.get(), getIt.get()));
  getIt.registerLazySingleton<GetShoppingCartUseCase>(() =>
      GetShoppingCartUseCase(
          getIt.get(), getIt.get(), getIt.get(), getIt.get(), getIt.get()));
  getIt.registerLazySingleton<SelectSeatUseCase>(
      () => SelectSeatUseCase(getIt.get(), getIt.get()));
  getIt.registerLazySingleton<UnselectSeatUseCase>(
      () => UnselectSeatUseCase(getIt.get(), getIt.get()));
  getIt.registerLazySingleton<ShoppingCartUpdateSubscribeUseCase>(
      () => ShoppingCartUpdateSubscribeUseCase(eventHub: getIt.get()));
  getIt.registerLazySingleton<ShoppingCartUpdateStateUseCase>(
      () => ShoppingCartUpdateStateUseCase(getIt.get(), getIt.get()));
  getIt.registerLazySingleton<AssignClientUseCase>(
      () => AssignClientUseCase(getIt.get(), getIt.get(), getIt.get()));
}

void _initSeats() {
  getIt.registerLazySingleton<UpdateSeatsStateUseCase>(
      () => UpdateSeatsStateUseCase(getIt.get()));
  getIt.registerLazySingleton<SeatRepo>(() => SeatRepoImpl());
  getIt.registerLazySingleton<GetSeatsByMovieSessionId>(
      () => GetSeatsByMovieSessionId(getIt.get(), getIt.get()));
}

void _initCinemaHall() {
  getIt.registerLazySingleton<CinemaHallRepo>(
      () => CinemaHallRepoImpl(getIt.get()));
  getIt.registerLazySingleton<GetCinemaHallById>(
      () => GetCinemaHallById(getIt.get()));
}

void _initMovieSession() {
  getIt.registerLazySingleton<GetMovieSessions>(
      () => GetMovieSessions(getIt.get()));
  getIt.registerLazySingleton<MovieSessionRepo>(
      () => MovieSessionRepoImpl(getIt.get()));
}

void _initMovie() {
  getIt.registerLazySingleton<GetMovieById>(() => GetMovieById(getIt.get()));
  getIt.registerLazySingleton<GetMovies>(() => GetMovies(getIt.get()));
  getIt.registerLazySingleton<MovieRepo>(() => MovieRepoImpl(getIt.get()));
}
