import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

import '../cubit/theme_cubit.dart';

class ThemeWidget extends StatefulWidget {
  const ThemeWidget({super.key});

  @override
  State<ThemeWidget> createState() => _ThemeWidget();
}

class _ThemeWidget extends State<ThemeWidget> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<ThemeCubit, ThemeCubitState>(builder: (context, lang) {
      return SizedBox(
        width: 80,
        height: 40,
        child: DropdownButton<bool>(
          value: lang.isDark,
          elevation: 16,
          onChanged: (bool? value) {
            context.read<ThemeCubit>().setTheme(value!);
          },
          items: [
            DropdownMenuItem<bool>(
              value: true,
              child: Text(AppLocalizations.of(context)!.dark_theme),
            ),
            DropdownMenuItem<bool>(
              value: false,
              child: Text(AppLocalizations.of(context)!.light_theme),
            ),
          ],
        ),
      );
    });
  }

  @override
  void dispose() {
    super.dispose();
  }
}
