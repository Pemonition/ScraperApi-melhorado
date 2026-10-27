# ScraperApi (melhorado)

API REST em **ASP.NET Core (.NET 10)** que devolve um catálogo de produtos obtido por um
**microsserviço de scraping em Python**, comunicando-se via **gRPC**.

Baseado no projeto original [rannyer/ScraperApi](https://github.com/rannyer/ScraperApi).

```
Cliente ──HTTP/JSON──▶ API .NET (/api/produtos) ──gRPC──▶ Scraper Python ──HTTP──▶ books.toscrape.com
                         │  cache em memória
                         └─ /swagger  /health
```

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/produtos` | Catálogo raspado (usa cache de alguns minutos) |
| GET | `/api/produtos?atualizar=true` | Ignora o cache e raspa de novo |
| GET | `/health` | Health check (usado pela plataforma de nuvem) |
| GET | `/swagger` | Documentação interativa |

Exemplo de resposta:

```json
{
  "mensagem": "Dados obtidos com sucesso via microsservico de scraping em Python",
  "quantidade": 20,
  "obtidoEm": "2026-10-27T22:12:05Z",
  "doCache": false,
  "itens": [{ "titulo": "A Light in the Attic", "preco": "£51.77" }]
}
```

## O que foi melhorado em relação ao original

- **Microsserviço Python incluído** (`scraper/server.py`): o original dependia de um serviço
  gRPC que não estava no repositório. Agora o projeto é completo e usa o mesmo `produto.proto`.
- **Pronto para nuvem**: `Dockerfile`, `render.yaml` (deploy em 1 clique), porta lida da variável `PORT`,
  HTTPS redirection só em desenvolvimento (na nuvem o proxy faz o TLS).
- **gRPC client factory** (`AddGrpcClient`): o canal HTTP/2 é reaproveitado em vez de ser criado a cada requisição.
- **Deadline no gRPC** e erros mapeados corretamente: `503` se o scraper estiver fora, `504` se demorar demais
  (antes tudo virava `500 "Falha..."`).
- **Cache em memória** (configurável em `Cache:Minutos`) — respostas instantâneas e menos carga no site raspado.
- **Resposta tipada** (`CatalogoResponse`) em vez de `object` anônimo — aparece certinha no Swagger.
- **Swagger** habilitado (o pacote estava instalado, mas não era usado), **health check** e **CORS**.
- Pasta `Constrollers` renomeada para `Controllers`; `bin/`, `obj/` e `.idea/` removidos do Git (`.gitignore`).
- `ScraperApi.http` atualizado (apontava para `/weatherforecast`).
- **CI no GitHub Actions**: compila, gera a imagem Docker e faz um teste de fumaça a cada push.

## Rodando localmente

### Sem Docker

```bash
# 1) scraper Python
python -m venv .venv
.venv/Scripts/activate        # Linux/Mac: source .venv/bin/activate
pip install -r scraper/requirements.txt
python scraper/server.py      # ouve em localhost:50051

# 2) em outro terminal, a API
dotnet run --launch-profile http   # http://localhost:5100/swagger
```

### Com Docker

```bash
docker build -t scraperapi-melhorado .
docker run -p 8080:8080 scraperapi-melhorado   # http://localhost:8080/swagger
```

## Deploy gratuito no Render

1. Crie uma conta em <https://render.com> entrando com o GitHub.
2. **New → Blueprint** e escolha este repositório (o Render lê o `render.yaml`).
3. Clique em **Apply**. O Render constrói o `Dockerfile` e publica em `https://<nome>.onrender.com`.
4. A cada `git push` na `main` o Render faz o deploy de novo automaticamente.

> No plano gratuito o serviço "dorme" após ~15 min sem acessos; a primeira requisição depois disso
> leva cerca de 1 minuto para acordar.

### Por que Render (e não Vercel)?

| Plataforma | Roda .NET? | Docker? | Grátis sem cartão? | Observação |
|---|---|---|---|---|
| **Render** | ✅ | ✅ | ✅ | Deploy pelo GitHub, HTTPS, auto-deploy. Dorme quando ocioso. |
| Koyeb | ✅ | ✅ | ✅ | Boa alternativa; 1 serviço gratuito. |
| Azure App Service F1 | ✅ | ❌ (F1) | ✅ | Só 60 min de CPU/dia; precisaria hospedar o Python à parte. |
| Google Cloud Run | ✅ | ✅ | ❌ exige cartão | Cota grátis generosa. |
| Railway / Fly.io | ✅ | ✅ | ❌ | Hoje só oferecem créditos de teste. |
| Vercel / Netlify | ❌ | ❌ | ✅ | Só funções serverless (Node/Python/Go); não roda ASP.NET nem servidor gRPC. |

## Configuração (variáveis de ambiente)

| Variável | Padrão | Uso |
|---|---|---|
| `PORT` | — | Porta HTTP (definida pela plataforma) |
| `PythonGrpcSettings__Url` | `http://localhost:50051` | Endereço do scraper |
| `PythonGrpcSettings__TimeoutSegundos` | `20` | Deadline da chamada gRPC |
| `Cache__Minutos` | `5` | Tempo de cache do catálogo |
| `SCRAPER_PAGINAS` | `1` | Quantas páginas o scraper percorre (20 livros por página) |
| `SCRAPER_URL` | página 1 do books.toscrape.com | Página inicial do scraping |
