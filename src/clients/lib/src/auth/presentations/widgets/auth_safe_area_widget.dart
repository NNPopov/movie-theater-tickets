import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/abstraction/auth_statuses.dart';
import '../bloc/auth_cubit.dart';

class AuthSafeAreaWidget extends StatelessWidget {
  const AuthSafeAreaWidget(
      {super.key,
        required this.notAuthenticated,
        required this.authenticated});

  final Widget authenticated;
  final Widget notAuthenticated;


  @override
  Widget build(BuildContext context) {
    return               BlocBuilder<AuthBloc, AuthStatus>(
        builder: (BuildContext context, AuthStatus authStatus) {
          if (authStatus.status == AuthenticationStatus.authorized) {
            return authenticated;
          }
          return notAuthenticated;

        });
  }
}
