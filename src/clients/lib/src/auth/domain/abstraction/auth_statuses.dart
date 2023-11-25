import 'package:equatable/equatable.dart';
import 'package:flutter/material.dart';

@immutable
 class AuthStatus extends Equatable {
    AuthStatus({required this.status, this.errorMessage});

  final AuthenticationStatus status;
  late String? errorMessage;

  AuthStatus copyWith({
    AuthenticationStatus? status,
    String? errorMessage,
  }) {
    return AuthStatus(
      status: status ?? this.status,
      errorMessage: errorMessage,
    );
  }

  @override
  List<Object?> get props => [status,errorMessage];
}

enum AuthenticationStatus{

  authorized,
  unauthorized,
  expired,
  inProgress
}