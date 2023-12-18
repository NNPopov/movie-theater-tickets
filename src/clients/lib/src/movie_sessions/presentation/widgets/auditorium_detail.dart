import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/common/views/no_data_view.dart';
import '../../../../core/utils/utils.dart';
import '../../../cinema_halls/presentation/cubit/cinema_hall_cubit.dart';

class AuditoriumDetailView extends StatefulWidget {
  const AuditoriumDetailView(this.auditoriumId, {super.key});

  final String auditoriumId;
  static const id = 'cinema-hall';

  @override
  State<StatefulWidget> createState() => _auditoriumDetailView();
}

class _auditoriumDetailView extends State<AuditoriumDetailView> {

  @override
  void initState() {
    context.read<CinemaHallCubit>().getAuditorium(widget.auditoriumId);
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<CinemaHallCubit, CinemaHallState>(
      listener: (context, state) {
        if (state is CinemaHallError) {
          Utils.showSnackBar(context, state.message);
        }
      },
      builder: (context, state) {
        if (state is! CinemaHallLoaded && state is! CinemaHallError) {
          return const LoadingView();
        }
        if ((state is CinemaHallLoaded && state.auditorium == null) ) {
          return const NoDataView();
        }

        state as CinemaHallLoaded;

        final cinemaHall = state.auditorium;
        return Column(children: [
          Text("Auditorium name :${cinemaHall.description}")
        ]);
      },
    );
  }
}
