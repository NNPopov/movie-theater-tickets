import 'dart:async' as domain;
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class GlobalisationCubit extends Cubit<LanguagenStatus> {
  GlobalisationCubit() : super(CurrentLanguagenStatus('en')) {}

  domain.Future<void> setLanguage(String lang) async {
    emit(CurrentLanguagenStatus(lang));
  }
}

abstract class LanguagenStatus extends Equatable {
  const LanguagenStatus(this.language);

  final String language;

  @override
  List<Object> get props => [language];
}

class CurrentLanguagenStatus extends LanguagenStatus {
  const CurrentLanguagenStatus(super.language);
}
