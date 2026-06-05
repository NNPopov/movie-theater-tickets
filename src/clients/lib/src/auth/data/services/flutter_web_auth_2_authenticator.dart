import 'dart:convert';
import 'dart:math';

import 'package:crypto/crypto.dart';
import 'package:flutter_web_auth_2/flutter_web_auth_2.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import 'package:dartz/dartz.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../../domain/abstraction/authenticator.dart';

class FlutterWebAuth2Authenticator implements Authenticator {
  final String _clientId = dotenv.env["CLIENT_ID"].toString();
  final String _clientBaseUrl = dotenv.env["CLIENT_BASE_URL"].toString();

  final String _identityHost = Constants.BASE_SSO_URL;
  final bool _useHttps = Constants.USE_HTTPS;
  final String _identityTokenPath = dotenv.env["IDENTITY_TOKEN_PATH"]
      .toString();
  final String _callbackPath = dotenv.env["CALLBACK_PATH"].toString();
  final String _identityAuthPath = dotenv.env["IDENTITY_AUTH_PATH"].toString();

  @override
  ResultFuture<String> logIn() async {
    final callbackUrlScheme = '$_clientBaseUrl/$_callbackPath';

    final codeVerifier = _createCodeVerifier();
    final codeChallenge = _createCodeChallenge(codeVerifier);

    Uri url = createAuthenticateUri(
      _identityHost,
      _identityAuthPath,
      callbackUrlScheme,
      _useHttps,
      codeChallenge,
    );

    final result = await FlutterWebAuth2.authenticate(
      url: url.toString(),
      callbackUrlScheme: _callbackPath,
    );
    // preferEphemeral: true);

    final code = Uri.parse(result).queryParameters['code'];

    final tokenUrl = createCodeExchangeUri(
      _identityHost,
      _identityTokenPath,
      _useHttps,
    );

    final response = await http.post(
      tokenUrl,
      body: {
        'client_id': _clientId,
        'redirect_uri': callbackUrlScheme,
        'grant_type': 'authorization_code',
        'code': code,
        'code_verifier': codeVerifier,
      },
      headers: {
        "content-type": "application/x-www-form-urlencoded",
        "Accept": "application/json, text/plain, */*",
      },
    );

    return Right(response.body);
  }

  /// Generates a high-entropy PKCE `code_verifier` per RFC 7636 (43–128 chars
  /// from the unreserved set). Required because the `comeandwatchpkce` client
  /// enforces `code_challenge_method=S256`.
  String _createCodeVerifier() {
    const chars =
        'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~';
    final random = Random.secure();
    return List.generate(64, (_) => chars[random.nextInt(chars.length)]).join();
  }

  /// Derives the S256 `code_challenge`: base64url(sha256(verifier)) without
  /// padding, as required by the authorization request.
  String _createCodeChallenge(String codeVerifier) {
    final digest = sha256.convert(ascii.encode(codeVerifier));
    return base64UrlEncode(digest.bytes).replaceAll('=', '');
  }

  Uri createCodeExchangeUri(
    String identityHost,
    String identityAuthPath,
    bool useHttps,
  ) {
    if (useHttps) {
      return Uri.https(identityHost, identityAuthPath);
    } else {
      return Uri.http(identityHost, identityAuthPath);
    }
  }

  Uri createAuthenticateUri(
    String identityHost,
    String identityAuthPath,
    String callbackUrlScheme,
    bool useHttps,
    String codeChallenge,
  ) {
    var queryParameters = {
      'response_type': 'code',
      'client_id': _clientId,
      'redirect_uri': callbackUrlScheme,
      'scope': 'email openid phone',
      'code_challenge': codeChallenge,
      'code_challenge_method': 'S256',
    };
    if (useHttps) {
      return Uri.https(identityHost, identityAuthPath, queryParameters);
    } else {
      return Uri.http(identityHost, identityAuthPath, queryParameters);
    }
  }
}
