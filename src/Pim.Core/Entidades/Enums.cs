namespace Pim.Core.Entidades;

public enum Perfil { Gerente, Programador, Frota, Comercial, Conferente, Operador, Porteiro, Motorista }

public enum StatusPedido { Aberto, EmCarregamento, EmRota, Entregue, NaoEntregue, Cancelado }

public enum SituacaoVeiculo { Disponivel, EmRota, EmManutencao }

public enum StatusCarregamento { Planejado, EmRota, Encerrado, Cancelado }

public enum ResultadoEntrega { Pendente, Entregue, NaoEntregue }

public enum StatusRecebimento { EmAndamento, AguardandoAprovacao, Finalizado }

public enum StatusCaixa { NoArmazem, Carregada, EmRota, Entregue }

public enum TipoMovimentacao { Entrada, Carregamento, Saida, Entrega, Retorno }

public enum TipoRegistroPortaria { ChegadaFornecedor, SaidaFornecedor, SaidaFrota, VoltaFrota }
