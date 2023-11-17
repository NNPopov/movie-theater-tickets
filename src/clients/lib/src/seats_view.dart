import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seats_movie_session_widget.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/widgens/shopping_cart_widget.dart';
import 'dashboards/presentation/dashboard_widget.dart';
import 'hub/presentation/widgens/connectivity_widget.dart';
import 'home/presentation/widgets/home_app_bar.dart';
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
        appBar: const HomeAppBar(),
        body: Column(
          children: [

            const DashboardWidget(route: SeatsView.id),
            LayoutBuilder(builder: (context, constraint) {
              return SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: ConstrainedBox(
                  constraints: BoxConstraints(
                      maxWidth:
                          constraint.maxWidth > 880 ? constraint.maxWidth : 880,
                      minWidth: 870),
                  child: IntrinsicHeight(
                    child: Container(
                      height: 700,
                      child: Row(
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
                      ),
                    ),
                  ),
                ),
              );
            }),
          ],
        ));
  }

  @override
  void dispose() {
    super.dispose();
  }
}
