import 'package:dio/dio.dart';
import '../../src/helpers/constants.dart';
import 'auth-interceptor.dart';
import 'package:get_it/get_it.dart';
import 'package:dio_cache_interceptor/dio_cache_interceptor.dart';

//import 'package:dio_cache_interceptor_hive_store/dio_cache_interceptor_hive_store.dart';

GetIt getIt = GetIt.instance;

class Client {
  late final CacheStore cacheStore;
  late final DioCacheInterceptor dioCacheInterceptor;

  Client(AuthInterceptor? authInterceptor)
      : _authInterceptor = authInterceptor ?? getIt.get<AuthInterceptor>();

  late final AuthInterceptor _authInterceptor;

  Dio init() {
    Dio dio = Dio();

    dio.options.headers.addAll({'accept': 'application/json'});
    dio.options.baseUrl = Constants.BASE_API_URL;
    dio.interceptors.add(_authInterceptor);

    //cacheStore = HiveCacheStore('/.', hiveBoxName: 'http_cache');
    cacheStore = MemCacheStore(maxSize: 10485760, maxEntrySize: 1048576);

    var cacheOptions = CacheOptions(
      store: cacheStore,
      hitCacheOnErrorExcept: [401, 403],
      policy: CachePolicy.request,
      maxStale: const Duration(days: 7),
      keyBuilder: CacheOptions.defaultCacheKeyBuilder,
      allowPostMethod: false,
      priority: CachePriority.high,
    );

    dioCacheInterceptor = DioCacheInterceptor(options: cacheOptions);

    dio.interceptors.add(dioCacheInterceptor);
    return dio;
  }
}
