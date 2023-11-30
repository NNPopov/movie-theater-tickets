import '../../domain/entities/server_state.dart';

class ServerStateDto extends ServerState {
  const ServerStateDto({required super.serverDateTime});

  ServerStateDto.fromJson(Map<String, dynamic> json)
      : super(serverDateTime: DateTime.parse(json['serverDateTime']));
}

extension ServerStateMap on ServerState {
  ServerStateDto map() {
    return ServerStateDto(serverDateTime: this.serverDateTime);
  }
}
