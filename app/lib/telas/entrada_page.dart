import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../api/modelos.dart';
import '../sessao.dart';
import '../widgets/comuns.dart';
import '../widgets/leitor_codigo.dart';

// RF06 / RF07: o conferente bipa a nota, depois cada caixa, e finaliza
class EntradaPage extends StatefulWidget {
  final Sessao sessao;
  final int? recebimentoId;

  const EntradaPage({super.key, required this.sessao, this.recebimentoId});

  @override
  State<EntradaPage> createState() => _EntradaPageState();
}

class _EntradaPageState extends State<EntradaPage> {
  DetalheEntrada? _entrada;
  CaixaCriada? _ultimaCaixa;
  bool _carregando = false;

  @override
  void initState() {
    super.initState();
    if (widget.recebimentoId != null) _executar(() => widget.sessao.api.entrada(widget.recebimentoId!));
  }

  Future<void> _executar(Future<DetalheEntrada> Function() acao) async {
    setState(() => _carregando = true);
    try {
      final entrada = await acao();
      setState(() => _entrada = entrada);
      widget.sessao.entradaAtual = entrada.emAndamento ? entrada.id : null;
    } on ApiException catch (e) {
      if (mounted) avisar(context, e.mensagem, erro: true);
    } finally {
      if (mounted) setState(() => _carregando = false);
    }
  }

  Future<void> _biparNota() async {
    final chave = await lerCodigo(context,
        titulo: 'Bipar nota fiscal', dica: 'Leia o código de barras do DANFE (chave de 44 números).');
    if (chave != null) await _executar(() => widget.sessao.api.iniciarEntrada(chave));
  }

  // Bipa em sequência: a câmera reabre sozinha até o conferente voltar
  Future<void> _biparCaixas() async {
    final id = _entrada!.id;
    while (mounted) {
      final codigo = await lerCodigo(context,
          titulo: 'Bipar caixa', dica: 'Leia o código de barras do produto. Volte quando terminar.');
      if (codigo == null || !mounted) break;
      try {
        final caixa = await widget.sessao.api.biparProduto(id, codigo);
        final entrada = await widget.sessao.api.entrada(id);
        if (!mounted) return;
        setState(() {
          _ultimaCaixa = caixa;
          _entrada = entrada;
        });
        avisar(context, 'Etiqueta ${caixa.codigoCaixa} — ${caixa.produto}');
      } on ApiException catch (e) {
        if (mounted) avisar(context, e.mensagem, erro: true);
        break; // para a sequência para o conferente ler o erro
      }
    }
  }

  Future<void> _finalizar() async {
    final entrada = _entrada!;
    final confirmou = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Finalizar entrada?'),
        content: Text(entrada.bateComANota
            ? 'As caixas batem com a nota.'
            : 'Há diferença com a nota. A entrada vai ficar aguardando a aprovação do gerente (RN03).'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Voltar')),
          FilledButton(key: const Key('confirmarFinalizar'), onPressed: () => Navigator.pop(ctx, true), child: const Text('Finalizar')),
        ],
      ),
    );
    if (confirmou != true) return;
    await _executar(() => widget.sessao.api.finalizarEntrada(entrada.id));
    if (mounted && _entrada != null && !_entrada!.emAndamento) {
      avisar(context,
          _entrada!.status == 'Finalizado' ? 'Entrada finalizada.' : 'Entrada enviada para aprovação do gerente.');
    }
  }

  @override
  Widget build(BuildContext context) {
    final entrada = _entrada;
    return Scaffold(
      appBar: AppBar(title: Text(entrada == null ? 'Nova entrada' : 'Nota ${entrada.notaNumero}')),
      body: SafeArea(
        child: _carregando && entrada == null
            ? const Center(child: CircularProgressIndicator())
            : entrada == null
                ? Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Vazio(icone: Icons.receipt_long, mensagem: 'Bipe o código de barras da nota fiscal para começar.'),
                        BotaoGrande(chave: const Key('biparNota'), icone: Icons.qr_code_scanner, texto: 'Bipar nota fiscal', onPressed: _biparNota),
                      ],
                    ),
                  )
                : _detalhe(entrada),
      ),
    );
  }

  Widget _detalhe(DetalheEntrada entrada) {
    final tema = Theme.of(context);
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(entrada.fornecedor, style: tema.textTheme.titleMedium),
        Text('Status: ${entrada.status}', key: const Key('statusEntrada')),
        const SizedBox(height: 16),
        Progresso(feito: entrada.totalBipadas, total: entrada.totalNaNota),
        const SizedBox(height: 16),
        if (_ultimaCaixa != null)
          Card(
            color: tema.colorScheme.primaryContainer,
            child: ListTile(
              leading: const Icon(Icons.label, size: 36),
              title: Text(_ultimaCaixa!.codigoCaixa,
                  key: const Key('ultimaEtiqueta'), style: tema.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
              subtitle: Text('Escreva/cole esta etiqueta na caixa · ${_ultimaCaixa!.produto}'),
            ),
          ),
        const SizedBox(height: 8),
        Card(
          child: Column(
            children: [
              const ListTile(
                title: Text('Conferência com a nota'),
                trailing: Text('nota · bipadas'),
              ),
              for (final l in entrada.linhas)
                ListTile(
                  title: Text(l.produto),
                  trailing: Text(
                    '${l.naNota} · ${l.bipadas}${l.diferenca == 0 ? '' : ' (${l.diferenca > 0 ? '+' : ''}${l.diferenca})'}',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: l.diferenca == 0 ? Colors.green.shade700 : tema.colorScheme.error,
                    ),
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        if (entrada.emAndamento) ...[
          BotaoGrande(chave: const Key('biparCaixa'), icone: Icons.qr_code_scanner, texto: 'Bipar caixas', onPressed: _carregando ? null : _biparCaixas),
          const SizedBox(height: 12),
          OutlinedButton.icon(
            key: const Key('finalizarEntrada'),
            onPressed: _carregando ? null : _finalizar,
            icon: const Icon(Icons.done_all),
            label: const Text('Finalizar entrada'),
          ),
        ] else
          Card(
            color: entrada.status == 'Finalizado' ? Colors.green.shade100 : Colors.orange.shade100,
            child: ListTile(
              leading: Icon(entrada.status == 'Finalizado' ? Icons.check_circle : Icons.hourglass_top, color: Colors.black87),
              title: Text(
                entrada.status == 'Finalizado' ? 'Entrada finalizada' : 'Aguardando aprovação do gerente',
                style: const TextStyle(color: Colors.black87),
              ),
            ),
          ),
      ],
    );
  }
}
