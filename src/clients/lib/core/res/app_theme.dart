import 'package:flutter/material.dart';

import 'app_styles.dart';

class AppTheme {
  static ThemeData lightTheme = ThemeData(
    brightness: Brightness.light,
    colorScheme: const ColorScheme.light(),
    canvasColor: const Color(0xfff1ece5),
    cardColor: const Color(0xfff1ece5),
    useMaterial3: true,
    textTheme: const TextTheme(
        bodyLarge: TextStyle(
            fontSize: AppStyles.defaultFontSize, color: Colors.black)),

    textButtonTheme: TextButtonThemeData(
        style: ButtonStyle(
      padding: MaterialStateProperty.all(
          const EdgeInsets.symmetric(vertical: 1, horizontal: 1)),
      foregroundColor: MaterialStateProperty.all<Color>(Colors.blue),
    )),
  );

  static ThemeData darkTheme = ThemeData(
    brightness: Brightness.dark,
    colorScheme: const ColorScheme.dark(),
    canvasColor: Colors.black12,
    cardColor: Colors.black12,
    useMaterial3: true,
    textTheme: const TextTheme(
        bodyLarge: TextStyle(
            fontSize: AppStyles.defaultFontSize, color: Colors.white)),
    textButtonTheme: TextButtonThemeData(
        style: ButtonStyle(
          padding: MaterialStateProperty.all(
              const EdgeInsets.symmetric(vertical: 1, horizontal: 1)),
          foregroundColor: MaterialStateProperty.all<Color>(Colors.lightBlueAccent),
        )),
  );
}

extension CustomThemeData on ThemeData {
  Color get defaultBorderColor {
    if (this.brightness == Brightness.light) {
      return Colors.black12;
    } else {
      return Colors.black87;
    }
  }

  Color get primaryBackgroundColor {
    if (this.brightness == Brightness.light) {
      return const Color(0xfff1ece5);
    } else {
      return Colors.black54;
    }
  }

  Color get widgetColor {
    if (this.brightness == Brightness.light) {
      return Colors.white;
    } else {
      return Colors.black54;
    }
  }

  Color get primaryMenuColor {
    if (this.brightness == Brightness.light) {
      return Colors.blue;
    } else {
      return Colors.lightBlueAccent;
    }
  }
}
