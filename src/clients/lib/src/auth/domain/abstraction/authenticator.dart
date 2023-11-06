

import '../../../../core/utils/typedefs.dart';

abstract class Authenticator {

  ResultFuture<String> logIn();
}