import 'dart:async' as domain;
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';


class ThemeCubit extends Cubit<ThemeCubitState> {
  ThemeCubit() : super(const ThemeCubitState(false));

  domain.Future<void> setTheme(bool isDark) async {
    emit(ThemeCubitState(isDark));
  }
}

class ThemeCubitState extends Equatable {
  const ThemeCubitState(this.isDark);

  final bool isDark;

  @override
  List<Object> get props => [isDark];
}

