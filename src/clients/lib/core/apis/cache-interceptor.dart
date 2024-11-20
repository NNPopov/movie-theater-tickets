// import 'package:dio/dio.dart';
// import 'package:dio_cache_interceptor/dio_cache_interceptor.dart';
//
// //import 'package:dio_cache_interceptor_hive_store/dio_cache_interceptor_hive_store.dart';
//
// class CacheInterceptor {
//   late final CacheStore cacheStore;
//   late final DioCacheInterceptor dioCacheInterceptor;
//
//   CacheInterceptor() {
//     cacheStore = HiveCacheStore('/.', hiveBoxName: 'http_cache');
//
//     var cacheOptions = CacheOptions(
//       store: cacheStore,
//       hitCacheOnErrorExcept: [401, 403],
//       policy: CachePolicy.request,
//       maxStale: const Duration(days: 7),
//       keyBuilder: CacheOptions.defaultCacheKeyBuilder,
//       allowPostMethod: false,
//       priority: CachePriority.high,
//     );
//
//     dioCacheInterceptor = DioCacheInterceptor(options: cacheOptions);
//   }
//
//   Interceptor BuildCahceInterceptor() {
//     return dioCacheInterceptor;
//   }
// }
