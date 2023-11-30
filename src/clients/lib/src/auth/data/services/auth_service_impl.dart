import 'dart:async';
import 'dart:convert';

import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../../domain/abstraction/auth_statuses.dart';
import '../../domain/abstraction/authenticator.dart';
import '../../domain/services/auth_service.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:jwt_decoder/jwt_decoder.dart';

class AuthServiceImpl implements AuthService {
  AuthServiceImpl({required this.authenticator});

  final Authenticator authenticator;

  final storage = const FlutterSecureStorage();

  final _controller = StreamController<AuthStatus>.broadcast();

  late AuthStatus authStatus =
      AuthStatus(status: AuthenticationStatus.unauthorized);

  @override
  Stream<AuthStatus> get status async* {
    yield* _controller.stream;
  }


  @override
  ResultFuture<String> getJwtToken() async {
    var token = await storage.read(key: Constants.TOKEN_KEY);

    if (token == null) {
      if (authStatus.status != AuthenticationStatus.unauthorized) {
        authStatus =
            authStatus.copyWith(status: AuthenticationStatus.unauthorized);
        _controller.add(authStatus);
      }


      return const Left(NotAuthorisedException());
    }

    bool hasExpired = JwtDecoder.isExpired(token);

    if (hasExpired) {
      authStatus = authStatus.copyWith(status: AuthenticationStatus.expired);

      if (authStatus.status != AuthenticationStatus.unauthorized ||
          authStatus.status != AuthenticationStatus.expired) {
        authStatus = authStatus.copyWith(status: AuthenticationStatus.expired);
        _controller.add(authStatus);
        await logOut();

        print('AuthStatus changed to $authStatus');
        return left(const NotAuthorisedException());
      }
    }
    return Right(token);
  }


  @override
  ResultFuture<AuthStatus> getCurrentStatus() async {
    await getJwtToken();

    return Right(authStatus);
  }

  @override
  ResultFuture<AuthStatus> logIn() async {
    var authResult = await authenticator.logIn();

    return authResult.fold((l) => Left(l), (r) async {
      final accessToken = jsonDecode(r)['access_token'] as String;

      await storage.write(key: Constants.TOKEN_KEY, value: accessToken);

      authStatus = authStatus.copyWith(status: AuthenticationStatus.authorized);

      _controller.add(authStatus);
      return Right(authStatus);
    });
  }

  @override
  ResultFuture<AuthStatus> logOut() async {

    if(authStatus.status == AuthenticationStatus.authorized || authStatus.status == AuthenticationStatus.expired) {
      await storage.delete(key: Constants.TOKEN_KEY);

      authStatus =
          authStatus.copyWith(status: AuthenticationStatus.unauthorized);

      _controller.add(authStatus);

      print('AuthStatus changed to $authStatus');
    }

    return Right(authStatus);
  }
}
