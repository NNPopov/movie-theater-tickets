import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../cinema_halls/domain/entity/cinema_hall_info.dart';
import '../../../cinema_halls/domain/repo/cinema_hall_repo.dart';
import '../../../hub/domain/event_hub.dart';
import '../entities/seat.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:collection/collection.dart';
import 'package:dartz/dartz.dart';
import '../repos/seat_repo.dart';

GetIt getIt = GetIt.instance;

class GetCinemaHallInfo
    extends FutureUsecaseWithParams<CinemaHallInfo, String> {

  GetCinemaHallInfo(this._repo, this._eventHub);

  final CinemaHallRepo _repo;
  final EventHub _eventHub;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<CinemaHallInfo> call(String params) async
  {
    var result =  await _repo.getCinemaHallInfoById(params);

    await _eventHub.seatsUpdateSubscribe(params);
    // return  result.fold((failure) => Left(failure),
    //         (seats) async {
    //       var finalSeats = seats.map((e) {
    //         var s = Seat.temp(
    //             row: e.row,
    //             seatNumber: e.seatNumber,
    //             blocked: e.blocked,
    //             isCurrentReserve: true,
    //             seatStatus: e.seatStatus,
    //             hashId: e.hashId);
    //
    //         return s;
    //       }).toList();
    //
    //       final rowSeats = groupBy(finalSeats, (seat) => seat.row)
    //           .values
    //           .map((seats) => seats.toList()..sort((a, b) => a.seatNumber - b.seatNumber))
    //           .toList()..sort((a, b) => a[0].row - b[0].row);

          //_eventBus.send(SeatsUpdateEvent(rowSeats));

          return result;
      //  });

  // return  result.fold((failure) => Left(failure),
  //           (seats) async {
  //     var finalSeats = seats.map((e) {
  //       var s = Seat.temp(
  //           row: e.row,
  //           seatNumber: e.seatNumber,
  //           blocked: e.blocked,
  //           isCurrentReserve: true,
  //           seatStatus: e.seatStatus,
  //           hashId: e.hashId);
  //
  //       return s;
  //     }).toList();
  //
  //     final rowSeats = groupBy(finalSeats, (seat) => seat.row)
  //         .values
  //         .map((seats) => seats.toList()..sort((a, b) => a.seatNumber - b.seatNumber))
  //         .toList()..sort((a, b) => a[0].row - b[0].row);
  //
  //     //_eventBus.send(SeatsUpdateEvent(rowSeats));
  //
  //     return Right(rowSeats);
  //   });
  }


}