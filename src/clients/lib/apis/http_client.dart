import 'package:dio/dio.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import '../src/helpers/constants.dart';

class Client {

  Client();


  Dio init() {
    Dio _dio = new Dio();
    // _dio.interceptors.add(new AuthInterceptor());
    _dio.options.headers.addAll( {'accept': 'application/json'});
    _dio.options.baseUrl = dotenv.env["BASE_API_URL"].toString();
    return _dio;
  }
}
