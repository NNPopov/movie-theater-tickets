abstract class  AuthEventBus{

  void send(AuthStatus status);

  Stream<AuthStatus> get stream;
}

class AuthEvent
{}

class ExpiredAuthEvent extends AuthEvent
{}

class UnauthorizedAuthEvent extends AuthEvent
{}

class ForbiddenAuthEvent extends AuthEvent
{}

class AuthorizedAuthEvent extends AuthEvent
{}

abstract class AuthStatus
{
}

class ExpiredAuthStatus extends AuthStatus
{}

class UnauthorizedAuthStatus extends AuthStatus
{}

class AuthorizedAuthStatus extends AuthStatus
{}
