using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;
using QRCoder;

namespace Pim.Web.Controllers;

[Authorize(Roles = "Gerente,Frota,Programador")]
public class VeiculosController : Controller
{
    private readonly PimDbContext _db;

    public VeiculosController(PimDbContext db) => _db = db;

    // RF18: situação de cada veículo
    public async Task<IActionResult> Index()
    {
        var veiculos = await _db.Veiculos.OrderBy(v => v.Placa).ToListAsync();
        return View(veiculos);
    }

    // RF02: cadastrar
    [Authorize(Roles = "Gerente,Frota")]
    public IActionResult Criar() => View(new Veiculo());

    [HttpPost]
    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> Criar(Veiculo veiculo)
    {
        veiculo.Placa = (veiculo.Placa ?? "").Trim().ToUpper();

        if (await _db.Veiculos.AnyAsync(v => v.Placa == veiculo.Placa))
            ModelState.AddModelError(nameof(Veiculo.Placa), "Já existe um veículo com essa placa.");

        if (!ModelState.IsValid)
            return View(veiculo);

        veiculo.Situacao = SituacaoVeiculo.Disponivel;
        _db.Veiculos.Add(veiculo);
        await _db.SaveChangesAsync();
        TempData["Mensagem"] = "Veículo cadastrado.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> Editar(int id)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();
        return View(veiculo);
    }

    [HttpPost]
    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> Editar(int id, Veiculo dados)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();

        dados.Placa = (dados.Placa ?? "").Trim().ToUpper();
        if (await _db.Veiculos.AnyAsync(v => v.Placa == dados.Placa && v.Id != id))
            ModelState.AddModelError(nameof(Veiculo.Placa), "Já existe um veículo com essa placa.");

        if (!ModelState.IsValid)
            return View(dados);

        // copia só os campos do formulário (a situação não muda aqui)
        veiculo.Placa = dados.Placa;
        veiculo.Modelo = dados.Modelo;
        veiculo.LimiteCaixas = dados.LimiteCaixas;
        await _db.SaveChangesAsync();
        TempData["Mensagem"] = "Veículo atualizado.";
        return RedirectToAction(nameof(Index));
    }

    // RF17: colocar/tirar da manutenção
    [HttpPost]
    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> AlternarManutencao(int id)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();

        if (veiculo.Situacao == SituacaoVeiculo.EmRota)
        {
            TempData["Erro"] = "Veículo em rota não pode ir para manutenção.";
            return RedirectToAction(nameof(Index));
        }

        veiculo.Situacao = veiculo.Situacao == SituacaoVeiculo.EmManutencao
            ? SituacaoVeiculo.Disponivel
            : SituacaoVeiculo.EmManutencao;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // RF02: página da etiqueta para imprimir
    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> Etiqueta(int id)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();
        return View(veiculo);
    }

    // RF02: imagem do QR Code (o app lê "VEICULO:<id>")
    [Authorize(Roles = "Gerente,Frota")]
    public IActionResult QrCode(int id)
    {
        using var gerador = new QRCodeGenerator();
        using var dados = gerador.CreateQrCode($"VEICULO:{id}", QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(dados).GetGraphic(10);
        return File(png, "image/png");
    }
}
