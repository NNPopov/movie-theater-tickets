import 'dart:ui';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'dart:convert';

class Constants {
  Constants._();

  static const BASE_SSO_URL = "https://localhost:9443";
  static final BASE_API_URL = dotenv.env["BASE_API_URL"].toString();
  //"https://api.client.fhc-dev.net";

  static const TOKEN_KEY = 'TOKEN';
  static const SSOID = 'SSOID';


  static const PrimaryColor =  Color(0xFF13A9BA);
}