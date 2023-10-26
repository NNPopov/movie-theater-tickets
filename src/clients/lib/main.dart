import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'core/services/router.main.dart';
import 'injection_container.dart';
import 'package:get_it/get_it.dart';


final getIt = GetIt.instance;

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
      //     BlocProvider<ShoppingCartCubit>(
      //         create: (_) => getIt.get<ShoppingCartCubit>())
      //   ],
      //   child:
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
      //);
  }
}
