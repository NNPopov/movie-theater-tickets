# Architecture reference

Read this when creating a new feature/slice, modifying layer boundaries, deciding what
goes into `_shared/`, or working with cross-cutting concerns like auth interceptors.

For state management patterns inside a slice (Cubit, Bloc, states), see the
`bloc-state-management` skill — it auto-triggers on those topics.

For decisions about whether something is a new slice or an extension of an existing one,
see the `slice-decomposition` skill.

## Project structure

Each feature is split into **slices** (Vertical Slice). One slice = one operation or a
tightly related group. Code is shared via `_shared/` only when it is actually used by
≥ 2 slices — never "for future use".

```
lib/
├── core/                         # infrastructure, reused by all features
│   ├── auth/
│   │   ├── domain/
│   │   │   ├── entities/             # AuthSession, CurrentUser
│   │   │   └── ports/                # AuthApiPort, TokenStoragePort
│   │   ├── data/
│   │   │   ├── dto/
│   │   │   ├── auth_api_client.dart
│   │   │   ├── auth_api_adapter.dart
│   │   │   └── secure_token_storage_adapter.dart
│   │   ├── application/
│   │   │   ├── auth_cubit.dart
│   │   │   └── auth_state.dart
│   │   ├── infrastructure/
│   │   │   ├── token_holder.dart     # in-memory token cache for the interceptor
│   │   │   ├── auth_interceptor.dart
│   │   │   └── auth_guard.dart
│   │   └── auth_module.dart
│   ├── logging/
│   │   ├── domain/
│   │   │   └── app_logger.dart       # interface
│   │   └── infrastructure/
│   │       └── console_logger_adapter.dart
│   ├── network/                  # Dio setup, interceptors, retry, auth headers
│   ├── storage/                  # secure storage, local cache abstractions
│   ├── di/                       # injectable config, service locator
│   ├── routing/                  # root AppRouter (auto_route), guards
│   ├── rbac/                     # permissions system core
│   ├── i18n/                     # slang generated files + LocaleCubit
│   ├── errors/                   # Failure (sealed), AppException
│   ├── theme/                    # ThemeData, Material 3 tokens
│   └── widgets/                  # shared widgets: AppButton, AppScaffold, ...
├── features/
│   └── users/                    # EXAMPLE: users feature with two slices
│       ├── _shared/              # only what is actually shared by ≥ 2 slices
│       │   ├── domain/entities/  # User domain entity
│       │   ├── data/             # UsersApiClient (Retrofit, all feature endpoints)
│       │   └── presentation/widgets/
│       ├── list_users/
│       │   ├── domain/
│       │   │   ├── entities/         # PaginatedUsers — value object of this slice
│       │   │   ├── ports/            # ListUsersPort — narrow, 1 method
│       │   │   └── usecases/
│       │   ├── data/
│       │   │   ├── dto/
│       │   │   └── list_users_adapter.dart   # implements ListUsersPort
│       │   ├── application/          # list_users_cubit + sealed state via freezed
│       │   └── presentation/         # screen, route, widgets
│       ├── create_user/              # same internal layout as list_users
│       └── users_feature_module.dart # DI registration for all feature slices
└── main.dart
```

## Hard dependency rules

- **Slices within the same feature do not import each other.** Their only connection
  point is `_shared/`.
- If two slices start pulling the same code, move it to `_shared/`. Empty `_shared/` is
  fine. Never put anything there "for future use".
- `_shared/` holds **concrete reusable code**: entities, API clients, shared widgets.
  **Abstractions (ports/interfaces) are better duplicated between slices** than lifted
  into `_shared/`. Duplicating one or two interface methods is an acceptable price for
  slice isolation and Interface Segregation. Two slices wanting "the same" method on
  identically shaped ports are still different ports in different contexts; their
  interfaces evolve independently.
- The shared API client (Retrofit with all operation methods) lives in `_shared/data/`.
  This is a pragmatic compromise to avoid generating a separate Retrofit client per
  operation. A slice adapter receives the API client via DI and uses the one method it
  needs.
- Ports are **narrow** (Interface Segregation): `ListUsersPort` with one method, NOT a
  fat "UsersRepository" with 10 methods.
- The domain entity (`User`) lives in `_shared/domain/entities/`. Value objects for a
  specific operation (`NewUserData`, `PaginatedUsers`) live in the slice's
  `domain/entities/`.
- A feature does not import another feature directly. If needed — via events or via
  `core/`.
