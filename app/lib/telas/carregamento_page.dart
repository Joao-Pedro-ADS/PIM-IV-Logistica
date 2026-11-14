import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../api/modelos.dart';
import '../sessao.dart';
import '../widgets/comuns.dart';
import '../widgets/leitor_codigo.dart';

// RF11: o operador lê o QR do veículo e bipa as caixas que sobem nele (RN10)
class CarregamentoPage extends StatefulWidget {
  final Sessao sessao;

  const CarregamentoPage({super.key, required this.sessao});

  @override
  State<CarregamentoPage> createState() => _CarregamentoPageState();
}

class _CarregamentoPageState extends State<CarregamentoPage> {
  ResumoCarregamento? _carregamento;
  String? _qr;
  bool _carregando = false;

  Future<void> _lerVeiculo() async {
    final qr = await lerCodigo(context, titulo: 'QR Code do veículo', dica: 'Leia a etiqueta QR colada no veículo.');
    if (qr == null) return;
    setState(() => _carregando = true);
    try {
      final c = await widget.sessao.api.carregamentoDoVeiculo(qr);
      setState(() {
        _carregamento = c;
        _qr = qr;
      });
    } on ApiException catch (e) {
      if (mounted) avisar(context, e.mensagem, erro: true);
    } finally {
      if (mounted) setState(() => _carregando = false);
    }
  }

  Future<void> _atualizar() async {
    if (_qr == null) return;
    try {
      final c = await widget.sessao.api.carregamentoDoVeiculo(_qr!);
      if (mounted) setState(() => _carregamento = c);
    } on ApiException catch (e) {
      if (mounted) avisar(context, e.mensagem, erro: true);
    }
  }

  // Bipa em sequência até completar ou o operador voltar
  Future<void> _biparCaixas() async {
    while (mounted && !_carregamento!.completo) {
      final codigo = await lerCodigo(context,
          titulo: 'Bipar caixa (${_carregamento!.caixasBipadas}/${_carregamento!.totalCaixas})',
          dica: 'Leia a etiqueta da caixa (CX...). Volte quando terminar.');
      if (codigo == null || !mounted) break;
      try {
        final c = await widget.sessao.api.biparCaixaCarregamento(_carregamento!.id, codigo);
        if (!mounted) return;
        setState(() => _carregamento = c);
        avisar(context, c.completo ? 'Carregamento completo! O veículo pode sair.' : 'Caixa ${codigo.toUpperCase()} carregada.');
      } on ApiException catch (e) {
        // RN10: caixa recusada. Para a sequência para o operador separar a caixa.
        if (mounted) avisar(context, e.mensagem, erro: true);
        break;
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = _carregamento;
    return Scaffold(
      appBar: AppBar(title: Text(c == null ? 'Carregar veículo' : 'Veículo ${c.placa}')),
      body: SafeArea(
        child: _carregando
            ? const Center(child: CircularProgressIndicator())
            : c == null
                ? Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Vazio(icone: Icons.local_shipping, mensagem: 'Leia o QR Code do veículo para ver o carregamento.'),
                        BotaoGrande(chave: const Key('lerVeiculo'), icone: Icons.qr_code_scanner, texto: 'Ler QR do veículo', onPressed: _lerVeiculo),
                      ],
                    ),
                  )
                : RefreshIndicator(
                    onRefresh: _atualizar,
                    child: ListView(
                      padding: const EdgeInsets.all(16),
                      children: [
                        Text('Motorista: ${c.motorista}', style: Theme.of(context).textTheme.titleMedium),
                        Text('Carregamento #${c.id} · ${c.status}'),
                        const SizedBox(height: 16),
                        Progresso(key: const Key('progressoCarregamento'), feito: c.caixasBipadas, total: c.totalCaixas),
                        const SizedBox(height: 16),
                        if (c.completo)
                          Card(
                            color: Colors.green.shade100,
                            child: const ListTile(
                              leading: Icon(Icons.check_circle, color: Colors.black87),
                              title: Text('Todas as caixas foram bipadas.', style: TextStyle(color: Colors.black87)),
                              subtitle: Text('Avise a portaria: o veículo pode sair (RF12).', style: TextStyle(color: Colors.black87)),
                            ),
                          )
                        else ...[
                          BotaoGrande(chave: const Key('biparCaixasCarregamento'), icone: Icons.qr_code_scanner, texto: 'Bipar caixas', onPressed: _biparCaixas),
                          const SizedBox(height: 16),
                          Text('Falta subir', style: Theme.of(context).textTheme.titleMedium),
                          for (final f in c.faltando)
                            ListTile(
                              leading: CircleAvatar(child: Text('${f.faltam}')),
                              title: Text(f.produto),
                              subtitle: Text('Pedido ${f.pedidoNumero} · ${f.codigoProduto}'),
                            ),
                        ],
                      ],
                    ),
                  ),
      ),
    );
  }
}
