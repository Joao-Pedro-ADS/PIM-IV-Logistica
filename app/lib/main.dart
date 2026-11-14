import 'package:flutter/material.dart';

import 'sessao.dart';
import 'telas/inicio_page.dart';
import 'telas/login_page.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(PimApp(sessao: await Sessao.carregar()));
}

class PimApp extends StatelessWidget {
  final Sessao sessao;

  const PimApp({super.key, required this.sessao});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'PIM Logística',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(colorSchemeSeed: const Color(0xFF1565C0), useMaterial3: true),
      darkTheme: ThemeData(colorSchemeSeed: const Color(0xFF1565C0), brightness: Brightness.dark, useMaterial3: true),
      // troca de tela sozinho quando o usuário entra ou sai (ou o token vence)
      home: ListenableBuilder(
        listenable: sessao,
        builder: (_, _) => sessao.usuario == null ? LoginPage(sessao: sessao) : InicioPage(sessao: sessao),
      ),
    );
  }
}
