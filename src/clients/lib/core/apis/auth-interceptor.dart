import 'package:dio/dio.dart';


import 'package:get_it/get_it.dart';

import '../../src/auth/domain/services/auth_service.dart';

GetIt getIt = GetIt.instance;

class AuthInterceptor extends Interceptor {
  AuthInterceptor({AuthService? authService})
      : _authService = authService ?? getIt.get<AuthService>();

  late final AuthService _authService;


  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final listOfPaths = <String>[
      '/send-otp',
      '/validate-otp',
    ];

    if (listOfPaths.contains(options.path.toString())) {
      return handler.next(options);
    }



    var tokenResult = await _authService.getJwtToken();
    tokenResult.fold((l) => null, (token) =>
        options.headers.addAll({'Authorization': 'Bearer $token'})
    );


    return handler.next(options);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    if (response.statusCode == 401) {}
    return handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    return handler.next(err);
  }
}
