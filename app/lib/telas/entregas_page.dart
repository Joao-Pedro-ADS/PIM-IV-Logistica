import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../api/modelos.dart';
import '../sessao.dart';
import '../widgets/comuns.dart';

// RF13: o motorista marca cada pedido como entregue ou não entregue (RN13: com motivo)
class EntregasPage extends StatefulWidget {
  final Sessao sessao;

  const EntregasPage({super.key, required this.sessao});

  @override
  State<EntregasPage> createState() => _EntregasPageState();
}

class _EntregasPageState extends State<EntregasPage> {
  EntregasDoMotorista? _entregas;
  String? _semEntregas;
  bool _carregando = true;

  static const _motivosComuns = ['Cliente fechado', 'Endereço não encontrado', 'Recusado pelo cliente', 'Mercadoria avariada'];

  @override
  void initState() {
    super.initState();
    _carregar();
  }

  Future<void> _carregar() async {
    setState(() => _carregando = true);
    try {
      final e = await widget.sessao.api.entregas();
      if (mounted) {
        setState(() {
          _entregas = e;
          _semEntregas = null;
        });
      }
    } on ApiException catch (e) {
      // 400 "Você não tem carregamento em rota." vira a tela vazia
      if (mounted) {
        setState(() {
          _entregas = null;
          _semEntregas = e.mensagem;
        });
      }
    } finally {
      if (mounted) setState(() => _carregando = false);
    }
  }

  Future<void> _registrar(EntregaPedido p, {required bool entregue}) async {
    String? motivo;
    if (!entregue) {
      motivo = await _pedirMotivo(p);
      if (motivo == null) return;
    } else {
      final ok = await showDialog<bool>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: Text('Confirmar entrega do pedido ${p.pedidoNumero}?'),
          content: Text('${p.cliente}\n${p.caixas} caixas'),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Voltar')),
            FilledButton(key: const Key('confirmarEntrega'), onPressed: () => Navigator.pop(ctx, true), child: const Text('Entregue')),
          ],
        ),
      );
      if (ok != true) return;
    }

    try {
      final e = await widget.sessao.api.registrarEntrega(p.id, entregue: entregue, motivo: motivo);
      if (!mounted) return;
      setState(() => _entregas = e);
      avisar(context, entregue ? 'Pedido ${p.pedidoNumero} entregue.' : 'Pedido ${p.pedidoNumero} não entregue: $motivo.');
    } on ApiException catch (e) {
      if (mounted) avisar(context, e.mensagem, erro: true);
    }
  }

  Future<String?> _pedirMotivo(EntregaPedido p) {
    final campo = TextEditingController();
    return showDialog<String>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => AlertDialog(
          title: Text('Pedido ${p.pedidoNumero} não entregue'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Motivo (obrigatório):'),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                runSpacing: 4,
                children: [
                  for (final m in _motivosComuns)
                    ChoiceChip(label: Text(m), selected: campo.text == m, onSelected: (_) => setLocal(() => campo.text = m)),
                ],
              ),
              const SizedBox(height: 8),
              TextField(
                key: const Key('campoMotivo'),
                controller: campo,
                decoration: const InputDecoration(border: OutlineInputBorder(), hintText: 'Outro motivo'),
                onChanged: (_) => setLocal(() {}),
              ),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Voltar')),
            FilledButton(
              key: const Key('confirmarNaoEntrega'),
              onPressed: campo.text.trim().isEmpty ? null : () => Navigator.pop(ctx, campo.text.trim()),
              child: const Text('Registrar'),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final e = _entregas;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Minhas entregas'),
        actions: [IconButton(tooltip: 'Atualizar', onPressed: _carregar, icon: const Icon(Icons.refresh))],
      ),
      body: SafeArea(
        child: _carregando && e == null
            ? const Center(child: CircularProgressIndicator())
            : e == null
                ? Vazio(icone: Icons.route, mensagem: _semEntregas ?? 'Nenhuma entrega.', onRecarregar: _carregar)
                : RefreshIndicator(
                    onRefresh: _carregar,
                    child: ListView(
                      padding: const EdgeInsets.all(16),
                      children: [
                        Text('Veículo ${e.placa}', style: Theme.of(context).textTheme.titleLarge),
                        if (e.dataSaida != null) Text('Saiu às ${hora(e.dataSaida!)}'),
                        const SizedBox(height: 12),
                        Progresso(feito: e.pedidos.length - e.pendentes, total: e.pedidos.length, unidade: 'pedidos registrados'),
                        if (e.pendentes == 0)
                          const Padding(
                            padding: EdgeInsets.only(top: 12),
                            child: Text('Tudo registrado. Volte ao armazém e passe pela portaria (RF14).'),
                          ),
                        const SizedBox(height: 12),
                        for (final p in e.pedidos) _cartao(p),
                      ],
                    ),
                  ),
      ),
    );
  }

  Widget _cartao(EntregaPedido p) {
    final tema = Theme.of(context);
    final cor = switch (p.resultado) {
      'Entregue' => Colors.green.shade700,
      'NaoEntregue' => tema.colorScheme.error,
      _ => tema.colorScheme.outline,
    };
    return Card(
      key: Key('pedido${p.pedidoNumero}'),
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text('Pedido ${p.pedidoNumero}', style: tema.textTheme.titleMedium)),
                Chip(
                  label: Text(p.resultado == 'NaoEntregue' ? 'Não entregue' : p.resultado, style: TextStyle(color: cor)),
                  side: BorderSide(color: cor),
                ),
              ],
            ),
            Text(p.cliente, style: tema.textTheme.bodyLarge),
            Text(p.endereco),
            Text('${p.caixas} caixas'),
            if (p.motivo != null) Text('Motivo: ${p.motivo}', style: TextStyle(color: tema.colorScheme.error)),
            if (p.pendente) ...[
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: FilledButton.icon(
                      key: Key('entregue${p.pedidoNumero}'),
                      onPressed: () => _registrar(p, entregue: true),
                      icon: const Icon(Icons.check),
                      label: const Text('Entregue'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                      key: Key('naoEntregue${p.pedidoNumero}'),
                      onPressed: () => _registrar(p, entregue: false),
                      icon: const Icon(Icons.close),
                      label: const Text('Não entregue'),
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}
