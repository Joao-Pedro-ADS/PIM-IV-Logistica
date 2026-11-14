import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'api/api_client.dart';
import 'api/modelos.dart';
import 'api/pim_api.dart';

/// Guarda no celular o endereço do servidor e o login, para não pedir de novo a cada abertura.
class Sessao extends ChangeNotifier {
  // Emulador Android enxerga o computador em 10.0.2.2. No celular, use o IP do computador na rede Wi-Fi.
  static const servidorPadrao = 'http://10.0.2.2:5090';

  final SharedPreferences _prefs;
  late final ApiClient cliente;
  late final PimApi api;
  Usuario? usuario;

  Sessao(this._prefs) {
    cliente = ApiClient(baseUrl: _prefs.getString('servidor') ?? servidorPadrao);
    cliente.aoPerderLogin = sair;
    api = PimApi(cliente);

    final token = _prefs.getString('token');
    final nome = _prefs.getString('nome');
    final perfil = _prefs.getString('perfil');
    if (token != null && nome != null && perfil != null) {
      usuario = Usuario(nome: nome, perfil: perfilDe(perfil), token: token);
      cliente.token = token;
    }
  }

  static Future<Sessao> carregar() async => Sessao(await SharedPreferences.getInstance());

  String get servidor => cliente.baseUrl;

  Future<void> entrar(String servidor, String login, String senha) async {
    cliente.baseUrl = servidor.trim();
    await _prefs.setString('servidor', cliente.baseUrl);

    final u = await api.login(login.trim(), senha);
    usuario = u;
    cliente.token = u.token;
    await _prefs.setString('token', u.token);
    await _prefs.setString('nome', u.nome);
    await _prefs.setString('perfil', u.perfil.name);
    notifyListeners();
  }

  void sair() {
    usuario = null;
    cliente.token = null;
    for (final chave in ['token', 'nome', 'perfil', 'entradaAtual']) {
      _prefs.remove(chave);
    }
    notifyListeners();
  }

  // Entrada em andamento do conferente: permite continuar depois de fechar o app
  int? get entradaAtual => _prefs.getInt('entradaAtual');

  set entradaAtual(int? id) => id == null ? _prefs.remove('entradaAtual') : _prefs.setInt('entradaAtual', id);
}
