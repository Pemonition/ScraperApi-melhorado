# Imagem unica com os dois servicos (API .NET + scraper Python via gRPC).
# Os planos gratuitos rodam 1 container por servico e nao tem rede privada entre servicos,
# entao o scraper roda no mesmo container, ouvindo apenas em localhost:50051.

# ---------- Etapa 1: build da API .NET ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ScraperApi.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish ScraperApi.csproj -c Release -o /app/api --no-restore

# ---------- Etapa 2: runtime (ASP.NET + Python) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-venv \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY scraper/requirements.txt scraper/requirements.txt
RUN python3 -m venv /opt/venv \
    && /opt/venv/bin/pip install --no-cache-dir -r scraper/requirements.txt

COPY Protos/ Protos/
COPY scraper/ scraper/
# Gera os stubs gRPC do Python no build (o mesmo .proto usado pela API .NET)
RUN /opt/venv/bin/python -c "import sys; sys.path.insert(0, 'scraper'); import server"

COPY --from=build /app/api api/
COPY start.sh .
RUN chmod +x start.sh

ENV ASPNETCORE_ENVIRONMENT=Production \
    PythonGrpcSettings__Url=http://127.0.0.1:50051 \
    PORT=8080
EXPOSE 8080

CMD ["./start.sh"]
