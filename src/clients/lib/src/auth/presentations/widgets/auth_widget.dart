import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/abstruction/auth_event_bus.dart';
import '../cubit/auth_cubit.dart';


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

  Future<void> logIn() async {
    await context.read<AuthCubit>().logInt();
  }

  Future<void> logOut() async {
    await context.read<AuthCubit>().logOut();
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
            width: 60,
            height: 60,
            child: TextButton(
              onPressed: ()  async {
                await logOut();
              },
              child: const Text('Log Out'),
            ),
          );
        }

        return SizedBox(
          width: 60,
          height: 60,
          child: TextButton(
            onPressed: () async {
              await logIn();
            },
            child: const Text('Log In'),
          ),
        );
      });
  }
  @override
  void dispose() {
    super.dispose();
  }
}
