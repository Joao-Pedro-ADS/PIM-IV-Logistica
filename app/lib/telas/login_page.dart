import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../sessao.dart';

// RF01: login com os perfis do app (conferente, operador, porteiro, motorista)
class LoginPage extends StatefulWidget {
  final Sessao sessao;

  const LoginPage({super.key, required this.sessao});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  late final _servidor = TextEditingController(text: widget.sessao.servidor);
  final _login = TextEditingController();
  final _senha = TextEditingController();
  String? _erro;
  bool _carregando = false;

  Future<void> _entrar() async {
    if (_login.text.trim().isEmpty || _senha.text.isEmpty) {
      setState(() => _erro = 'Informe o login e a senha.');
      return;
    }
    setState(() {
      _carregando = true;
      _erro = null;
    });
    try {
      await widget.sessao.entrar(_servidor.text, _login.text, _senha.text);
    } on ApiException catch (e) {
      if (mounted) setState(() => _erro = e.mensagem);
    } finally {
      if (mounted) setState(() => _carregando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: AutofillGroup(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Icon(Icons.local_shipping, size: 72, color: Theme.of(context).colorScheme.primary),
                    const SizedBox(height: 8),
                    Text('PIM Logística',
                        textAlign: TextAlign.center, style: Theme.of(context).textTheme.headlineMedium),
                    const SizedBox(height: 4),
                    const Text('Armazém, portaria e entregas', textAlign: TextAlign.center),
                    const SizedBox(height: 32),
                    TextField(
                      key: const Key('campoLogin'),
                      controller: _login,
                      autofillHints: const [AutofillHints.username],
                      decoration: const InputDecoration(labelText: 'Login', border: OutlineInputBorder(), prefixIcon: Icon(Icons.person)),
                      textInputAction: TextInputAction.next,
                    ),
                    const SizedBox(height: 16),
                    TextField(
                      key: const Key('campoSenha'),
                      controller: _senha,
                      obscureText: true,
                      autofillHints: const [AutofillHints.password],
                      decoration: const InputDecoration(labelText: 'Senha', border: OutlineInputBorder(), prefixIcon: Icon(Icons.lock)),
                      onSubmitted: (_) => _entrar(),
                    ),
                    if (_erro != null) ...[
                      const SizedBox(height: 16),
                      Text(_erro!,
                          key: const Key('erroLogin'),
                          style: TextStyle(color: Theme.of(context).colorScheme.error, fontSize: 16)),
                    ],
                    const SizedBox(height: 24),
                    SizedBox(
                      height: 56,
                      child: FilledButton(
                        key: const Key('botaoEntrar'),
                        onPressed: _carregando ? null : _entrar,
                        child: _carregando
                            ? const SizedBox(width: 24, height: 24, child: CircularProgressIndicator(strokeWidth: 3))
                            : const Text('Entrar', style: TextStyle(fontSize: 18)),
                      ),
                    ),
                    const SizedBox(height: 24),
                    ExpansionTile(
                      title: const Text('Servidor'),
                      subtitle: Text(_servidor.text),
                      children: [
                        TextField(
                          key: const Key('campoServidor'),
                          controller: _servidor,
                          keyboardType: TextInputType.url,
                          decoration: const InputDecoration(
                            border: OutlineInputBorder(),
                            helperText: 'Emulador: http://10.0.2.2:5090 · Celular: http://IP-do-computador:5090',
                          ),
                          onChanged: (_) => setState(() {}),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
