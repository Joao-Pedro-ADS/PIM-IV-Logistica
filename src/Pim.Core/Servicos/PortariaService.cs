using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

// RF05, RF12, RF14: o porteiro registra chegada de fornecedor, saída e volta da frota
public class PortariaService
{
    private readonly PimDbContext _db;

    public PortariaService(PimDbContext db) => _db = db;

    private IQueryable<Carregamento> ComTudo() => _db.Carregamentos
        .Include(c => c.Veiculo)
        .Include(c => c.Motorista)
        .Include(c => c.Caixas)
        .Include(c => c.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Itens).ThenInclude(i => i.Produto);

    // RF05: veículo de fornecedor não tem cadastro, então guarda só a placa
    public async Task<RegistroPortaria> ChegadaFornecedorAsync(string placa, int porteiroId)
    {
        placa = (placa ?? "").Trim().ToUpper().Replace("-", "");
        if (placa.Length != 7)
            throw new RegraNegocioException("A placa tem 7 caracteres, sem traço.");

        var registro = new RegistroPortaria
        {
            Tipo = TipoRegistroPortaria.ChegadaFornecedor,
            DataHora = DateTime.Now,
            PlacaFornecedor = placa,
            PorteiroId = porteiroId
        };
        _db.RegistrosPortaria.Add(registro);
        await _db.SaveChangesAsync();
        return registro;
    }

    // RF12: saída do veículo pelo QR Code
    public async Task<ResumoCarregamento> SaidaAsync(string qr, int porteiroId)
    {
        int veiculoId = RegrasCarregamento.LerQrVeiculo(qr);

        var carregamento = await ComTudo()
            .FirstOrDefaultAsync(c => c.VeiculoId == veiculoId && c.Status == StatusCarregamento.Planejado)
            ?? throw new RegraNegocioException("Este veículo não tem carregamento para sair.");

        // RN11: só sai com todas as caixas bipadas
        var resumo = RegrasCarregamento.Resumir(carregamento);
        if (resumo.CaixasBipadas < resumo.TotalCaixas)
            throw new RegraNegocioException(
                $"RN11: faltam {resumo.TotalCaixas - resumo.CaixasBipadas} caixas para bipar. O veículo não pode sair.");

        var agora = DateTime.Now;
        carregamento.Status = StatusCarregamento.EmRota;
        carregamento.DataSaida = agora;
        carregamento.Veiculo!.Situacao = SituacaoVeiculo.EmRota;      // RN16

        foreach (var cp in carregamento.Pedidos)
            cp.PedidoVenda!.Status = StatusPedido.EmRota;

        foreach (var caixa in carregamento.Caixas)
        {
            caixa.Status = StatusCaixa.EmRota;
            _db.MovimentacoesCaixa.Add(new MovimentacaoCaixa
            {
                CaixaId = caixa.Id, Tipo = TipoMovimentacao.Saida, DataHora = agora,
                UsuarioId = porteiroId, CarregamentoId = carregamento.Id
            });
        }

        // RN12: veículo, motorista (pelo carregamento) e horário
        _db.RegistrosPortaria.Add(new RegistroPortaria
        {
            Tipo = TipoRegistroPortaria.SaidaFrota, DataHora = agora,
            VeiculoId = veiculoId, CarregamentoId = carregamento.Id, PorteiroId = porteiroId
        });

        await _db.SaveChangesAsync();
        return RegrasCarregamento.Resumir(carregamento);
    }

    // RF14: volta do veículo pelo QR Code
    public async Task<ResumoCarregamento> VoltaAsync(string qr, int porteiroId)
    {
        int veiculoId = RegrasCarregamento.LerQrVeiculo(qr);

        var carregamento = await ComTudo()
            .FirstOrDefaultAsync(c => c.VeiculoId == veiculoId && c.Status == StatusCarregamento.EmRota && c.DataVolta == null)
            ?? throw new RegraNegocioException("Este veículo não está em rota.");

        var agora = DateTime.Now;
        carregamento.DataVolta = agora;
        carregamento.Veiculo!.Situacao = SituacaoVeiculo.Disponivel;   // RN16

        // RN12
        _db.RegistrosPortaria.Add(new RegistroPortaria
        {
            Tipo = TipoRegistroPortaria.VoltaFrota, DataHora = agora,
            VeiculoId = veiculoId, CarregamentoId = carregamento.Id, PorteiroId = porteiroId
        });

        RegrasCarregamento.TentarEncerrar(carregamento);   // RF16
        await _db.SaveChangesAsync();
        return RegrasCarregamento.Resumir(carregamento);
    }
}
