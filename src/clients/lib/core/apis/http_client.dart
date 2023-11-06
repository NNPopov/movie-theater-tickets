import 'package:dio/dio.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'auth-interceptor.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class Client {

  Client(AuthInterceptor? authInterceptor)
      : _authInterceptor = authInterceptor ?? getIt.get<AuthInterceptor>();

  late final AuthInterceptor _authInterceptor;

  Dio init() {
    Dio dio =  Dio();

    dio.options.headers.addAll( {'accept': 'application/json'});
    dio.options.baseUrl = dotenv.env["BASE_API_URL"].toString();
    dio.interceptors.add(_authInterceptor);

    return dio;
  }
}