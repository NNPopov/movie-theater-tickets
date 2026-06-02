# Navigation reference

Read this when creating a new screen, defining its route, working with route guards,
deep links, or modifying the root `AppRouter`.

## Where navigation lives

- The root `AppRouter` is in `core/routing/app_router.dart`. Its generated counterpart
  is `app_router.gr.dart`.
- Each slice declares its route in a `<slice>_route.dart` file inside its
  `presentation/` folder. The root router collects these declarations.
- `AuthGuard` and `PermissionGuard` live in `core/routing/guards/`.

This separation matters: a slice **owns** its route declaration, the same way it owns
its Cubit and screen. The root router only composes them. When a slice is removed, its
route file goes with it and the root router only loses one import.

## Adding a new screen

1. Create the screen widget in `<slice>/presentation/<slice>_screen.dart`.
2. Create the route declaration in `<slice>/presentation/<slice>_route.dart`:

   ```dart
   @RoutePage()
   class CreateUserRoute extends StatelessWidget {
     const CreateUserRoute({super.key});
     @override
     Widget build(BuildContext context) => const CreateUserScreen();
   }
   ```

3. Register the route in `core/routing/app_router.dart` under the appropriate parent.
4. Run `dart run build_runner build --delete-conflicting-outputs` to regenerate
   `app_router.gr.dart`.
5. Navigate via `context.router.push(const CreateUserRoute())` — never via raw paths.

## Guards

`PermissionGuard` requires a `Set<Permission>` in its constructor and consults
`PermissionCubit` from `core/rbac/`. See `agent_docs/rbac.md` for the permission model.

```dart
AutoRoute(
  page: AdminPanelRoute.page,
  path: '/admin',
  guards: [PermissionGuard({Permission.editUsers, Permission.viewReports})],
),
```

`AuthGuard` (in `core/auth/infrastructure/auth_guard.dart`) blocks unauthenticated
users and redirects to the login flow.

## Deep links

- All deep links are declared with explicit `path:` arguments on routes. Avoid
  auto-generated paths, which become unstable when slice names change.
- A deep link must survive a cold start: any state required by the destination screen
  comes either from the URL itself (path/query parameters) or is loaded by the
  destination Cubit on init. The screen never assumes that some other screen "left
  state behind".

## Common mistakes

- ❌ Putting route declarations in `core/routing/`. Routes belong to their slice.
- ❌ Navigating with raw string paths. Always use the generated `*Route` classes for
  type safety.
- ❌ Forgetting `dart run build_runner build` after editing a `@RoutePage()` annotation
  or the router. The error is misleading — it surfaces as a missing class, not a
  codegen warning.
- ❌ Reading data from a navigation argument and then mutating it. Treat navigation
  arguments as immutable inputs.
- ❌ Stuffing business logic into a guard. Guards only **gate** access; they do not
  fetch data or change state.
