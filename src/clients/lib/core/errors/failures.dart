import 'package:equatable/equatable.dart';

import 'exceptions.dart';

abstract class Failure extends Equatable {
  const Failure({required this.message, required this.statusCode});

  final String message;
  final dynamic statusCode;

  // Ignore till you're adding Notifications Cubit, so that they can learn
  // the actual structure of the error message
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
  const ConflictFailure({required super.message, super.statusCode=409});

  ConflictFailure.fromException(ServerException exception)
      : this(message: exception.message);
}

class ServerFailure extends Failure {
  const ServerFailure({required super.message, required super.statusCode});

  ServerFailure.fromException(ServerException exception)
      : this(message: exception.message, statusCode: exception.statusCode);
}

class NotAuthorisedException extends Failure  {
   NotAuthorisedException({required super.message, required super.statusCode});


  @override
  List<dynamic> get props => [message, statusCode];
}
