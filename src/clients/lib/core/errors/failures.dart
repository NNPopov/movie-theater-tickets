import 'package:equatable/equatable.dart';

import 'exceptions.dart';

abstract class Failure extends Equatable {
  const Failure({required this.message, required this.statusCode});

  final String message;
  final dynamic statusCode;

  String get errorMessage => '$statusCode Error: $message';

  @override
  List<dynamic> get props => [message, statusCode];
}

class DataFailure extends Failure {
  const DataFailure({required super.message, required super.statusCode});
}

class CacheFailure extends Failure {
  const CacheFailure({required super.message, required super.statusCode});
}

class ConflictFailure extends Failure {
  const ConflictFailure({required super.message, super.statusCode = 409});

  ConflictFailure.fromException(ServerException exception)
      : this(message: exception.message);
}

class NotFoundFailure extends Failure {
  const NotFoundFailure(
      {super.message = 'ContentNotFound', super.statusCode = 204});

  NotFoundFailure.fromException(ServerException exception)
      : this(message: exception.message);
}

class ServerFailure extends Failure {
  const ServerFailure({required super.message, required super.statusCode});

  ServerFailure.fromException(ServerException exception)
      : this(message: exception.message, statusCode: exception.statusCode);
}

class ValidationFailure extends Failure {
  const ValidationFailure({required super.message, super.statusCode = 400});

  ValidationFailure.fromException(ServerException exception)
      : this(message: exception.message, statusCode: exception.statusCode);
}

class NotAuthorisedException extends Failure {
  const NotAuthorisedException(
      {super.message = 'Use is not authorized', super.statusCode = 401});

  @override
  List<dynamic> get props => [message, statusCode];
}

class ShoppingCartNotAssignedException extends Failure {
  const ShoppingCartNotAssignedException(
      {required super.message, required super.statusCode});

  @override
  List<dynamic> get props => [message, statusCode];
}
