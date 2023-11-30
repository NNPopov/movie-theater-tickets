import '../../../../core/utils/typedefs.dart';
import '../abstraction/auth_statuses.dart';

abstract class AuthService{

  ResultFuture<String> getJwtToken();

  ResultFuture<AuthStatus> getCurrentStatus();

  ResultFuture<AuthStatus> logIn();

  ResultFuture<AuthStatus> logOut();

  Stream<AuthStatus> get status;
}


