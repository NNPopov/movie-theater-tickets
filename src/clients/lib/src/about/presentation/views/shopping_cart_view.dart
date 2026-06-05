import 'package:auto_route/auto_route.dart';
import 'package:flutter/material.dart';

/// Route page for the About Us screen (no per-route provider).
@RoutePage(name: 'AboutRoute')
class AboutPage extends StatelessWidget {
  const AboutPage({super.key});

  @override
  Widget build(BuildContext context) => const AboutUsView();
}

class AboutUsView extends StatefulWidget {
  const AboutUsView({super.key});

  static const id = '/about';

  @override
  State<AboutUsView> createState() => _AboutUsView();
}

class _AboutUsView extends State<AboutUsView> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return const SizedBox.expand();
  }
}
