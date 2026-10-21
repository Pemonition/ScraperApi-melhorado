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

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            _logger.LogInformation("Getting all produtos");
            var resultado = await _produtoService.ObterCatalogoComScrapingAsync();
            return Ok(resultado);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Erro ao processar catalogo de produtos");
            return StatusCode(500, new { erro = "Falha..." });
        }
    }
}