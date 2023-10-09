import 'dart:convert';
import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import 'package:dio/dio.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/seat.dart';
import '../../domain/repos/seat_repo.dart';
import 'package:get_it/get_it.dart';

import '../models/seat_dto.dart';

GetIt getIt = GetIt.instance;

class SeatRepoImpl extends SeatRepo {
  late Dio _client;

  SeatRepoImpl({Dio? client}) {
    _client = client ?? getIt.get<Dio>();
  }


  @override
  ResultFuture<List<Seat>> getSeatsByMovieSessionId(String movieSessionId) async {
    try {
      Response response = await _client.get('/api/moviesessions/$movieSessionId/seats');
      List<dynamic> movies = jsonDecode(jsonEncode(response.data));

      List<Seat> seatDtos =
      movies.map((json) => SeatDto.fromJson(json) as Seat).toList();

      return Right(seatDtos);
    } on DioException catch (e) {
      return Left(ServerFailure(
          message: json.decode(e.response.toString())["errorMessage"],
          statusCode: e.message));
    }
  }
}
