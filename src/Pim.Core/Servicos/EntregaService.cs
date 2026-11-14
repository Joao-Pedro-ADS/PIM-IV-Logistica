using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

// O que o motorista vê no app
public record EntregasDoMotorista(int CarregamentoId, string Placa, DateTime? DataSaida, string Status, List<EntregaPedido> Pedidos);

public record EntregaPedido(int Id, int PedidoNumero, string Cliente, string Endereco, int Caixas, string Resultado, string? Motivo);

// RF13 / RF16: o motorista registra cada pedido como entregue ou não entregue
public class EntregaService
{
    private readonly PimDbContext _db;

    public EntregaService(PimDbContext db) => _db = db;

    public async Task<EntregasDoMotorista> MinhasEntregasAsync(int usuarioId)
    {
        var motorista = await _db.Motoristas.FirstOrDefaultAsync(m => m.UsuarioId == usuarioId)
            ?? throw new RegraNegocioException("Este login não está ligado a um motorista.");

        var c = await _db.Carregamentos
            .Include(x => x.Veiculo)
            .Include(x => x.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Cliente)
            .Include(x => x.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Endereco)
            .Include(x => x.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Itens)
            .FirstOrDefaultAsync(x => x.MotoristaId == motorista.Id && x.Status == StatusCarregamento.EmRota)
            ?? throw new RegraNegocioException("Você não tem carregamento em rota.");

        return new EntregasDoMotorista(
            c.Id, c.Veiculo!.Placa, c.DataSaida, c.Status.ToString(),
            c.Pedidos.Select(cp => new EntregaPedido(
                cp.Id,
                cp.PedidoVenda!.Numero,
                cp.PedidoVenda.Cliente!.Nome,
                cp.PedidoVenda.Endereco!.ToString(),
                cp.PedidoVenda.TotalCaixas,
                cp.Resultado.ToString(),
                cp.Motivo)).ToList());
    }

    public async Task RegistrarAsync(int carregamentoPedidoId, bool entregue, string? motivo, int usuarioId)
    {
        var cp = await _db.CarregamentoPedidos
            .Include(x => x.PedidoVenda)
            .Include(x => x.Carregamento).ThenInclude(c => c!.Motorista)
            .Include(x => x.Carregamento).ThenInclude(c => c!.Pedidos)
            .Include(x => x.Carregamento).ThenInclude(c => c!.Caixas)
            .FirstOrDefaultAsync(x => x.Id == carregamentoPedidoId)
            ?? throw new RegraNegocioException("Entrega não encontrada.");

        var carregamento = cp.Carregamento!;
        if (carregamento.Motorista!.UsuarioId != usuarioId)
            throw new RegraNegocioException("Esta entrega é de outro motorista.");
        if (carregamento.Status != StatusCarregamento.EmRota)
            throw new RegraNegocioException("Este carregamento não está em rota.");
        if (cp.Resultado != ResultadoEntrega.Pendente)
            throw new RegraNegocioException("A entrega deste pedido já foi registrada.");

        // RN13: não entregue precisa de motivo
        if (!entregue && string.IsNullOrWhiteSpace(motivo))
            throw new RegraNegocioException("RN13: informe o motivo da não entrega.");

        var agora = DateTime.Now;
        cp.DataRegistro = agora;
        cp.Resultado = entregue ? ResultadoEntrega.Entregue : ResultadoEntrega.NaoEntregue;
        cp.Motivo = entregue ? null : motivo!.Trim();
        cp.PedidoVenda!.Status = entregue ? StatusPedido.Entregue : StatusPedido.NaoEntregue;

        // caixas entregues saem de "em rota"; as não entregues voltam e são bipadas no retorno (RF15)
        if (entregue)
        {
            foreach (var caixa in carregamento.Caixas.Where(x => x.PedidoVendaId == cp.PedidoVendaId))
            {
                caixa.Status = StatusCaixa.Entregue;
                _db.MovimentacoesCaixa.Add(new MovimentacaoCaixa
                {
                    CaixaId = caixa.Id, Tipo = TipoMovimentacao.Entrega, DataHora = agora,
                    UsuarioId = usuarioId, CarregamentoId = carregamento.Id
                });
            }
        }

        RegrasCarregamento.TentarEncerrar(carregamento);   // RF16
        await _db.SaveChangesAsync();
    }
}
