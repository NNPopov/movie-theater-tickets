import 'dart:developer';

import 'package:bloc/bloc.dart';

import '../common/app_logger.dart';


class BlocEventStateObserver extends BlocObserver {

  final logger = getLogger(BlocEventStateObserver);


  @override
  void onEvent(Bloc<dynamic, dynamic> bloc, Object? event) {
    super.onEvent(bloc, event);
    logger.d('onEvent $event');
  }

  @override
  void onChange(BlocBase<dynamic> bloc, Change<dynamic> change) {
    super.onChange(bloc, change);
    logger.d('onChange $change');
  }

  @override
  void onCreate(BlocBase<dynamic> bloc)  {
    super.onCreate(bloc);
    logger.d('onCreate $bloc');
  }

  @override
  void onClose(BlocBase<dynamic> bloc)  {
    super.onClose(bloc);
    logger.d('onClose $bloc');
  }

  @override
  void onTransition(
      Bloc<dynamic, dynamic> bloc,
      Transition<dynamic, dynamic> transition,
      ) {
    super.onTransition(bloc, transition);
    logger.d('onTransition $transition');
  }

  @override
  void onError(BlocBase<dynamic> bloc, Object error, StackTrace stackTrace) {
    super.onError(bloc, error, stackTrace);
    logger.d('onError $bloc $error');
  }
}