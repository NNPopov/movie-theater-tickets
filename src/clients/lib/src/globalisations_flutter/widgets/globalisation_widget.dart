import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

import '../cubit/globalisation_cubit.dart';

class GlobalisationWidget extends StatefulWidget {
  const GlobalisationWidget({super.key});

  @override
  State<GlobalisationWidget> createState() => _GlobalisationWidget();
}

class _GlobalisationWidget extends State<GlobalisationWidget> {
  @override
  void initState() {
    super.initState();
  }

  List<Locale> list = AppLocalizations.supportedLocales;

  @override
  Widget build(BuildContext context) {
    Locale dropdownValue = list.first;
  return  BlocBuilder<GlobalisationCubit, LanguagenStatus>(
        builder: (context, lang) {
    return SizedBox(
      width: 70,
      height: 40,
      child: DropdownButton<Locale>(
        value: lang.locate,
        elevation: 16,
        style: const TextStyle(color: Colors.black),
        onChanged: (Locale? value) {
          context.read<GlobalisationCubit>().setLanguage(value!);
        },
        items: list.map<DropdownMenuItem<Locale>>((Locale value) {
          return DropdownMenuItem<Locale>(
            value: value,
            child: Text(value.languageCode),
          );
        }).toList(),
      ),
    );
    });
  }

  @override
  void dispose() {
    super.dispose();
  }
}
