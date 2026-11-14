using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

// Resumo de um carregamento para o app: o que já subiu e o que ainda falta
public record ResumoCarregamento(
    int Id, string Placa, string Motorista, DateTime DataPrevista, string Status,
    int TotalCaixas, int CaixasBipadas, List<FaltaProduto> Faltando);

public record FaltaProduto(int PedidoNumero, string Produto, string CodigoProduto, int Faltam);

// Regras usadas por mais de um serviço do carregamento
internal static class RegrasCarregamento
{
    // O QR Code do veículo (gerado no Web) tem o formato "VEICULO:{id}"
    public static int LerQrVeiculo(string? qr)
    {
        var texto = (qr ?? "").Trim().ToUpper();
        if (!texto.StartsWith("VEICULO:") || !int.TryParse(texto["VEICULO:".Length..], out int id))
            throw new RegraNegocioException("QR Code de veículo inválido.");
        return id;
    }

    // Quantas caixas de cada produto ainda faltam em cada pedido
    // precisa de: Pedidos -> PedidoVenda -> Itens -> Produto, e Caixas
    public static List<FaltaProduto> Faltando(Carregamento c) =>
        c.Pedidos
            .SelectMany(cp => cp.PedidoVenda!.Itens.Select(i => new FaltaProduto(
                cp.PedidoVenda.Numero,
                i.Produto!.Descricao,
                i.Produto.Codigo,
                i.Quantidade - c.Caixas.Count(cx => cx.PedidoVendaId == cp.PedidoVendaId && cx.ProdutoId == i.ProdutoId))))
            .Where(f => f.Faltam > 0)
            .ToList();

    public static ResumoCarregamento Resumir(Carregamento c) => new(
        c.Id, c.Veiculo!.Placa, c.Motorista!.Nome, c.DataPrevista, c.Status.ToString(),
        c.Pedidos.Sum(cp => cp.PedidoVenda!.TotalCaixas),
        c.Caixas.Count,
        Faltando(c));

    // RF16 / RN15: encerra quando todos os pedidos têm entrega registrada e o veículo já voltou
    public static void TentarEncerrar(Carregamento c)
    {
        if (c.Status == StatusCarregamento.EmRota
            && c.DataVolta != null
            && c.Pedidos.All(cp => cp.Resultado != ResultadoEntrega.Pendente))
        {
            c.Status = StatusCarregamento.Encerrado;
        }
    }
}
