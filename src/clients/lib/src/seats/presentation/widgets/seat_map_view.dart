import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/common/views/loading_view.dart';
import '../../../cinema_halls/domain/layout/seat_layout.dart';
import '../../../movie_sessions/domain/entities/movie_session.dart';
import '../../../shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import '../../domain/entities/seat_id.dart';
import '../../domain/render/seat_hit_tester.dart';
import '../../domain/render/seat_layout_transform.dart';
import '../cubit/seat_cubit.dart';
import '../cubit/seat_layout_cubit.dart';
import '../render/seat_map_painter.dart';
import '../render/seat_palette.dart';

/// Coordinate-driven seat renderer: a single [CustomPaint] inside an
/// [InteractiveViewer] (ADR 0005 P5). Replaces the index-driven grid body.
///
/// All tap→seat resolution flows through the pure [SeatLayoutTransform] +
/// `resolveSeatAt` — no seat geometry math lives in this widget (N6). Status and
/// cart feeds are reused unchanged; only how the hall is drawn and how a tap is
/// resolved changes.
class SeatMapView extends StatefulWidget {
  const SeatMapView({required this.movieSession, super.key});

  final MovieSession movieSession;

  @override
  State<SeatMapView> createState() => _SeatMapViewState();
}

class _SeatMapViewState extends State<SeatMapView> {
  final TransformationController _controller = TransformationController();

  @override
  void initState() {
    super.initState();
    // Kick off the status load; the layout load is started by the screen's
    // provider via `SeatLayoutCubit()..load(hallId)`.
    context.read<SeatBloc>().add(
      SeatEvent(movieSessionId: widget.movieSession.id),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<SeatLayoutCubit, SeatLayoutState>(
      builder: (context, layoutState) {
        switch (layoutState.status) {
          case SeatLayoutStatus.initial:
          case SeatLayoutStatus.loading:
            return const LoadingView();
          case SeatLayoutStatus.error:
            return Center(child: Text(layoutState.errorMessage ?? ''));
          case SeatLayoutStatus.loaded:
            return _buildMap(context, layoutState.layout!);
        }
      },
    );
  }

  Widget _buildMap(BuildContext context, SeatLayout layout) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final canvasSize = Size(constraints.maxWidth, constraints.maxHeight);
        final boundsRect = Rect.fromLTWH(
          layout.bounds.x,
          layout.bounds.y,
          layout.bounds.width,
          layout.bounds.height,
        );
        final transform = SeatLayoutTransform.fit(boundsRect, canvasSize);

        // Discrete taps are captured by the OUTER detector so the renderer owns
        // the full composed inverse (viewer matrix ∘ fit); the InteractiveViewer
        // still owns pan/zoom (drag/pinch win the arena, taps fall through).
        return GestureDetector(
          behavior: HitTestBehavior.opaque,
          onTapUp: (details) =>
              _handleTap(details.localPosition, layout, transform),
          child: InteractiveViewer(
            transformationController: _controller,
            minScale: 0.5,
            maxScale: 4,
            boundaryMargin: const EdgeInsets.all(double.infinity),
            child: BlocBuilder<SeatBloc, SeatState>(
              builder: (context, seatState) {
                return BlocSelector<
                  ShoppingCartCubit,
                  ShoppingCartState,
                  String
                >(
                  selector: (state) => state.hashId,
                  builder: (context, hashId) {
                    return CustomPaint(
                      size: canvasSize,
                      painter: SeatMapPainter(
                        layout: layout,
                        transform: transform,
                        byId: seatState.byId,
                        cartHashId: hashId,
                      ),
                    );
                  },
                );
              },
            ),
          ),
        );
      },
    );
  }

  void _handleTap(
    Offset viewportPoint,
    SeatLayout layout,
    SeatLayoutTransform transform,
  ) {
    // viewport → canvas-base: invert the live InteractiveViewer matrix.
    final canvasPoint = MatrixUtils.transformPoint(
      Matrix4.inverted(_controller.value),
      viewportPoint,
    );
    final layoutPoint = transform.canvasToLayout(canvasPoint);
    final seatId = resolveSeatAt(layoutPoint, layout.seats);
    if (seatId == null) {
      return; // gap / outside → nothing
    }

    final seat = context.read<SeatBloc>().state.byId[seatId];
    // Same hashId the colour uses (ShoppingCartState.hashId) so colour and action
    // never diverge.
    final cartHashId = context.read<ShoppingCartCubit>().state.hashId;

    switch (tapIntentFor(seat, cartHashId)) {
      case SeatTapIntent.select:
        _select(seatId);
      case SeatTapIntent.unselect:
        _unselect(seatId);
      case SeatTapIntent.none:
        break;
    }
  }

  void _select(SeatId seatId) {
    final cart = context.read<ShoppingCartCubit>();
    if (cart.state.status != ShoppingCartStateStatus.initial) {
      cart.seatSelect(
        row: seatId.$1,
        seatNumber: seatId.$2,
        movieSessionId: widget.movieSession.id,
      );
    }
  }

  void _unselect(SeatId seatId) {
    final cart = context.read<ShoppingCartCubit>();
    if (cart.state.status != ShoppingCartStateStatus.initial) {
      cart.unSeatSelect(
        row: seatId.$1,
        seatNumber: seatId.$2,
        movieSessionId: widget.movieSession.id,
      );
    }
  }
}
