# RBAC reference

Read this when working with permissions, roles, route guards, or any UI element that
should appear or behave differently depending on who the user is.

## Model

```dart
// core/rbac/permission.dart
enum Permission {
  viewCatalog,
  editCatalog,
  viewUsers,
  editUsers,
  viewReports,
}

// core/rbac/role.dart
enum UserRole { guest, user, manager, admin }

// core/rbac/role_policy.dart
const Map<UserRole, Set<Permission>> kRolePolicy = {
  UserRole.guest:   {Permission.viewCatalog},
  UserRole.user:    {Permission.viewCatalog, Permission.viewReports},
  UserRole.manager: {Permission.viewCatalog, Permission.editCatalog, Permission.viewReports},
  UserRole.admin:   {...Permission.values},
};
```

`PermissionCubit` subscribes to `AuthCubit` and holds the current `Set<Permission>` for
the active user.

## Three places to check permissions — and you must check at all relevant levels

1. **Route level** — `PermissionGuard` in `auto_route`. Stops navigation entirely if the
   user lacks the required permission set.
2. **Widget level** — helper `PermissionBuilder(required: {Permission.editCatalog}, child: ...)`,
   which lives in `core/rbac/`. If permissions are absent, render `SizedBox.shrink()`
   or a disabled version of the control.
3. **Use-case level** — the use-case checks the permission itself and returns
   `Left(PermissionDenied())` if permissions are absent. **This is the final line of
   defence.**

**Never** check permissions only in the UI. Always duplicate the check in the use-case.
This protects against a malicious client trying to bypass the UI layer.

## How a use-case enforces a permission

```dart
class EditCatalogItemUseCase {
  EditCatalogItemUseCase(this._permissions, this._port);
  final PermissionCubit _permissions;
  final EditCatalogItemPort _port;

  Future<Either<Failure, CatalogItem>> call(CatalogItem item) async {
    if (!_permissions.state.has(Permission.editCatalog)) {
      return const Left(Failure.permissionDenied());
    }
    return _port(item);
  }
}
```

The same use-case is invoked from a screen guarded by `PermissionGuard` and from a
widget wrapped in `PermissionBuilder`. The defence in depth is intentional: each layer
handles a different threat (wrong route, accidental render, manipulated client).

## Common mistakes

- ❌ Checking only in `PermissionBuilder`. A patched build skips the UI hide and the
  use-case still mutates server state.
- ❌ Rolling a custom `Map<String, bool>` of permissions instead of using the
  `Permission` enum. The enum is the single source of truth.
- ❌ Storing role strings from the backend untyped. Map them to the `UserRole` enum at
  the adapter boundary; treat unknown values as `UserRole.guest`.
- ❌ Reading `PermissionCubit` directly inside a widget when a `PermissionBuilder`
  exists. Use the helper.
- ❌ Adding a new feature behind a permission and forgetting to update `kRolePolicy`.
  Whenever a new `Permission` enum value is added, every role's set must be reviewed.
