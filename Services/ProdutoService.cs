using ScraperApi.GrpcClient;

namespace ScraperApi.Services;

public class ProdutoService : IProdutoService
{
    private readonly IPythonScraperClient _pythonScraperClient;

    public ProdutoService(IPythonScraperClient pythonScraperClient)
    {
        _pythonScraperClient = pythonScraperClient;
    }
    
    public async Task<object> ObterCatalogoComScrapingAsync()
    {
        var produtoRaspados = await _pythonScraperClient.BuscarProdutosDoScraperAsync();
        
        //Regra de Negocio / Formatacao pro modelo da API
        return new
        {
            mensagem = "Dados Obtidados com sucesso via microsservico de scrapping em python",
            quantidade = produtoRaspados.Count(),
            itens = produtoRaspados
        };

    }
}