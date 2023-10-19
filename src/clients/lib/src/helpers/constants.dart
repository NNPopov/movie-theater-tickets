import 'dart:ui';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'dart:convert';

class Constants {


  Constants._();

  static final BASE_SSO_URL = dotenv.env["BASE_SSO_URL"].toString();
  static final BASE_API_URL = dotenv.env["BASE_API_URL"].toString();
  //"https://api.client.fhc-dev.net";

  static const SHOPPING_CARD = 'SHOPPING_CARD';
  static const TOKEN_KEY = 'TOKEN';
  static const SSOID = 'SSOID';

  static const SHOPPING_CARD_ID = 'SHOPPING_CARD_ID';


  static const PrimaryColor =  Color(0xFF13A9BA);
}