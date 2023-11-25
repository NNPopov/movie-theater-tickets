import 'dart:developer';

import 'package:bloc/bloc.dart';


class BlocEventStateObserver extends BlocObserver {

  @override
  void onEvent(Bloc<dynamic, dynamic> bloc, Object? event) {
    super.onEvent(bloc, event);
    log('onEvent $event');
  }

  @override
  void onChange(BlocBase<dynamic> bloc, Change<dynamic> change) {
    super.onChange(bloc, change);
    log('onChange $change');
  }

  @override
  void onCreate(BlocBase<dynamic> bloc)  {
    super.onCreate(bloc);
    log('onCreate $bloc');
  }

  @override
  void onClose(BlocBase<dynamic> bloc)  {
    super.onClose(bloc);
    log('onClose $bloc');
  }

  @override
  void onTransition(
      Bloc<dynamic, dynamic> bloc,
      Transition<dynamic, dynamic> transition,
      ) {
    super.onTransition(bloc, transition);
    log('onTransition $transition');
  }

  @override
  void onError(BlocBase<dynamic> bloc, Object error, StackTrace stackTrace) {
    super.onError(bloc, error, stackTrace);
    log('onError $bloc $error');
  }
}