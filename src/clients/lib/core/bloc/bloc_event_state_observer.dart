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

    var message = change.toString();
    logger.d(
        'onChange ${message.substring(0, message.length > 50 ? 50 : message.length)}');
  }

  @override
  void onCreate(BlocBase<dynamic> bloc) {
    super.onCreate(bloc);
    logger.d('onCreate $bloc');
  }

  @override
  void onClose(BlocBase<dynamic> bloc) {
    super.onClose(bloc);
    logger.d('onClose $bloc');
  }

  @override
  void onTransition(
    Bloc<dynamic, dynamic> bloc,
    Transition<dynamic, dynamic> transition,
  ) {
    super.onTransition(bloc, transition);

    var message = transition.toString();
    logger.d(
        'onTransition ${message.substring(0, message.length > 50 ? 50 : message.length)}');
  }

  @override
  void onError(BlocBase<dynamic> bloc, Object error, StackTrace stackTrace) {
    super.onError(bloc, error, stackTrace);
    logger.d('onError $bloc $error');
  }
}
