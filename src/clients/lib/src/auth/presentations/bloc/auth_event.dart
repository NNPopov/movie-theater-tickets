import 'package:equatable/equatable.dart';

sealed class AuthEvent extends Equatable {
  @override
  List<Object?> get props => [];
}

class LogInEvent extends AuthEvent {
  @override
  List<Object?> get props => [];
}

class LogOutEvent extends AuthEvent {
  @override
  List<Object?> get props => [];
}
