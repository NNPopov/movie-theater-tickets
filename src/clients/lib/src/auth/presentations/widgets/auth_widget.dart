import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:movie_theater_tickets/src/auth/presentations/bloc/auth_event.dart';

import '../../domain/abstraction/auth_statuses.dart';
import '../bloc/auth_cubit.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class AuthWidget extends StatefulWidget {
  const AuthWidget({super.key});

  @override
  State<AuthWidget> createState() => _AuthWidget();
}

class _AuthWidget extends State<AuthWidget> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<AuthBloc, AuthStatus>(
      builder: (BuildContext context, AuthStatus state) {
        print('tatus resived $state.status');

        if (state.status == AuthenticationStatus.authorized) {
          return SizedBox(
            width: 70,
            height: 40,
            child: TextButton(style: OutlinedButton.styleFrom(
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(1),
              ),
            ),
              onPressed: ()   {
                 context.read<AuthBloc>().add(LogOutEvent());
              },
              child:  Text(AppLocalizations.of(context)!.sing_out),
            ),
          );
        }

        return SizedBox(
          width: 70,
          height: 40,
          child: TextButton(style: OutlinedButton.styleFrom(
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(1),
            ),
          ),
            onPressed: ()  {
               context.read<AuthBloc>().add(LogInEvent());
            },
            child:  Text(AppLocalizations.of(context)!.sing_in),
          ),
        );
      });
  }
  @override
  void dispose() {
    super.dispose();
  }
}
