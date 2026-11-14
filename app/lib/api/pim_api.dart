import 'api_client.dart';
import 'modelos.dart';

/// Uma função por rota da Pim.Api (docs/api.md).
class PimApi {
  final ApiClient cliente;

  PimApi(this.cliente);

  // RF01
  Future<Usuario> login(String login, String senha) async =>
      Usuario.fromJson(await cliente.post('/auth/login', {'login': login, 'senha': senha}));

  // RF05
  Future<void> chegadaFornecedor(String placa) => cliente.post('/portaria/chegada-fornecedor', {'placa': placa});

  // RF06 / RF07
  Future<DetalheEntrada> iniciarEntrada(String chaveNota) async =>
      DetalheEntrada.fromJson(await cliente.post('/recebimentos', {'chaveNota': chaveNota}));

  Future<DetalheEntrada> entrada(int id) async => DetalheEntrada.fromJson(await cliente.get('/recebimentos/$id'));

  Future<CaixaCriada> biparProduto(int recebimentoId, String codigoProduto) async => CaixaCriada.fromJson(
      await cliente.post('/recebimentos/$recebimentoId/caixas', {'codigoProduto': codigoProduto}));

  Future<DetalheEntrada> finalizarEntrada(int id) async =>
      DetalheEntrada.fromJson(await cliente.post('/recebimentos/$id/finalizar'));

  // RF11
  Future<ResumoCarregamento> carregamentoDoVeiculo(String qr) async => ResumoCarregamento.fromJson(
      await cliente.get('/carregamentos/veiculo?qr=${Uri.encodeQueryComponent(qr)}'));

  Future<ResumoCarregamento> biparCaixaCarregamento(int carregamentoId, String codigoCaixa) async =>
      ResumoCarregamento.fromJson(
          await cliente.post('/carregamentos/$carregamentoId/caixas', {'codigoCaixa': codigoCaixa}));

  // RF12 / RF14
  Future<ResumoCarregamento> saida(String qr) async =>
      ResumoCarregamento.fromJson(await cliente.post('/portaria/saida', {'qr': qr}));

  Future<ResumoCarregamento> volta(String qr) async =>
      ResumoCarregamento.fromJson(await cliente.post('/portaria/volta', {'qr': qr}));

  // RF13
  Future<EntregasDoMotorista> entregas() async => EntregasDoMotorista.fromJson(await cliente.get('/entregas'));

  Future<EntregasDoMotorista> registrarEntrega(int id, {required bool entregue, String? motivo}) async =>
      EntregasDoMotorista.fromJson(await cliente.post('/entregas/$id', {'entregue': entregue, 'motivo': motivo}));

  // RF15
  Future<RetornoCaixa> biparRetorno(String codigoCaixa) async =>
      RetornoCaixa.fromJson(await cliente.post('/retornos/caixas', {'codigoCaixa': codigoCaixa}));
}
