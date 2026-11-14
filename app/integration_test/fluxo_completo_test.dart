// Teste de ponta a ponta: o app de verdade falando com a Pim.Api de verdade.
//
// Antes de rodar:
//   1. banco zerado, Web (5080) e API (5090) no ar;
//   2. um carregamento criado no Web: DEF4G56 + Marcos Silva + pedidos 5001 e 5002.
// Rodar:  flutter test integration_test -d linux --dart-define=SERVIDOR=http://localhost:5090

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:pim_app/main.dart';
import 'package:pim_app/sessao.dart';
import 'package:shared_preferences/shared_preferences.dart';

const servidor = String.fromEnvironment('SERVIDOR', defaultValue: 'http://localhost:5090');

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  // espera algo aparecer na tela (as chamadas à API são reais e levam tempo)
  Future<void> esperar(WidgetTester tester, Finder alvo, {int segundos = 20}) async {
    for (var i = 0; i < segundos * 10; i++) {
      await tester.pump(const Duration(milliseconds: 100));
      if (alvo.evaluate().isNotEmpty) return;
    }
    throw TestFailure('Não apareceu na tela: $alvo');
  }

  Future<void> tocar(WidgetTester tester, Finder alvo) async {
    await esperar(tester, alvo);
    await tester.ensureVisible(alvo.first);
    await tester.pump(const Duration(milliseconds: 300));
    await tester.tap(alvo.first);
    await tester.pump(const Duration(milliseconds: 300));
  }

  Future<void> entrar(WidgetTester tester, String login) async {
    await esperar(tester, find.byKey(const Key('campoLogin')));
    await tester.enterText(find.byKey(const Key('campoLogin')), login);
    await tester.enterText(find.byKey(const Key('campoSenha')), '123456');
    await tocar(tester, find.byKey(const Key('botaoEntrar')));
    await esperar(tester, find.byKey(const Key('sair')));
  }

  // toca no "voltar" da tela de cima (espera a animação de troca de tela terminar)
  Future<void> voltar(WidgetTester tester) async {
    await tester.tap(find.byType(BackButton).last);
    await tester.pump(const Duration(milliseconds: 600));
  }

  Future<void> sair(WidgetTester tester) async {
    // volta para o início, se estiver numa tela interna
    await tester.pump(const Duration(milliseconds: 600));
    while (find.byType(BackButton).evaluate().isNotEmpty) {
      await voltar(tester);
    }
    await tocar(tester, find.byKey(const Key('sair')));
    await esperar(tester, find.byKey(const Key('campoLogin')));
  }

  // "bipa" digitando no leitor (no computador não tem câmera)
  Future<void> bipar(WidgetTester tester, String codigo) async {
    await esperar(tester, find.byKey(const Key('campoCodigo')));
    await tester.pump(const Duration(milliseconds: 400)); // termina a animação de abrir
    await tester.showKeyboard(find.byKey(const Key('campoCodigo')));
    await tester.enterText(find.byKey(const Key('campoCodigo')), codigo);
    // confirma com Enter, como faz o leitor de código de barras
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await tester.pump(const Duration(milliseconds: 300));
  }

  Future<void> sairDoLeitor(WidgetTester tester) async {
    await esperar(tester, find.byKey(const Key('campoCodigo')));
    await tester.pump(const Duration(milliseconds: 600));
    await voltar(tester);
  }

  testWidgets('ciclo completo de um pedido pelo app', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(PimApp(sessao: await Sessao.carregar()));

    // ---------- Login: servidor e perfil recusado ----------
    await tocar(tester, find.text('Servidor'));
    await tester.enterText(find.byKey(const Key('campoServidor')), servidor);
    await tester.enterText(find.byKey(const Key('campoLogin')), 'gerente');
    await tester.enterText(find.byKey(const Key('campoSenha')), '123456');
    await tocar(tester, find.byKey(const Key('botaoEntrar')));
    await esperar(tester, find.text('Este perfil usa o sistema Web.'));

    // ---------- RF06 / RF07: entrada da nota 9010 (5 caixas do Produto B) ----------
    await entrar(tester, 'conferente');
    await tocar(tester, find.byKey(const Key('novaEntrada')));
    await tocar(tester, find.byKey(const Key('biparNota')));
    await bipar(tester, '35261111222333000181550010000090101000090100');
    await esperar(tester, find.text('Nota 9010'));

    await tocar(tester, find.byKey(const Key('biparCaixa')));
    await bipar(tester, '7891000100103'); // Produto A: não está na nota
    await esperar(tester, find.textContaining('RN02'));

    await tocar(tester, find.byKey(const Key('biparCaixa')));
    for (var i = 1; i <= 5; i++) {
      await bipar(tester, '7891000200206');
      await esperar(tester, find.text('$i de 5 caixas', skipOffstage: false));
    }
    await sairDoLeitor(tester);
    expect(find.text('CX00000043'), findsOneWidget); // RN01: etiqueta gerada pelo sistema

    await tocar(tester, find.byKey(const Key('finalizarEntrada')));
    await tocar(tester, find.byKey(const Key('confirmarFinalizar')));
    await esperar(tester, find.text('Entrada finalizada'));
    await sair(tester);

    // ---------- RF05 e RN11 na portaria ----------
    await entrar(tester, 'porteiro');
    await tocar(tester, find.byKey(const Key('chegadaFornecedor')));
    await tester.enterText(find.byKey(const Key('campoPlaca')), 'xyz-1a23');
    await tocar(tester, find.byKey(const Key('registrarChegada')));
    await esperar(tester, find.textContaining('XYZ1A23 às'));
    await voltar(tester);

    await tocar(tester, find.byKey(const Key('saida')));
    await tocar(tester, find.byKey(const Key('lerQrPortaria')));
    await bipar(tester, 'VEICULO:2');
    await esperar(tester, find.text('Saída NÃO liberada'));
    expect(find.textContaining('RN11'), findsWidgets);
    await sair(tester);

    // ---------- RF11: operador carrega 20 caixas ----------
    await entrar(tester, 'operador');
    await tocar(tester, find.byKey(const Key('carregar')));
    await tocar(tester, find.byKey(const Key('lerVeiculo')));
    await bipar(tester, 'VEICULO:2');
    await esperar(tester, find.text('Veículo DEF4G56'));
    expect(find.text('0 de 20 caixas'), findsOneWidget);

    await tocar(tester, find.byKey(const Key('biparCaixasCarregamento')));
    await bipar(tester, 'CX00000037'); // Produto D: não pertence ao carregamento
    await esperar(tester, find.textContaining('RN10'));

    await tocar(tester, find.byKey(const Key('biparCaixasCarregamento')));
    // 5001: A8 B4 · 5002: A5 C3  ->  A 1..13, B 21..24, C 31..33
    final caixas = [for (var n = 1; n <= 13; n++) n, 21, 22, 23, 24, 31, 32, 33];
    for (var i = 0; i < caixas.length; i++) {
      await bipar(tester, 'cx${caixas[i].toString().padLeft(8, '0')}');
      await esperar(tester, find.text('${i + 1} de 20 caixas', skipOffstage: false));
    }
    await esperar(tester, find.text('Todas as caixas foram bipadas.'));
    await sair(tester);

    // ---------- RF12: saída liberada ----------
    await entrar(tester, 'porteiro');
    await tocar(tester, find.byKey(const Key('saida')));
    await tocar(tester, find.byKey(const Key('lerQrPortaria')));
    await bipar(tester, 'VEICULO:2');
    await esperar(tester, find.text('Saída liberada'));
    expect(find.textContaining('EmRota'), findsOneWidget);
    expect(find.text('Motorista: Marcos Silva'), findsOneWidget);
    await sair(tester);

    // ---------- RF13: 5001 entregue, 5002 não entregue com motivo ----------
    await entrar(tester, 'motorista');
    await tocar(tester, find.byKey(const Key('minhasEntregas')));
    await esperar(tester, find.byKey(const Key('pedido5002')));

    await tocar(tester, find.byKey(const Key('entregue5001')));
    await tocar(tester, find.byKey(const Key('confirmarEntrega')));
    await esperar(tester, find.text('1 de 2 pedidos registrados'));

    await tocar(tester, find.byKey(const Key('naoEntregue5002')));
    await esperar(tester, find.byKey(const Key('confirmarNaoEntrega')));
    // RN13: sem motivo, o botão fica desativado
    final botao = tester.widget<FilledButton>(find.byKey(const Key('confirmarNaoEntrega')));
    expect(botao.onPressed, isNull);
    await tocar(tester, find.text('Cliente fechado'));
    await tocar(tester, find.byKey(const Key('confirmarNaoEntrega')));
    await esperar(tester, find.text('Motivo: Cliente fechado'));
    expect(find.text('2 de 2 pedidos registrados'), findsOneWidget);
    await sair(tester);

    // ---------- RF14 / RF16: volta encerra o carregamento ----------
    await entrar(tester, 'porteiro');
    await tocar(tester, find.byKey(const Key('volta')));
    await tocar(tester, find.byKey(const Key('lerQrPortaria')));
    await bipar(tester, 'VEICULO:2');
    await esperar(tester, find.text('Volta registrada'));
    expect(find.textContaining('Encerrado'), findsOneWidget);
    await sair(tester);

    // ---------- RF15: retorno das 8 caixas do 5002 ----------
    await entrar(tester, 'conferente');
    await tocar(tester, find.byKey(const Key('retorno')));
    await tocar(tester, find.byKey(const Key('biparRetorno')));
    final do5002 = [9, 10, 11, 12, 13, 31, 32, 33];
    for (final n in do5002) {
      await bipar(tester, 'CX${n.toString().padLeft(8, '0')}');
      await esperar(tester, find.text('CX${n.toString().padLeft(8, '0')}', skipOffstage: false));
    }
    await sairDoLeitor(tester);
    expect(find.text('Pedido 5002 voltou para Aberto'), findsOneWidget);
    await sair(tester);
  }, timeout: const Timeout(Duration(minutes: 8)));
}
