import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seats_movie_session_widget.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/widgens/shopping_cart_widget.dart';
import '../core/common/widgets/connectivity_widget.dart';
import 'auth/presentations/widgets/auth_widget.dart';
import 'movie_sessions/domain/entities/movie_session.dart';

class SeatsView extends StatefulWidget {
  const SeatsView(this.movieSession, {super.key});

  final MovieSession movieSession;
  static const id = '/seats';

  @override
  State<StatefulWidget> createState() => _SeatsView();
}

class _SeatsView extends State<SeatsView> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
        backgroundColor: Colors.white,
        appBar: AppBar(
            title: const Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Text("seats"),
            ),
            AuthWidget(),
          ],
        )),
        body: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const ConnectivityWidget(),
            Expanded(
              child: Align(
                alignment: Alignment.topCenter,
                child: SeatsMovieSessionWidget(
                  movieSession: widget.movieSession,
                ),
              ),
            ),
            const ShoppingCartWidget()
          ],
        ));
  }

  @override
  void dispose() {
    super.dispose();
  }
}
