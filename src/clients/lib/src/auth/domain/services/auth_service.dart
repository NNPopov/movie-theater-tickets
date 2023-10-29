import '../../../../core/utils/typedefs.dart';
import '../abstruction/auth_event_bus.dart';

abstract class AuthService{

  ResultFuture<String> getJwtToken();

  ResultFuture<void> setJwtToken(String token);

  ResultFuture<AuthStatus> getCurrentStatus();

  ResultFuture<AuthStatus> logIn();

  ResultFuture<AuthStatus> logOut();
}


