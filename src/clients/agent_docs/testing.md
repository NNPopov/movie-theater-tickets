# Testing reference

Read this when writing or modifying tests. For general Flutter testing knowledge, see
the `flutter-testing-apps` skill; this file documents only what is specific to this
project.

## Coverage requirements

A slice is not "done" without tests on **all four** layers (this is the project default
established in `CLAUDE.md`):

- **Unit tests for every use-case** in `domain/usecases/`. A use-case orchestrates
  business rules; if it is not tested, the rule is not enforced.
- **Unit tests for every adapter** in `data/`. Cover the success path, every HTTP
  failure code the endpoint can return, and an unexpected exception path that
  verifies `logger.error` was called.
- **`bloc_test` for every Cubit/Bloc** in `application/`. Cover every state
  transition, including cancel/dismiss paths.
- **Widget tests for every screen** with a mocked Cubit. Cover each observable UI
  state (loading, success, error, empty). This catches missing `BlocListener`
  wiring and bad widget keys far cheaper than an integration test.

The user may explicitly waive a layer for a specific slice ("skip widget tests this
time"). Without such a waiver, all four are mandatory.

## Folder layout

The test tree mirrors `lib/` exactly, including the slice subfolders:

```
test/
└── features/
    └── users/
        ├── _shared/
        │   └── domain/entities/user_test.dart
        ├── list_users/
        │   ├── domain/usecases/list_users_usecase_test.dart
        │   ├── application/list_users_cubit_test.dart
        │   └── presentation/list_users_screen_test.dart
        └── create_user/
            └── ...
```

Mirroring is mandatory. When grep-ing for the test of a given source file, the path is
predictable.

## Mocking: mocktail, never mockito

Mocks are written with `mocktail`. Reasons: it works without codegen, supports null
safety natively, and registers fallback values cleanly for sealed types like `Failure`.

Pattern for a use-case test:

```dart
class _MockListUsersPort extends Mock implements ListUsersPort {}

void main() {
  late _MockListUsersPort port;
  late ListUsersUseCase useCase;

  setUpAll(() {
    // Register fallback values for any sealed/enum types used as positional args.
    registerFallbackValue(const PaginatedUsers.empty());
  });

  setUp(() {
    port = _MockListUsersPort();
    useCase = ListUsersUseCase(port);
  });

  test('returns paginated users on success', () async {
    when(() => port(page: any(named: 'page'), perPage: any(named: 'perPage')))
        .thenAnswer((_) async => const Right(PaginatedUsers.empty()));

    final result = await useCase(page: 1, perPage: 20);

    expect(result, isA<Right<Failure, PaginatedUsers>>());
    verify(() => port(page: 1, perPage: 20)).called(1);
  });
}
```

Notice two things. First, `mocktail` requires `registerFallbackValue` for any custom
type that appears in a `when(() => ...)` matcher with `any()` — without it the test
throws at runtime. Second, `verify` confirms the use-case actually called the port
with the right arguments, not just that it returned the right value.

## bloc_test pattern for a Cubit

```dart
blocTest<ListUsersCubit, ListUsersState>(
  'emits [Loading, Loaded] when use-case returns success',
  build: () {
    when(() => useCase(page: any(named: 'page'), perPage: any(named: 'perPage')))
        .thenAnswer((_) async => Right(_paginatedSample));
    return ListUsersCubit(useCase);
  },
  act: (cubit) => cubit.load(),
  expect: () => [
    const ListUsersState.loading(),
    ListUsersState.loaded(_paginatedSample),
  ],
);
```

Read this carefully because it shows the canonical shape: `build` constructs a fresh
Cubit (and stubs collaborators), `act` triggers the behaviour under test, and `expect`
asserts the **exact** state sequence. If the Cubit emits an extra intermediate state,
the test fails — that is the point. State sequences are part of the public contract.

## Widget tests with a mocked Cubit

```dart
class _MockListUsersCubit extends MockCubit<ListUsersState>
    implements ListUsersCubit {}

testWidgets('shows error banner on Failure', (tester) async {
  final cubit = _MockListUsersCubit();
  whenListen(
    cubit,
    Stream.fromIterable([
      const ListUsersState.loading(),
      ListUsersState.error(const Failure.network()),
    ]),
    initialState: const ListUsersState.initial(),
  );

  await tester.pumpWidget(
    MaterialApp(home: BlocProvider.value(value: cubit, child: const ListUsersScreen())),
  );
  await tester.pump();

  expect(find.byKey(const Key('error_banner')), findsOneWidget);
});
```

`MockCubit` and `whenListen` come from `bloc_test`. They let the widget test drive the
Cubit's state externally without any real use-case in play.

## Common mistakes

- ❌ Using `mockito` or `@GenerateMocks`. The project uses `mocktail` exclusively.
- ❌ Writing a test that calls a real adapter. Adapters are integration concerns; unit
  tests stop at the port boundary.
- ❌ Asserting only the final state in a `bloc_test`. Always assert the **sequence** —
  intermediate states matter for UX (loading spinners, optimistic updates).
- ❌ Forgetting `registerFallbackValue` for custom matchers. The runtime error is
  cryptic; remember it lives in `setUpAll`.
- ❌ Importing test code into `lib/`. Test helpers live under `test/_helpers/`, never
  in production code.
- ❌ Putting a test under `test/` that touches more than one slice. If a test crosses
  slice boundaries, it is an integration test and belongs under `integration_test/`,
  not `test/`.
- ❌ Forgetting `TranslationProvider` in widget tests for any widget that uses
  `context.t`. The runtime error is `Please wrap your app with "TranslationProvider"`.
  Remember `LocaleSettings.setLocale(AppLocale.en)` in `setUpAll` and wrap the
  test root with `TranslationProvider`.
- ❌ Using a single-listener `StreamController` to drive a Cubit's stream when the
  widget under test contains both `BlocBuilder` and `BlocListener`. Both subscribe
  and the second throws `Bad state: Stream has already been listened to`. Use
  `StreamController.broadcast()` instead.
- ❌ Forgetting to run `dart run slang` after editing `*.json` localization files.
  `build_runner build` does **not** drive the slang generator — they are separate.
  After any JSON change: run `dart run slang` first, then
  `dart run build_runner build --delete-conflicting-outputs`.

## Reference tests

When you need a working example for a non-trivial test pattern, read the file
directly rather than relying on a snippet here. Real tests stay in sync with the
codebase; documented snippets drift over time.

- **Widget test with router and Cubit** — see
  `test/features/posts/erase_db_post/presentation/erase_db_post_button_test.dart`.
  Demonstrates: mocking `StackRouter` via `StackRouterScope(stateHash: 0, controller: mockRouter, child: ...)`,
  injecting a mock Cubit through `getIt.registerFactory` + `unregister` in tear-down,
  driving state transitions with a `StreamController.broadcast()`, wrapping the
  test tree with `TranslationProvider` after `LocaleSettings.setLocale(AppLocale.en)`.
- **Adapter test with full failure mapping and double catch** — see
  `test/features/posts/erase_db_post/data/erase_db_post_adapter_test.dart`.
  Demonstrates: how to construct a `DioException` for each status code (401/403/404/500),
  asserting `Failure` subtype via `result.fold((f) => f, (_) => fail(...))`,
  verifying `logger.error` was called with `error:` and `stackTrace:` named arguments
  on the unexpected-exception path (the outer catch-all).

These pointers replace boilerplate copying. When the referenced file evolves,
the pattern evolves with it. If a referenced file is renamed or removed, update
this section in the same commit.
