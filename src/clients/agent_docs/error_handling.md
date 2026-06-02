# Error handling reference

Read this when writing or modifying anything in the `data/` layer (adapters, DTOs,
mappers), the `core/network/` layer, or `main.dart` global error setup.

## Failure types

`Failure` is a sealed class in `core/errors/failure.dart`:

- `NetworkFailure` — network problems.
- `ServerFailure` — 5xx, unexpected server responses.
- `UnauthorizedFailure` — 401 on a protected endpoint (the user will be logged out).
- `InvalidCredentialsFailure` — 401 on `/login` (wrong password).
- `ForbiddenFailure` — 403.
- `NotFoundFailure` — 404.
- `ConflictFailure` — 409.
- `ValidationFailure(Map<String, String>)` — 422 or client-side validation.
- `CacheFailure` — local cache is invalid.
- `PermissionDenied` — RBAC denied.
- `UnknownFailure` — anything unexpected that could not be qualified.

## DTO: soft contract with the server

The server can change responses, and the client must survive that without crashing.

**Rule:** every DTO field must be either `nullable` or have `@Default(value)`, unless
there is a strict guarantee that the server **always** returns a non-empty value of
exactly that type. `required T` is allowed only for fields without which the DTO is
semantically meaningless — usually `id`, sometimes `username` or another primary key.
All other fields go in defensively.

**Example of a good DTO:**

```dart
@freezed
sealed class CurrentUserDto with _$CurrentUserDto {
  const factory CurrentUserDto({
    required int id,                                              // without id the DTO is meaningless
    required String username,                                     // primary identifier
    @Default('') String name,                                     // may be empty
    @Default('') String email,
    @JsonKey(name: 'profile_image_url') String? profileImageUrl, // may be null
    @JsonKey(name: 'tier_id') int? tierId,                       // may be null
    @JsonKey(name: 'is_superuser') @Default(false) bool isSuperuser,  // null → false
  }) = _CurrentUserDto;

  factory CurrentUserDto.fromJson(Map<String, dynamic> json) =>
      _$CurrentUserDtoFromJson(json);
}
```

## Domain: strict contract inside the application

A domain entity stays strict. If business logic requires a non-empty email, the domain
class declares `required String email`. The mapper `dto.toDomain()` either substitutes
defaults or returns `Left(ValidationFailure)` from the adapter method.

**Separation of concerns:** DTO is soft (a technical contract specific to the server),
domain is strict (business rules). The mapper is the **only** place where soft becomes
strict.

## Adapter: two-level catch is mandatory

Every adapter method (port implementation) must have **two-level** error handling.

1. Inner `on DioException catch (e)` — for HTTP errors, with mapping to the appropriate
   `Failure`.
2. Outer `catch (e, st)` — for everything else: `TypeError`, `FormatException`,
   `CheckedFromJsonException`, mapper failures, anything.

The outer catch must:

- **Log** via `AppLogger.error(message, error: e, stackTrace: st)`.
- **Return** `Left(const Failure.unknown())`.
- **Never** throw an exception out of the adapter.
- **Never** silently swallow errors without logging.

**Mandatory template — every adapter method must follow this shape:**

```dart
@LazySingleton(as: SomePort)
class SomeAdapter implements SomePort {
  SomeAdapter(this._api, this._logger);
  final SomeApiClient _api;
  final AppLogger _logger;

  @override
  Future<Either<Failure, Result>> call(...) async {
    try {
      try {
        final dto = await _api.someMethod(...);
        return Right(dto.toDomain());
      } on DioException catch (e) {
        return Left(_mapHttp(e));
      }
    } catch (e, st) {
      _logger.error('SomeAdapter.call failed unexpectedly', error: e, stackTrace: st);
      return Left(const Failure.unknown());
    }
  }
}
```

**Why the outer catch matters.** `json_serializable` throws `TypeError`,
`FormatException`, and other non-`DioException` errors during parsing. In an async
chain without a catch-all, those are silently lost: the `Future` never resolves and
the UI hangs forever. This is a real trap that has already been hit in this project.
The double catch closes it.

**Do not** log password or token details in the catch-all. Log general context (method
name, adapter name) only — never the payload.

## Global error boundary

In `main.dart`, before `runApp`, configure global handlers:

```dart
FlutterError.onError = (details) {
  logger.error(
    'Flutter framework error',
    error: details.exception,
    stackTrace: details.stack,
  );
  FlutterError.presentError(details);
};

PlatformDispatcher.instance.onError = (error, stack) {
  logger.error('Uncaught platform error', error: error, stackTrace: stack);
  return true;
};
```

This is a safety net for **all** unhandled exceptions: the Flutter framework, async
without `await`, isolates. Even if an adapter misses an error, the global handler
catches it.

## Anti-patterns specific to error handling

- ❌ Adapter method without an outer `catch (e, st)`.
- ❌ Logging in the catch-all without `stackTrace: st`.
- ❌ Catching `dynamic` and silently returning `Left(const Failure.unknown())` without
  logging — at minimum, log method/adapter name + error.
- ❌ Exposing a DTO type outside `data/`. Domain consumers see only domain entities.
- ❌ A mapper that throws. A failed mapping must result in
  `Left(ValidationFailure(...))` from the adapter, not an exception.
- ❌ Logging tokens, passwords, refresh tokens, or other sensitive payloads in error
  messages. Strip them before logging.
