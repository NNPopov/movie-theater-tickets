import 'package:flutter/material.dart';

extension ContextExt on BuildContext {
  Size get size => MediaQuery.of(this).size;

  double get width => size.width;

  double get height => size.height;

  ThemeData get theme => Theme.of(this);

}
