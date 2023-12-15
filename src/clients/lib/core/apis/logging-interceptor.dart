import 'package:dio/dio.dart';
import 'package:dio_cache_interceptor/dio_cache_interceptor.dart';

class LoggingInterceptor extends Interceptor {
  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    print("Sending request to: ${options.uri}");
    handler.next(options);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    print("Received response from: ${response.requestOptions.uri}");


    if (response.extra[CacheResponse.cacheKey] != null) {
      print("Response was cached");
    } else {
      print("Response was not cached");
    }
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    print("Request error: ${err.message}");
    handler.next(err);
  }
}