- `core/` does not know about `features/`.
- `features/*/domain/` imports nothing from `package:flutter/...`, `package:dio/...`,
  or packages outside `dartz`/`freezed`/pure Dart.
- `features/*/data/` imports `domain/` of its own slice + `_shared/` of its own feature
  + infrastructure from `core/`.
- `features/*/application/` imports `domain/` of its own slice + `flutter_bloc`.
- `features/*/presentation/` imports `application/` and `domain/` of its own slice.

## Bounded contexts: one concept ≠ one class

The same concept (`User`, `Order`, `Product`) in different layers and features is a
**different entity**, even if it ultimately refers to the same technical object.

Signs that a class belongs to its own context:
- Different set of fields.
- Different field semantics. Example: `username` in the catalog is a display
  identifier; `username` in auth is the identifier used in API requests on my behalf.
- Different data source: from DB via `/users`, from a JWT claim, from `/me`, from a
  push payload.
- Classes belong to layers that must not import each other.

**Example from this project:**
- `User` in `features/users/_shared/domain/entities/user.dart` — a user as a catalog
  object (public fields).
- `CurrentUser` in `core/auth/domain/entities/current_user.dart` — "me" in the current
  session, including permission fields like `isSuperuser` that make no sense on a
  list of other users.

These are not duplicates. They are **separate entities**. `core/auth/` does not import
`User` from `features/users/`: that would violate the "core does not know about
features" rule, and semantically the classes are different.

A reliable signal that you wrongly "optimized" the duplication: you want to add a
nullable field to a "shared" class that only makes sense in one context. That means
two classes are needed.

## DI: infrastructure does not depend on application

Cross-cutting concerns (auth, logging, metrics) tend to create cycles in the DI graph
when the infrastructure layer depends directly on the application layer.

**Rule:** infrastructure components (interceptors, middleware, global hooks) **do not
depend** on Cubit/Bloc. If they need application state, that state is passed through a
dedicated infrastructure holder.

Canonical example: `TokenHolder` in `core/auth/infrastructure/token_holder.dart`. It
stores the current access token in memory and is read synchronously by
`AuthInterceptor`. `AuthCubit` updates `TokenHolder` on login/logout. The interceptor
depends on `TokenHolder`, not on `AuthCubit`.

If two components still need to be wired via callback (e.g. on 401 the interceptor must
call `forceLogout` on the Cubit), wiring happens in `main.dart` **after**
`configureDependencies()`:

```dart
final dio = getIt<Dio>();
final tokens = getIt<TokenHolder>();
final authCubit = getIt<AuthCubit>();
dio.interceptors.add(
  AuthInterceptor(tokens, () => unawaited(authCubit.forceLogout())),
);
```

This breaks the initialization cycle: the interceptor accepts a callback, and DI knows
nothing about the Cubit inside the interceptor.

## Terminology: Port and Adapter

The Hexagonal Architecture terms are used literally:

- **Port** — a narrow interface (`abstract class`), declared in `domain/ports/`.
- **Adapter** — a port implementation, lives in `data/`. Named:
  `ListUsersAdapter implements ListUsersPort`.
- The word **Repository** is **not** used in this project: it encourages fat interfaces
  and obscures the semantic weight of "adapter".

## Code patterns

**Narrow port:**

```dart
// features/users/list_users/domain/ports/list_users_port.dart
abstract class ListUsersPort {
  Future<Either<Failure, PaginatedUsers>> call({
    required int page,
    required int perPage,
  });
}
```

**Adapter using the shared API client:**

```dart
// features/users/list_users/data/list_users_adapter.dart
@LazySingleton(as: ListUsersPort)
class ListUsersAdapter implements ListUsersPort {
  final UsersApiClient _api;
  ListUsersAdapter(this._api);

  @override
  Future<Either<Failure, PaginatedUsers>> call({
    required int page,
    required int perPage,
  }) async {
    try {
      final dto = await _api.getUsers(page: page, perPage: perPage);
      return Right(dto.toDomain());
    } on DioException catch (e) {
      return Left(_mapError(e));
    }
  }
}
```

The adapter snippet above shows only the inner `try`. The full required pattern with
the outer catch-all and logging is in `agent_docs/error_handling.md` — read that
before writing or modifying any adapter.

**Feature module (DI):**

```dart
// features/users/users_feature_module.dart
@module
abstract class UsersFeatureModule {
  // Primary registration is done via @injectable / @LazySingleton on adapter classes.
  // Only manual registration of external dependencies goes here, if needed.
}
```
