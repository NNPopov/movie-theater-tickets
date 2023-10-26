abstract class  AuthEventBus{

  void send(AuthEvent event);
}

class AuthEvent
{}

class ExpiredAuthEvent extends AuthEvent
{}

class UnauthorizedAuthEvent extends AuthEvent
{}

class ForbiddenAuthEvent extends AuthEvent
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
