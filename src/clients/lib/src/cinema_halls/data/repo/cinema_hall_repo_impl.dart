import 'dart:convert';
import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import 'package:dio/dio.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_hall.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/repo/cinema_hall_repo.dart';
import '../models/cinema_hall_dto.dart';
import 'package:get_it/get_it.dart';

import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class CinemaHallRepoImpl implements CinemaHallRepo {
  late Dio _client;

  CinemaHallRepoImpl({Dio? client}) {
    _client = client ?? getIt.get<Dio>();
  }

  @override
  ResultFuture<CinemaHall> getCinemaHallById(String cinemaHallId) async {
    try {
      final response = await _client.get('/api/cinema-halls/$cinemaHallId');
      var movieSession = json.decode(response.toString());

      var cinemaHallDto = CinemaHallDto.fromJson(movieSession);

      return Right(cinemaHallDto);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message, statusCode: e.statusCode));
    }
  }
}
