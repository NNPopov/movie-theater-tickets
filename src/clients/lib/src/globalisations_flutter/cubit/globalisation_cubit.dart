import 'dart:async' as domain;
import 'dart:ui';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class GlobalisationCubit extends Cubit<LanguagenStatus> {
  GlobalisationCubit() : super(const CurrentLanguagenStatus(Locale('en'))) {}

  domain.Future<void> setLanguage(Locale lang) async {
    emit(CurrentLanguagenStatus(lang));
  }
}

abstract class LanguagenStatus extends Equatable {
  const LanguagenStatus(this.locate);

  final Locale locate;

  @override
  List<Object> get props => [locate];
}

class CurrentLanguagenStatus extends LanguagenStatus {
  const CurrentLanguagenStatus(super.locate);
}
