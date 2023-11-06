import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/abstraction/auth_event_bus.dart';
import '../cubit/auth_cubit.dart';
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
    return BlocConsumer<AuthCubit, AuthStatus>(
      listener: (context, state) {
        // if (state is ShoppingCartError) {
        //   Utils.showSnackBar(context, state.message);
        // }
        // if (state is ShoppingCartConflictState) {
        //   Utils.showSnackBar(context, 'This place is already occupied');
        // }
      },
      // buildWhen: (context, state) {
      //   if (state is ShoppingCartError) {
      //     return false;
      //   } else {
      //     return true;
      //   }
      // },
      builder: (BuildContext context, AuthStatus state) {
        if (state is AuthorizedAuthStatus) {
          return SizedBox(
            width: 70,
            height: 40,
            child: TextButton(style: OutlinedButton.styleFrom(
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(1),
              ),
            ),
              onPressed: ()  async {
                await context.read<AuthCubit>().logOut();
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
            onPressed: () async {
              await context.read<AuthCubit>().logInt();
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
