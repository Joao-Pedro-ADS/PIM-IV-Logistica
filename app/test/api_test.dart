import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:pim_app/api/api_client.dart';
import 'package:pim_app/api/modelos.dart';
import 'package:pim_app/api/pim_api.dart';

http.Response json(Object corpo, int status) =>
    http.Response.bytes(utf8.encode(jsonEncode(corpo)), status, headers: {'content-type': 'application/json'});

void main() {
  group('ApiClient', () {
    test('manda o token e o corpo em JSON', () async {
      late http.Request recebida;
      final api = PimApi(ApiClient(
        baseUrl: 'http://servidor:5090/',
        token: 'abc',
        cliente: MockClient((req) async {
          recebida = req;
          return json({'codigo': 'CX00000009', 'pedidoNumero': 5002, 'caixasFaltando': 0, 'pedidoReaberto': true}, 200);
        }),
      ));

      final r = await api.biparRetorno('cx00000009');

      expect(recebida.url.toString(), 'http://servidor:5090/retornos/caixas');
      expect(recebida.headers['Authorization'], 'Bearer abc');
      expect(jsonDecode(recebida.body), {'codigoCaixa': 'cx00000009'});
      expect(r.pedidoReaberto, isTrue);
    });

    test('400 vira ApiException com a mensagem da regra', () async {
      final api = PimApi(ApiClient(
        baseUrl: 'http://x',
        cliente: MockClient((_) async => json({'erro': 'RN11: faltam 8 caixas para bipar. O veículo não pode sair.'}, 400)),
      ));

      expect(
        () => api.saida('VEICULO:2'),
        throwsA(isA<ApiException>().having((e) => e.mensagem, 'mensagem', startsWith('RN11'))),
      );
    });

    test('401 avisa que o login caiu', () async {
      var perdeu = false;
      final cliente = ApiClient(baseUrl: 'http://x', cliente: MockClient((_) async => http.Response('', 401)))
        ..aoPerderLogin = () => perdeu = true;

      await expectLater(PimApi(cliente).entregas(), throwsA(isA<ApiException>().having((e) => e.naoAutenticado, '401', isTrue)));
      expect(perdeu, isTrue);
    });

    test('403 e falha de rede têm mensagem própria', () async {
      final semPermissao = PimApi(ApiClient(baseUrl: 'http://x', cliente: MockClient((_) async => http.Response('', 403))));
      await expectLater(semPermissao.entregas(), throwsA(isA<ApiException>().having((e) => e.status, 'status', 403)));

      final semRede = PimApi(ApiClient(baseUrl: 'http://x', cliente: MockClient((_) async => throw Exception('rede'))));
      await expectLater(semRede.entregas(), throwsA(isA<ApiException>().having((e) => e.status, 'status', 0)));
    });
  });

  group('Modelos', () {
    test('login', () {
      final u = Usuario.fromJson({'token': 't', 'nome': 'Diego Rocha', 'perfil': 'Operador', 'expiraEm': '2026-11-09T00:00:00Z'});
      expect(u.perfil, Perfil.operador);
    });

    test('entrada: totais e diferença', () {
      final e = DetalheEntrada.fromJson({
        'id': 3, 'notaNumero': 9010, 'fornecedor': 'Fornecedor Alfa Ltda', 'status': 'EmAndamento',
        'linhas': [
          {'produto': 'Produto B', 'naNota': 5, 'bipadas': 4, 'diferenca': -1},
          {'produto': 'Produto C', 'naNota': 2, 'bipadas': 2, 'diferenca': 0},
        ],
      });
      expect(e.totalNaNota, 7);
      expect(e.totalBipadas, 6);
      expect(e.bateComANota, isFalse);
      expect(e.emAndamento, isTrue);
    });

    test('carregamento completo', () {
      final c = ResumoCarregamento.fromJson({
        'id': 1, 'placa': 'DEF4G56', 'motorista': 'Marcos Silva', 'dataPrevista': '2026-11-08T00:00:00',
        'status': 'Planejado', 'totalCaixas': 20, 'caixasBipadas': 20, 'faltando': [],
      });
      expect(c.completo, isTrue);
    });

    test('entregas pendentes', () {
      final e = EntregasDoMotorista.fromJson({
        'carregamentoId': 1, 'placa': 'DEF4G56', 'dataSaida': '2026-11-08T10:30:00', 'status': 'EmRota',
        'pedidos': [
          {'id': 1, 'pedidoNumero': 5001, 'cliente': 'A', 'endereco': 'Rua 1', 'caixas': 12, 'resultado': 'Entregue', 'motivo': null},
          {'id': 2, 'pedidoNumero': 5002, 'cliente': 'B', 'endereco': 'Rua 2', 'caixas': 8, 'resultado': 'Pendente', 'motivo': null},
        ],
      });
      expect(e.pendentes, 1);
      expect(e.dataSaida!.hour, 10);
    });
  });
}
