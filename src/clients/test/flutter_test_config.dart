import 'dart:async';
import 'dart:io';

import 'package:path_provider_platform_interface/path_provider_platform_interface.dart';
import 'package:plugin_platform_interface/plugin_platform_interface.dart';

/// Global test harness configuration applied to every test under `test/`.
///
/// `localstorage` 6 backs its store with a file under the app documents
/// directory, resolved through `path_provider`. Plugin channels are not wired
/// under `flutter test`, so without this fake any code path that calls
/// `initLocalStorage()` (e.g. the shopping-cart local repo) throws a
/// `MissingPluginException`. Pointing `path_provider` at a temp directory lets
/// the real in-process storage round-trip run in unit tests.
Future<void> testExecutable(FutureOr<void> Function() testMain) async {
  PathProviderPlatform.instance = _TempPathProviderPlatform();
  await testMain();
}

class _TempPathProviderPlatform extends PathProviderPlatform
    with MockPlatformInterfaceMixin {
  final Directory _dir =
      Directory.systemTemp.createTempSync('movie_theatre_tests');

  @override
  Future<String?> getApplicationDocumentsPath() async => _dir.path;

  @override
  Future<String?> getApplicationSupportPath() async => _dir.path;

  @override
  Future<String?> getTemporaryPath() async => _dir.path;
}
