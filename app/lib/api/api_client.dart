import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

/// Erro devolvido pela API: regra de negócio (400), sem login (401), sem permissão (403)
/// ou falha de rede (status 0).
class ApiException implements Exception {
  final int status;
  final String mensagem;

  ApiException(this.status, this.mensagem);

  bool get naoAutenticado => status == 401;

  @override
  String toString() => mensagem;
}

/// Fala com a Pim.Api (ver docs/api.md). Guarda o endereço do servidor e o token JWT.
class ApiClient {
  String baseUrl;
  String? token;
  final http.Client _http;

  /// Chamado quando o token vence ou é recusado (401), para voltar ao login.
  void Function()? aoPerderLogin;

  ApiClient({required this.baseUrl, this.token, http.Client? cliente}) : _http = cliente ?? http.Client();

  Future<dynamic> get(String caminho) => _enviar('GET', caminho);

  Future<dynamic> post(String caminho, [Map<String, dynamic>? corpo]) => _enviar('POST', caminho, corpo);

  Future<dynamic> _enviar(String metodo, String caminho, [Map<String, dynamic>? corpo]) async {
    final uri = Uri.parse(baseUrl.replaceAll(RegExp(r'/+$'), '') + caminho);
    final cabecalhos = {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };

    http.Response resposta;
    try {
      final req = http.Request(metodo, uri)..headers.addAll(cabecalhos);
      if (corpo != null) req.body = jsonEncode(corpo);
      resposta = await http.Response.fromStream(await _http.send(req)).timeout(const Duration(seconds: 15));
    } on TimeoutException {
      throw ApiException(0, 'O servidor não respondeu. Confira a conexão.');
    } catch (_) {
      throw ApiException(0, 'Sem conexão com o servidor ($baseUrl).');
    }

    final texto = utf8.decode(resposta.bodyBytes);
    final dados = texto.isEmpty ? null : _tentarJson(texto);

    if (resposta.statusCode >= 200 && resposta.statusCode < 300) return dados;

    if (resposta.statusCode == 401) {
      aoPerderLogin?.call();
      throw ApiException(401, 'Sua sessão expirou. Entre de novo.');
    }
    if (resposta.statusCode == 403) {
      throw ApiException(403, 'Seu perfil não tem acesso a esta função.');
    }
    final erro = dados is Map && dados['erro'] != null ? dados['erro'].toString() : 'Erro ${resposta.statusCode} no servidor.';
    throw ApiException(resposta.statusCode, erro);
  }

  static dynamic _tentarJson(String texto) {
    try {
      return jsonDecode(texto);
    } catch (_) {
      return texto;
    }
  }
}
