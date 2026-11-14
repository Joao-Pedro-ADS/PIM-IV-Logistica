import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

/// Aviso no rodapé: verde para sucesso, vermelho para regra recusada.
/// Também vibra, para o operador perceber sem olhar a tela (acessibilidade).
void avisar(BuildContext context, String mensagem, {bool erro = false}) {
  if (erro) {
    HapticFeedback.heavyImpact();
  } else {
    HapticFeedback.lightImpact();
  }
  final cores = Theme.of(context).colorScheme;
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(
      content: Text(mensagem, style: TextStyle(color: erro ? cores.onError : null, fontSize: 16)),
      backgroundColor: erro ? cores.error : Colors.green.shade700,
      duration: Duration(seconds: erro ? 5 : 3),
    ));
}

/// Botão grande, fácil de acertar com luva ou com uma mão só.
class BotaoGrande extends StatelessWidget {
  final IconData icone;
  final String texto;
  final VoidCallback? onPressed;
  final Key? chave;

  const BotaoGrande({super.key, required this.icone, required this.texto, this.onPressed, this.chave});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 64,
      child: FilledButton.icon(
        key: chave,
        onPressed: onPressed,
        icon: Icon(icone, size: 28),
        label: Text(texto, style: const TextStyle(fontSize: 18)),
      ),
    );
  }
}

/// Barra de progresso com o texto "12 de 20 caixas".
class Progresso extends StatelessWidget {
  final int feito;
  final int total;
  final String unidade;

  const Progresso({super.key, required this.feito, required this.total, this.unidade = 'caixas'});

  @override
  Widget build(BuildContext context) {
    final completo = total > 0 && feito >= total;
    return Semantics(
      label: '$feito de $total $unidade',
      excludeSemantics: true, // o leitor de tela fala o rótulo uma vez só
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('$feito de $total $unidade',
              style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    color: completo ? Colors.green.shade700 : null,
                    fontWeight: FontWeight.bold,
                  )),
          const SizedBox(height: 8),
          LinearProgressIndicator(
            value: total == 0 ? 0 : (feito / total).clamp(0, 1).toDouble(),
            minHeight: 12,
            borderRadius: BorderRadius.circular(6),
            color: completo ? Colors.green.shade700 : null,
          ),
        ],
      ),
    );
  }
}

/// Tela de "nada para fazer" com botão de tentar de novo.
class Vazio extends StatelessWidget {
  final IconData icone;
  final String mensagem;
  final VoidCallback? onRecarregar;

  const Vazio({super.key, required this.icone, required this.mensagem, this.onRecarregar});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icone, size: 64, color: Theme.of(context).colorScheme.outline),
            const SizedBox(height: 16),
            Text(mensagem, textAlign: TextAlign.center, style: Theme.of(context).textTheme.titleMedium),
            if (onRecarregar != null) ...[
              const SizedBox(height: 16),
              OutlinedButton.icon(onPressed: onRecarregar, icon: const Icon(Icons.refresh), label: const Text('Atualizar')),
            ],
          ],
        ),
      ),
    );
  }
}

String hora(DateTime d) => '${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')}';
