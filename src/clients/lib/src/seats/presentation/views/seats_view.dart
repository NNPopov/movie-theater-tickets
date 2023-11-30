import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seats_movie_session_widget.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/widgens/shopping_cart_widget.dart';
import '../../../dashboards/presentation/dashboard_widget.dart';
import '../../../movie_sessions/domain/entities/movie_session.dart';
import 'package:get_it/get_it.dart';
GetIt getIt = GetIt.instance;

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
    return Column(
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
                    child: SizedBox(
                      height: 700,
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: Align(
                              alignment: Alignment.topCenter,
                              child: SeatsMovieSessionWidget(
                                movieSession: widget.movieSession,
                                  getCinemaHallInfo:getIt.get()
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
        );
  }

  @override
  void dispose() {
    super.dispose();
  }
}
