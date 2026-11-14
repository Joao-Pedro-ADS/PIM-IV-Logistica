import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../api/modelos.dart';
import '../sessao.dart';
import '../widgets/comuns.dart';
import '../widgets/leitor_codigo.dart';

// RF15 / RN14: bipa as caixas que voltaram; na última, o pedido volta para Aberto
class RetornoPage extends StatefulWidget {
  final Sessao sessao;

  const RetornoPage({super.key, required this.sessao});

  @override
  State<RetornoPage> createState() => _RetornoPageState();
}

class _RetornoPageState extends State<RetornoPage> {
  final List<RetornoCaixa> _bipadas = [];

  Future<void> _biparCaixas() async {
    while (mounted) {
      final codigo = await lerCodigo(context,
          titulo: 'Caixa que voltou', dica: 'Leia a etiqueta da caixa (CX...). Volte quando terminar.');
      if (codigo == null || !mounted) break;
      try {
        final r = await widget.sessao.api.biparRetorno(codigo);
        if (!mounted) return;
        setState(() => _bipadas.insert(0, r));
        avisar(
            context,
            r.pedidoReaberto
                ? 'Pedido ${r.pedidoNumero} completo: voltou para Aberto.'
                : 'Caixa ${r.codigo} recebida. Faltam ${r.caixasFaltando} do pedido ${r.pedidoNumero}.');
      } on ApiException catch (e) {
        if (mounted) avisar(context, e.mensagem, erro: true);
        break;
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Caixas que voltaram')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            BotaoGrande(chave: const Key('biparRetorno'), icone: Icons.qr_code_scanner, texto: 'Bipar caixas', onPressed: _biparCaixas),
            const SizedBox(height: 16),
            if (_bipadas.isEmpty)
              const Vazio(icone: Icons.assignment_return, mensagem: 'Bipe as caixas de pedidos não entregues depois que o veículo voltar.')
            else
              for (final r in _bipadas)
                Card(
                  child: ListTile(
                    leading: Icon(r.pedidoReaberto ? Icons.check_circle : Icons.inventory_2,
                        color: r.pedidoReaberto ? Colors.green.shade700 : null),
                    title: Text(r.codigo),
                    subtitle: Text(r.pedidoReaberto
                        ? 'Pedido ${r.pedidoNumero} voltou para Aberto'
                        : 'Pedido ${r.pedidoNumero} · faltam ${r.caixasFaltando}'),
                  ),
                ),
          ],
        ),
      ),
    );
  }
}
