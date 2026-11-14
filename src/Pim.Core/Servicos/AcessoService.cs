using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

public class AcessoService
{
    private readonly PimDbContext _db;

    public AcessoService(PimDbContext db) => _db = db;

    // RF01: devolve o usuário se login e senha conferem (ou null)
    public async Task<Usuario?> AutenticarAsync(string login, string senha)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Login == login && u.Ativo);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash))
            return null;

        return usuario;
    }
}
