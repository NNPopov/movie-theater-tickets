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
  final String _identityTokenPath =
      dotenv.env["IDENTITY_TOKEN_PATH"].toString();
  final String _callbackPath = dotenv.env["CALLBACK_PATH"].toString();
  final String _identityAuthPath = dotenv.env["IDENTITY_AUTH_PATH"].toString();

  ResultFuture<String> logIn() async {
    final callbackUrlScheme = '${_clientBaseUrl}/${_callbackPath}';

    Uri url = createAuthenticateUri(
        _identityHost, _identityAuthPath, callbackUrlScheme, _useHttps);

    final result = await FlutterWebAuth2.authenticate(
        url: url.toString(),
        callbackUrlScheme: _callbackPath);
       // preferEphemeral: true);

    final code = Uri.parse(result).queryParameters['code'];

    final tokenUrl = createCodeExchangeUri(_identityHost, _identityTokenPath, _useHttps);

    final response = await http.post(tokenUrl, body: {
      'client_id': _clientId,
      'redirect_uri': callbackUrlScheme,
      'grant_type': 'authorization_code',
      'code': code
    }, headers: {
      "content-type": "application/x-www-form-urlencoded",
      "Accept": "application/json, text/plain, */*"
    });

    return Right(response.body);
  }

  Uri createCodeExchangeUri(
      String identityHost, String identityAuthPath, bool use_https) {
    if (use_https) {
      return Uri.https(identityHost, identityAuthPath);
    } else {
      return Uri.http(identityHost, identityAuthPath);
    }
  }

  Uri createAuthenticateUri(String identityHost, String identityAuthPath,
      String callbackUrlScheme, bool use_https) {
    var queryParameters = {
      'response_type': 'code',
      'client_id': _clientId,
      'redirect_uri': '$callbackUrlScheme',
      'scope': 'email openid phone'
    };
    if (use_https) {
      return Uri.https(identityHost, identityAuthPath, queryParameters);
    } else {
      return Uri.http(identityHost, identityAuthPath, queryParameters);
    }
  }
}
