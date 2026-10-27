using Microsoft.Extensions.Caching.Memory;
using ScraperApi.GrpcClient;

namespace ScraperApi.Services;

public class ProdutoService : IProdutoService
{
    private const string CacheKey = "catalogo-produtos";

    private readonly IPythonScraperClient _pythonScraperClient;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _duracaoCache;

    public ProdutoService(IPythonScraperClient pythonScraperClient, IMemoryCache cache, IConfiguration configuration)
    {
        _pythonScraperClient = pythonScraperClient;
        _cache = cache;
        _duracaoCache = TimeSpan.FromMinutes(configuration.GetValue("Cache:Minutos", 5));
    }

    public async Task<CatalogoResponse> ObterCatalogoComScrapingAsync(bool forcarAtualizacao = false, CancellationToken cancellationToken = default)
    {
        // Cache evita raspar o site a cada chamada (mais rapido e mais educado com o site de origem)
        if (!forcarAtualizacao && _cache.TryGetValue(CacheKey, out CatalogoResponse? emCache) && emCache is not null)
        {
            return emCache with { DoCache = true };
        }

        var produtosRaspados = await _pythonScraperClient.BuscarProdutosDoScraperAsync(cancellationToken);

        //Regra de Negocio / Formatacao pro modelo da API
        var catalogo = new CatalogoResponse(
            Mensagem: "Dados obtidos com sucesso via microsservico de scraping em Python",
            Quantidade: produtosRaspados.Count,
            ObtidoEm: DateTime.UtcNow,
            DoCache: false,
            Itens: produtosRaspados);

        _cache.Set(CacheKey, catalogo, _duracaoCache);
        return catalogo;
    }
}
