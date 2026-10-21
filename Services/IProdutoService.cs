namespace ScraperApi.Services;

public interface IProdutoService
{
    Task<object> ObterCatalogoComScrapingAsync();
}