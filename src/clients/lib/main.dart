import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:movie_theater_tickets/src/globalisations_flutter/cubit/globalisation_cubit.dart';
import 'package:movie_theater_tickets/src/global.dart';
import 'package:movie_theater_tickets/src/home/presentation/widgets/home_app_bar.dart';
import 'package:movie_theater_tickets/src/hub/presentation/cubit/connectivity_bloc.dart';
import 'package:movie_theater_tickets/src/hub/presentation/widgens/connectivity_safe_area_widget.dart';
import 'package:movie_theater_tickets/src/server_state/presentation/cubit/server_state_cubit.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import 'core/common/app_logger.dart';
import 'core/services/router.main.dart';
import 'injection_container.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:logger/logger.dart';
//import 'package:logging/logging.dart';

import 'src/auth/presentations/bloc/auth_cubit.dart';

// import 'package:logger/logger.dart';
//
// var logger = Logger(
//   printer: PrettyPrinter(),
// );

// var loggerNoStack = Logger(
//   printer: PrettyPrinter(methodCount: 0),
// );

final getIt = GetIt.instance;

Future<void> main() async {
  // Logger.root.level = Level.ALL;
  //
  // Logger.root.onRecord.listen((LogRecord rec) {
  //   print('${rec.level.name}: ${rec.time}: ${rec.message}');
  // });

 // final log = Logger("App");
  final logger = getLogger(main);

  FlutterError.onError = (details) {
      logger.log(Level.all, details.exceptionAsString(), error: details.exception,
        stackTrace: details.stack);
  };

  runZonedGuarded(
    () async {
      await dotenv.load();

      await Global.init();

      await initializeDependencies();

      runApp(MyApp());
    },
    (error, stackTrace) =>
        logger.w( error.toString(), error: error,
            stackTrace: stackTrace)
  );
}

class MyApp extends StatelessWidget {
  MyApp({super.key});

  final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

  // @override
  // GlobalKey<NavigatorState> get navigatorKey =>  GlobalKey<NavigatorState>();

  @override
  Widget build(BuildContext context) {
    return BlocProvider<ConnectivityBloc>(
      create: (_) => ConnectivityBloc(getIt.get()),
      child: MultiBlocProvider(
        providers: [
          BlocProvider<AuthBloc>(create: (_) => AuthBloc(getIt.get())),
          BlocProvider<GlobalisationCubit>(create: (_) => GlobalisationCubit()),
          BlocProvider<ServerStateCubit>(create: (_) => ServerStateCubit(getIt.get())),
          BlocProvider<ShoppingCartCubit>(
            create: (context) => ShoppingCartCubit(
              getIt.get(),
              getIt.get(),
              getIt.get(),
              getIt.get(),
              getIt.get(),
              getIt.get(),
              getIt.get(),
            ),
          ),
        ],
        child: BlocBuilder<GlobalisationCubit, LanguagenStatus>(
          builder: (context, lang) {
            return MaterialApp(
              title: 'Flutter Demo',
              localizationsDelegates: const [
                AppLocalizations.delegate,
                GlobalMaterialLocalizations.delegate,
                GlobalWidgetsLocalizations.delegate,
                GlobalCupertinoLocalizations.delegate,
              ],
              locale: lang.locate,
              supportedLocales: AppLocalizations.supportedLocales,
              theme: ThemeData(
                colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
                useMaterial3: true,
                textTheme: const TextTheme(
                    bodyLarge: TextStyle(fontSize: 8.0, color: Colors.black)),
              ),
              home: ConnectivitySafeAreaWidget(
                child: Scaffold(
                  backgroundColor: Colors.white,
                  appBar: HomeAppBar(navigatorKey),
                  body: Navigator(
                    key: navigatorKey,
                    onGenerateRoute: generateRoute,
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}
