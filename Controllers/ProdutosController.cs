using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using ScraperApi.Services;

namespace ScraperApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService  _produtoService;
    private readonly ILogger<ProdutosController> _logger;

    public ProdutosController(IProdutoService produtoService, ILogger<ProdutosController> logger)
    {
        _produtoService = produtoService;
        _logger = logger;
    }

    /// <summary>Retorna o catalogo de produtos raspado pelo microsservico Python.</summary>
    /// <param name="atualizar">true ignora o cache e raspa o site novamente.</param>
    [HttpGet]
    [ProducesResponseType<CatalogoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> Get([FromQuery] bool atualizar = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Buscando catalogo de produtos (atualizar={Atualizar})", atualizar);
            var resultado = await _produtoService.ObterCatalogoComScrapingAsync(atualizar, cancellationToken);
            return Ok(resultado);
        }
        catch (RpcException e) when (e.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
        {
            _logger.LogWarning(e, "Scraper Python demorou demais para responder");
            return Problem("O microsservico de scraping demorou demais para responder.", statusCode: 504);
        }
        catch (RpcException e)
        {
            _logger.LogError(e, "Falha na chamada gRPC ao scraper Python: {Status}", e.StatusCode);
            return Problem($"Microsservico de scraping indisponivel ({e.Status.Detail}).", statusCode: 503);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Erro ao processar catalogo de produtos");
            return Problem("Erro inesperado ao processar o catalogo de produtos.", statusCode: 500);
        }
    }
}
