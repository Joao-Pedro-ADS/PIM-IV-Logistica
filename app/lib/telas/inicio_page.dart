import 'package:flutter/material.dart';

import '../api/modelos.dart';
import '../sessao.dart';
import '../widgets/comuns.dart';
import 'carregamento_page.dart';
import 'entrada_page.dart';
import 'entregas_page.dart';
import 'portaria_page.dart';
import 'retorno_page.dart';

/// Menu do perfil: cada perfil vê só as suas funções.
class InicioPage extends StatefulWidget {
  final Sessao sessao;

  const InicioPage({super.key, required this.sessao});

  @override
  State<InicioPage> createState() => _InicioPageState();
}

class _InicioPageState extends State<InicioPage> {
  Sessao get sessao => widget.sessao;

  Future<void> _abrir(Widget tela) async {
    await Navigator.of(context).push(MaterialPageRoute(builder: (_) => tela));
    if (mounted) setState(() {}); // atualiza o "continuar entrada"
  }

  List<Widget> _funcoes(Perfil perfil) {
    switch (perfil) {
      case Perfil.conferente:
        final atual = sessao.entradaAtual;
        return [
          if (atual != null)
            BotaoGrande(
              chave: const Key('continuarEntrada'),
              icone: Icons.playlist_add_check,
              texto: 'Continuar entrada #$atual',
              onPressed: () => _abrir(EntradaPage(sessao: sessao, recebimentoId: atual)),
            ),
          BotaoGrande(
            chave: const Key('novaEntrada'),
            icone: Icons.move_to_inbox,
            texto: 'Nova entrada (RF06)',
            onPressed: () => _abrir(EntradaPage(sessao: sessao)),
          ),
          BotaoGrande(
            chave: const Key('retorno'),
            icone: Icons.assignment_return,
            texto: 'Caixas que voltaram (RF15)',
            onPressed: () => _abrir(RetornoPage(sessao: sessao)),
          ),
        ];
      case Perfil.operador:
        return [
          BotaoGrande(
            chave: const Key('carregar'),
            icone: Icons.qr_code_scanner,
            texto: 'Carregar veículo (RF11)',
            onPressed: () => _abrir(CarregamentoPage(sessao: sessao)),
          ),
        ];
      case Perfil.porteiro:
        return [
          BotaoGrande(
            chave: const Key('chegadaFornecedor'),
            icone: Icons.local_shipping_outlined,
            texto: 'Chegada de fornecedor (RF05)',
            onPressed: () => _abrir(ChegadaFornecedorPage(sessao: sessao)),
          ),
          BotaoGrande(
            chave: const Key('saida'),
            icone: Icons.logout,
            texto: 'Saída de veículo (RF12)',
            onPressed: () => _abrir(MovimentoVeiculoPage(sessao: sessao, saida: true)),
          ),
          BotaoGrande(
            chave: const Key('volta'),
            icone: Icons.login,
            texto: 'Volta de veículo (RF14)',
            onPressed: () => _abrir(MovimentoVeiculoPage(sessao: sessao, saida: false)),
          ),
        ];
      case Perfil.motorista:
        return [
          BotaoGrande(
            chave: const Key('minhasEntregas'),
            icone: Icons.route,
            texto: 'Minhas entregas (RF13)',
            onPressed: () => _abrir(EntregasPage(sessao: sessao)),
          ),
        ];
    }
  }

  static const _nomesPerfil = {
    Perfil.conferente: 'Conferente',
    Perfil.operador: 'Operador',
    Perfil.porteiro: 'Porteiro',
    Perfil.motorista: 'Motorista',
  };

  @override
  Widget build(BuildContext context) {
    final usuario = sessao.usuario!;
    return Scaffold(
      appBar: AppBar(
        title: const Text('PIM Logística'),
        actions: [
          IconButton(
            key: const Key('sair'),
            tooltip: 'Sair',
            icon: const Icon(Icons.logout),
            onPressed: sessao.sair,
          ),
        ],
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            Text('Olá, ${usuario.nome.split(' ').first}', style: Theme.of(context).textTheme.headlineSmall),
            Text(_nomesPerfil[usuario.perfil]!, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 32),
            for (final botao in _funcoes(usuario.perfil)) ...[botao, const SizedBox(height: 16)],
          ],
        ),
      ),
    );
  }
}
