import 'package:flutter/material.dart';

import '../../../dashboards/presentation/dashboard_widget.dart';
import '../../../home/presentation/widgets/home_app_bar.dart';

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
    return Scaffold(
        backgroundColor: Colors.white,
        appBar: const HomeAppBar(),
        body: Column(children: [
          const DashboardWidget(route: AboutUsView.id),
        ]));
  }
}
