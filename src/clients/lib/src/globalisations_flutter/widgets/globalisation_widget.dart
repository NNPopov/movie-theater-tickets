import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../main.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
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

    return DropdownButton<Locale>(
      value: dropdownValue,
      //icon: const Icon(Icons.arrow_downward),
      elevation: 16,
      style:  TextStyle(color: Colors.black),
      // underline: Container(
      //   height: 2,
      //   color: Colors.deepPurpleAccent,
      // ),
      onChanged: (Locale? value) {
        context.read<GlobalisationCubit>().setLanguage(value!.languageCode);
      },
      items: list.map<DropdownMenuItem<Locale>>((Locale value) {
        return DropdownMenuItem<Locale>(
          value: value,
          child: Text(value.languageCode),
        );
      }).toList(),
    );
  }
  @override
  void dispose() {
    super.dispose();
  }
}
