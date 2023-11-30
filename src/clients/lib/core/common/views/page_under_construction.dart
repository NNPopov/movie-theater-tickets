
import 'package:flutter/material.dart';


class PageUnderConstruction extends StatelessWidget {
  const PageUnderConstruction({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(),
      extendBodyBehindAppBar: true,
      body:
        const Center(
          child: Text("Page Under Construction"),

      ),
    );
  }
}
