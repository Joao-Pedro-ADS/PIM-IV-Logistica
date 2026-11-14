// Classes que espelham as respostas da API (docs/api.md).

/// Perfis que usam o app.
enum Perfil { conferente, operador, porteiro, motorista }

Perfil perfilDe(String texto) => Perfil.values.firstWhere((p) => p.name == texto.toLowerCase());

class Usuario {
  final String nome;
  final Perfil perfil;
  final String token;

  Usuario({required this.nome, required this.perfil, required this.token});

  factory Usuario.fromJson(Map<String, dynamic> j) =>
      Usuario(nome: j['nome'], perfil: perfilDe(j['perfil']), token: j['token']);
}

// ---------- Entrada (RF06 / RF07) ----------

class LinhaConferencia {
  final String produto;
  final int naNota;
  final int bipadas;
  final int diferenca;

  LinhaConferencia(this.produto, this.naNota, this.bipadas, this.diferenca);

  factory LinhaConferencia.fromJson(Map<String, dynamic> j) =>
      LinhaConferencia(j['produto'], j['naNota'], j['bipadas'], j['diferenca']);
}

class DetalheEntrada {
  final int id;
  final int notaNumero;
  final String fornecedor;
  final String status;
  final List<LinhaConferencia> linhas;

  DetalheEntrada(this.id, this.notaNumero, this.fornecedor, this.status, this.linhas);

  bool get emAndamento => status == 'EmAndamento';
  bool get bateComANota => linhas.every((l) => l.diferenca == 0);
  int get totalNaNota => linhas.fold(0, (s, l) => s + l.naNota);
  int get totalBipadas => linhas.fold(0, (s, l) => s + l.bipadas);

  factory DetalheEntrada.fromJson(Map<String, dynamic> j) => DetalheEntrada(
        j['id'],
        j['notaNumero'],
        j['fornecedor'],
        j['status'],
        (j['linhas'] as List).map((l) => LinhaConferencia.fromJson(l)).toList(),
      );
}

class CaixaCriada {
  final String codigoCaixa;
  final String produto;

  CaixaCriada(this.codigoCaixa, this.produto);

  factory CaixaCriada.fromJson(Map<String, dynamic> j) => CaixaCriada(j['codigoCaixa'], j['produto']);
}

// ---------- Carregamento e portaria (RF11 / RF12 / RF14) ----------

class FaltaProduto {
  final int pedidoNumero;
  final String produto;
  final String codigoProduto;
  final int faltam;

  FaltaProduto(this.pedidoNumero, this.produto, this.codigoProduto, this.faltam);

  factory FaltaProduto.fromJson(Map<String, dynamic> j) =>
      FaltaProduto(j['pedidoNumero'], j['produto'], j['codigoProduto'], j['faltam']);
}

class ResumoCarregamento {
  final int id;
  final String placa;
  final String motorista;
  final DateTime dataPrevista;
  final String status;
  final int totalCaixas;
  final int caixasBipadas;
  final List<FaltaProduto> faltando;

  ResumoCarregamento(this.id, this.placa, this.motorista, this.dataPrevista, this.status, this.totalCaixas,
      this.caixasBipadas, this.faltando);

  bool get completo => caixasBipadas >= totalCaixas;

  factory ResumoCarregamento.fromJson(Map<String, dynamic> j) => ResumoCarregamento(
        j['id'],
        j['placa'],
        j['motorista'],
        DateTime.parse(j['dataPrevista']),
        j['status'],
        j['totalCaixas'],
        j['caixasBipadas'],
        (j['faltando'] as List).map((f) => FaltaProduto.fromJson(f)).toList(),
      );
}

// ---------- Entregas (RF13) ----------

class EntregaPedido {
  final int id;
  final int pedidoNumero;
  final String cliente;
  final String endereco;
  final int caixas;
  final String resultado;
  final String? motivo;

  EntregaPedido(this.id, this.pedidoNumero, this.cliente, this.endereco, this.caixas, this.resultado, this.motivo);

  bool get pendente => resultado == 'Pendente';

  factory EntregaPedido.fromJson(Map<String, dynamic> j) => EntregaPedido(
      j['id'], j['pedidoNumero'], j['cliente'], j['endereco'], j['caixas'], j['resultado'], j['motivo']);
}

class EntregasDoMotorista {
  final int carregamentoId;
  final String placa;
  final DateTime? dataSaida;
  final String status;
  final List<EntregaPedido> pedidos;

  EntregasDoMotorista(this.carregamentoId, this.placa, this.dataSaida, this.status, this.pedidos);

  int get pendentes => pedidos.where((p) => p.pendente).length;

  factory EntregasDoMotorista.fromJson(Map<String, dynamic> j) => EntregasDoMotorista(
        j['carregamentoId'],
        j['placa'],
        j['dataSaida'] == null ? null : DateTime.parse(j['dataSaida']),
        j['status'],
        (j['pedidos'] as List).map((p) => EntregaPedido.fromJson(p)).toList(),
      );
}

// ---------- Retorno (RF15) ----------

class RetornoCaixa {
  final String codigo;
  final int pedidoNumero;
  final int caixasFaltando;
  final bool pedidoReaberto;

  RetornoCaixa(this.codigo, this.pedidoNumero, this.caixasFaltando, this.pedidoReaberto);

  factory RetornoCaixa.fromJson(Map<String, dynamic> j) =>
      RetornoCaixa(j['codigo'], j['pedidoNumero'], j['caixasFaltando'], j['pedidoReaberto']);
}
