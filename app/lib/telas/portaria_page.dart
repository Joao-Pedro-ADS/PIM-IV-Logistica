import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../api/modelos.dart';
import '../sessao.dart';
import '../widgets/comuns.dart';
import '../widgets/leitor_codigo.dart';

// RF05: chegada do veículo do fornecedor (não tem cadastro: só a placa)
class ChegadaFornecedorPage extends StatefulWidget {
  final Sessao sessao;

  const ChegadaFornecedorPage({super.key, required this.sessao});

  @override
  State<ChegadaFornecedorPage> createState() => _ChegadaFornecedorPageState();
}

class _ChegadaFornecedorPageState extends State<ChegadaFornecedorPage> {
  final _placa = TextEditingController();
  final List<String> _registradas = [];
  bool _enviando = false;

  Future<void> _registrar() async {
    setState(() => _enviando = true);
    try {
      await widget.sessao.api.chegadaFornecedor(_placa.text);
      if (!mounted) return;
      final placa = _placa.text.trim().toUpperCase().replaceAll('-', '');
      setState(() {
        _registradas.insert(0, '$placa às ${hora(DateTime.now())}');
        _placa.clear();
      });
      avisar(context, 'Chegada de $placa registrada.');
    } on ApiException catch (e) {
      if (mounted) avisar(context, e.mensagem, erro: true);
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chegada de fornecedor')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            TextField(
              key: const Key('campoPlaca'),
              controller: _placa,
              textCapitalization: TextCapitalization.characters,
              style: const TextStyle(fontSize: 24, letterSpacing: 2),
              decoration: const InputDecoration(labelText: 'Placa do veículo', hintText: 'ABC1D23', border: OutlineInputBorder()),
              onSubmitted: (_) => _registrar(),
            ),
            const SizedBox(height: 16),
            BotaoGrande(chave: const Key('registrarChegada'), icone: Icons.check, texto: 'Registrar chegada', onPressed: _enviando ? null : _registrar),
            const SizedBox(height: 24),
            for (final r in _registradas) ListTile(leading: const Icon(Icons.local_shipping_outlined), title: Text(r)),
          ],
        ),
      ),
    );
  }
}

// RF12 (saída) e RF14 (volta) pelo QR Code do veículo
class MovimentoVeiculoPage extends StatefulWidget {
  final Sessao sessao;
  final bool saida;

  const MovimentoVeiculoPage({super.key, required this.sessao, required this.saida});

  @override
  State<MovimentoVeiculoPage> createState() => _MovimentoVeiculoPageState();
}

class _MovimentoVeiculoPageState extends State<MovimentoVeiculoPage> {
  ResumoCarregamento? _resultado;
  String? _erro;
  DateTime? _quando;
  bool _enviando = false;

  String get _nome => widget.saida ? 'Saída' : 'Volta';

  Future<void> _lerVeiculo() async {
    final qr = await lerCodigo(context, titulo: '$_nome: QR do veículo', dica: 'Leia a etiqueta QR colada no veículo.');
    if (qr == null) return;
    setState(() {
      _enviando = true;
      _erro = null;
      _resultado = null;
    });
    try {
      final api = widget.sessao.api;
      final r = widget.saida ? await api.saida(qr) : await api.volta(qr);
      if (!mounted) return;
      setState(() {
        _resultado = r;
        _quando = DateTime.now();
      });
      avisar(context, '$_nome do veículo ${r.placa} registrada.');
    } on ApiException catch (e) {
      // RN11: o porteiro precisa ver bem por que o veículo não pode sair
      if (mounted) {
        setState(() => _erro = e.mensagem);
        avisar(context, e.mensagem, erro: true);
      }
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final r = _resultado;
    final tema = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text('$_nome de veículo')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            BotaoGrande(
              chave: const Key('lerQrPortaria'),
              icone: Icons.qr_code_scanner,
              texto: 'Ler QR do veículo',
              onPressed: _enviando ? null : _lerVeiculo,
            ),
            const SizedBox(height: 24),
            if (_enviando) const Center(child: CircularProgressIndicator()),
            if (_erro != null)
              Card(
                color: tema.colorScheme.errorContainer,
                child: ListTile(
                  leading: Icon(Icons.block, color: tema.colorScheme.onErrorContainer, size: 36),
                  title: Text(widget.saida ? 'Saída NÃO liberada' : 'Volta não registrada',
                      style: TextStyle(color: tema.colorScheme.onErrorContainer, fontWeight: FontWeight.bold)),
                  subtitle: Text(_erro!, key: const Key('erroPortaria'), style: TextStyle(color: tema.colorScheme.onErrorContainer)),
                ),
              ),
            if (r != null)
              Card(
                color: Colors.green.shade100,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: DefaultTextStyle(
                    style: const TextStyle(color: Colors.black87, fontSize: 16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(widget.saida ? 'Saída liberada' : 'Volta registrada',
                            style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: Colors.black87)),
                        const SizedBox(height: 8),
                        // RN12: veículo, motorista e horário
                        Text('Veículo: ${r.placa}'),
                        Text('Motorista: ${r.motorista}'),
                        Text('Horário: ${hora(_quando!)}'),
                        Text('Caixas: ${r.caixasBipadas}'),
                        Text('Carregamento #${r.id}: ${r.status}', key: const Key('statusPortaria')),
                      ],
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
