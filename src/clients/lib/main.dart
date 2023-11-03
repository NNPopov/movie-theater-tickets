import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:movie_theater_tickets/src/globalisations_flutter/cubit/globalisation_cubit.dart';
import 'package:movie_theater_tickets/src/gobal.dart';
import 'core/services/router.main.dart';
import 'injection_container.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

final getIt = GetIt.instance;

void main() async {
  await Global.init();
  await dotenv.load();

  await initializeDependencies();

  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
        create: (context) => GlobalisationCubit(),
        child: BlocBuilder<GlobalisationCubit, LanguagenStatus>(
            builder: (context, lang) {
          return MaterialApp(
            title: 'Flutter Demo',
            localizationsDelegates: [
              AppLocalizations.delegate,
              GlobalMaterialLocalizations.delegate,
              GlobalWidgetsLocalizations.delegate,
              GlobalCupertinoLocalizations.delegate,
            ],
            locale: Locale(lang.language),
            supportedLocales: AppLocalizations.supportedLocales,
            theme: ThemeData(
              colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
              useMaterial3: true,
              textTheme: const TextTheme(
                  bodyLarge: TextStyle(fontSize: 8.0, color: Colors.black)),
            ),
            onGenerateRoute: generateRoute,
            //)
          );
          //);
        }));
  }
}
