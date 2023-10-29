import 'dart:convert';

import '../../../../core/errors/exceptions.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../../domain/abstruction/auth_event_bus.dart';
import '../../domain/abstruction/authenticator.dart';
import '../../domain/services/auth_service.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';
import 'package:jwt_decoder/jwt_decoder.dart';

GetIt getIt = GetIt.instance;

class AuthServiceImpl implements AuthService {
  AuthServiceImpl({Authenticator? authenticator})
      : _authenticator = authenticator ?? getIt.get<Authenticator>();

  late final Authenticator _authenticator;

  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<String> getJwtToken() async {
    var token = await storage.read(key: Constants.TOKEN_KEY);

    if (token == null) {
      return const Left(NotAuthorisedException(message: '', statusCode: 401));
    }

    bool hasExpired = JwtDecoder.isExpired(token);

    if(hasExpired) {
      await storage.delete(key: Constants.TOKEN_KEY);
      return const Left(NotAuthorisedException(message: '', statusCode: 401));
    }

    return Right(token);
  }

  @override
  ResultFuture<void> setJwtToken(String token) async {
    return Right(null);
  }

  @override
  ResultFuture<AuthStatus> getCurrentStatus() async {

    var token = await storage.read(key: Constants.TOKEN_KEY);

    if (token == null) {
      return Right(UnauthorizedAuthStatus());
    }

    bool hasExpired = JwtDecoder.isExpired(token);

    if(hasExpired) {
      return Right(ExpiredAuthStatus());
    }

    return Right(AuthorizedAuthStatus());
  }

  @override
  ResultFuture<AuthStatus> logIn() async {
    var authResult = await _authenticator.logIn();

    return authResult.fold((l) => Left(l), (r) async {
      final accessToken = jsonDecode(r)['access_token'] as String;

      await storage.write(key: Constants.TOKEN_KEY, value: accessToken);

      return  Right(AuthorizedAuthStatus());
    });
  }

  @override
  ResultFuture<AuthStatus> logOut() async {
    await storage.delete(key: Constants.TOKEN_KEY);

    return  Right(UnauthorizedAuthStatus());
  }
}
