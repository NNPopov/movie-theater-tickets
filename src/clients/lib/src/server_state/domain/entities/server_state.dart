import 'package:equatable/equatable.dart';

class ServerState extends Equatable {
  final DateTime serverDateTime;


   const ServerState({ required this.serverDateTime});

  static ServerState initState()
  {return ServerState(serverDateTime:DateTime.parse('1900-01-01 00:00:00.001Z'));}

  @override
  // TODO: implement props
  List<Object?> get props => [serverDateTime];
}