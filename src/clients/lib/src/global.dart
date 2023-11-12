import 'package:bloc/bloc.dart';
import '../core/bloc/bloc_event_state_observer.dart';
class Global {


  static Future init() async {

    Bloc.observer = BlocEventStateObserver();

  }

}