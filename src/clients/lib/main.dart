import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:movie_theater_tickets/src/globalisations_flutter/cubit/globalisation_cubit.dart';
import 'package:movie_theater_tickets/src/global.dart';
import 'package:movie_theater_tickets/src/home/presentation/widgets/home_app_bar.dart';
import 'package:movie_theater_tickets/src/hub/presentation/cubit/connectivity_bloc.dart';
import 'package:movie_theater_tickets/src/hub/presentation/widgens/connectivity_widget.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import 'core/common/widgets/overlay_dialog.dart';
import 'core/services/router.main.dart';
import 'injection_container.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:logging/logging.dart';

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
  Logger.root.level = Level.ALL;

  Logger.root.onRecord.listen((LogRecord rec) {
    print('${rec.level.name}: ${rec.time}: ${rec.message}');
  });

  final log = Logger("App");

  FlutterError.onError = (details) {
    log.log(Level.ALL, details.exceptionAsString(), details.exception,
        details.stack);
  };

  runZonedGuarded(
    () async {
     await dotenv.load();

     await Global.init();

     await initializeDependencies();

      runApp(MyApp());
    },
    (error, stackTrace) =>
        log.log(Level.ALL, error.toString(), error, stackTrace),
  );

  // runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  MyApp({super.key});

  OverlayEntry? _overlayEntry;

  OverlayEntry? _disconnectedOverlayEntry;

  @override
  Widget build(BuildContext context) {


    return BlocProvider<ConnectivityBloc>(
      create: (_) => ConnectivityBloc(getIt.get()),
      child: MultiBlocProvider(
        providers: [
          BlocProvider<AuthBloc>(create: (_) => AuthBloc(getIt.get())),
          BlocProvider<GlobalisationCubit>(create: (_) => GlobalisationCubit()),
          BlocProvider<ShoppingCartCubit>(
            //create: (_)=>//getIt.get(),
            create: (context) => ShoppingCartCubit(
              getIt.get(),
              getIt.get(),
              getIt.get(),
              getIt.get(),
              getIt.get(),
              getIt.get(),
              getIt.get(),
              //getIt.get(),
              //getIt.get(),
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
                  colorScheme:
                      ColorScheme.fromSeed(seedColor: Colors.deepPurple),
                  useMaterial3: true,
                  textTheme: const TextTheme(
                      bodyLarge: TextStyle(fontSize: 8.0, color: Colors.black)),
                ),
                home: BlocListener<ConnectivityBloc, ConnectivityState>(
                  child: const Scaffold(
                    backgroundColor: Colors.white,
                    appBar: HomeAppBar(),
                    body: Navigator(
                      onGenerateRoute: generateRoute,
                    ),
                  ),
                  listener: (context, state) {
                    if (state is ReconnectingState) {
                      _overlayEntry = OverlayEntry(
                        builder: (context) {
                          return OverlayDialog(
                            header: Text(
                                AppLocalizations.of(context)!
                                    .reconnecting_notification_text,
                                style: TextStyle(
                                  fontSize: 16,
                                  color: Colors.black87,
                                  decoration: TextDecoration.none,
                                )),
                            body: const SizedBox(
                              width: 50,
                              height: 50,
                              child: CircularProgressIndicator(),
                            ),
                          );
                        },
                      );
                      Overlay.of(
                        context,
                      ).insert(_overlayEntry!);
                    } else {
                      _overlayEntry?.remove();
                      _overlayEntry = null;
                    }

                    var connectivityBloc = context.read<ConnectivityBloc>();

                    if (state is DisconnectedState) {
                      _disconnectedOverlayEntry?.remove();
                      _disconnectedOverlayEntry = null;

                      _disconnectedOverlayEntry = OverlayEntry(
                          maintainState: true,
                          builder: (context) {
                            return OverlayDialog(
                                header: Text(
                                    AppLocalizations.of(context)!
                                        .connection_lost_notification_text,
                                    style: const TextStyle(
                                      fontSize: 16,
                                      color: Colors.black87,
                                      decoration: TextDecoration.none,
                                    )),
                                body: Container(
                                  width: 120,
                                  height: 50,
                                  child: TextButton(
                                    style: ButtonStyle(
                                      padding: MaterialStateProperty.all(
                                          const EdgeInsets.symmetric(
                                              vertical: 1, horizontal: 1)),
                                      foregroundColor:
                                          MaterialStateProperty.all<Color>(
                                              Colors.blue),
                                    ),
                                    onPressed: () {
                                      connectivityBloc.connect();
                                      _disconnectedOverlayEntry?.remove();
                                      _disconnectedOverlayEntry = null;
                                    },
                                    child: Text(AppLocalizations.of(context)!
                                        .connection_lost_reconnect_btn),
                                  ),
                                ));
                          });

                      Overlay.of(
                        context,
                      ).insert(_disconnectedOverlayEntry!);
                    }
                  },
                ));
          },
        ),
      ),
    );
  }
}
