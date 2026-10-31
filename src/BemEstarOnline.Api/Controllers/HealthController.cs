using BemEstarOnline.Api.Dominio.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Dados;
using Microsoft.AspNetCore.Mvc;

namespace BemEstarOnline.Api.Controllers;

/// <summary>Verificação de saúde: usada pelo Docker, pelo monitoramento e pelo site para saber se a API está no ar.</summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly BemEstarDbContext _db;
    private readonly IAutomacoesBanco _automacoes;

    public HealthController(BemEstarDbContext db, IAutomacoesBanco automacoes)
    {
        _db = db;
        _automacoes = automacoes;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var bancoOk = await _db.Database.CanConnectAsync();
        var resposta = new
        {
            status = bancoOk ? "online" : "banco indisponível",
            servico = "Bem Estar Online API",
            versao = "2.0.0",
            banco = _automacoes.ExecutadasPeloBanco ? "MySQL" : "Memória (demonstração)",
            horario = DateTime.Now
        };
        return bancoOk ? Ok(resposta) : StatusCode(StatusCodes.Status503ServiceUnavailable, resposta);
    }
}
