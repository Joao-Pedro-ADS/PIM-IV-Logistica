import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

/// Abre a câmera para "bipar" um código de barras ou QR Code e devolve o texto lido.
/// Sempre dá para digitar o código: etiqueta rasgada, câmera ruim ou celular sem câmera.
Future<String?> lerCodigo(BuildContext context, {required String titulo, String? dica}) =>
    Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => LeitorCodigoPage(titulo: titulo, dica: dica)),
    );

class LeitorCodigoPage extends StatefulWidget {
  final String titulo;
  final String? dica;

  const LeitorCodigoPage({super.key, required this.titulo, this.dica});

  @override
  State<LeitorCodigoPage> createState() => _LeitorCodigoPageState();
}

class _LeitorCodigoPageState extends State<LeitorCodigoPage> {
  final _digitado = TextEditingController();
  MobileScannerController? _camera;
  bool _lido = false;

  // A câmera só existe no Android/iOS. No computador (testes), só a digitação.
  bool get _temCamera => !kIsWeb && (defaultTargetPlatform == TargetPlatform.android || defaultTargetPlatform == TargetPlatform.iOS);

  @override
  void initState() {
    super.initState();
    if (_temCamera) _camera = MobileScannerController(detectionSpeed: DetectionSpeed.noDuplicates);
  }

  @override
  void dispose() {
    _camera?.dispose();
    _digitado.dispose();
    super.dispose();
  }

  void _devolver(String codigo) {
    codigo = codigo.trim();
    if (_lido || codigo.isEmpty) return;
    _lido = true;
    HapticFeedback.mediumImpact();
    Navigator.of(context).pop(codigo);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.titulo)),
      body: Column(
        children: [
          Expanded(
            child: _camera == null
                ? const Center(child: Text('Câmera indisponível neste aparelho. Digite o código abaixo.'))
                : Stack(
                    alignment: Alignment.center,
                    children: [
                      MobileScanner(
                        controller: _camera,
                        onDetect: (captura) {
                          final valor = captura.barcodes.firstOrNull?.rawValue;
                          if (valor != null) _devolver(valor);
                        },
                      ),
                      // moldura para mirar
                      IgnorePointer(
                        child: Container(
                          width: 260,
                          height: 180,
                          decoration: BoxDecoration(
                            border: Border.all(color: Colors.white, width: 3),
                            borderRadius: BorderRadius.circular(12),
                          ),
                        ),
                      ),
                    ],
                  ),
          ),
          SafeArea(
            top: false,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (widget.dica != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: Text(widget.dica!, style: Theme.of(context).textTheme.bodyMedium),
                    ),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          key: const Key('campoCodigo'),
                          controller: _digitado,
                          autofocus: _camera == null,
                          textCapitalization: TextCapitalization.characters,
                          decoration: const InputDecoration(labelText: 'Ou digite o código', border: OutlineInputBorder()),
                          onSubmitted: _devolver,
                        ),
                      ),
                      const SizedBox(width: 8),
                      FilledButton(
                        key: const Key('confirmarCodigo'),
                        onPressed: () => _devolver(_digitado.text),
                        child: const Text('OK'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
