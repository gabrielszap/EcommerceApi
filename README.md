# EcommerceApi

API do teste pratico de gerenciamento de pedidos. Este repositorio inclui a base executavel, a autenticacao, a criacao de pedidos, as consultas de pedidos, o cancelamento de pedidos e a documentacao Swagger/OpenAPI. 

## Pre-requisitos

- .NET SDK 10.0.203 ou um SDK .NET 10 compativel
- Docker Engine com Docker Compose v2 para execucao em container

Configure `Jwt__SigningKey` com um valor apenas de desenvolvimento com pelo menos 32 bytes UTF-8. Essa chave e obrigatoria na inicializacao, nunca deve ser usada como segredo real e deve ser fornecida por configuracao especifica do ambiente ou por um secret store em implantacoes reais.

## Arquitetura

A solucao usa Clean Architecture com quatro projetos de producao:

- `EcommerceApi.Domain`: agregado `Order`, `OrderItem`, status, invariantes, transicao de cancelamento e calculo de `TotalAmount`.
- `EcommerceApi.Application`: registro do MediatR, pipeline behavior de FluentValidation, login com credenciais fixas, comando/handler de criacao de pedido, comando/handler de cancelamento, query handlers e portas de persistencia de pedidos pertencentes a camada de aplicacao.
- `EcommerceApi.Infrastructure`: `OrderDbContext` do EF Core, mapeamentos SQLite, migrations, geracao de JWT, writer EF de pedidos e queries EF de leitura de pedidos.
- `EcommerceApi.Api`: host Minimal API, registro JWT bearer, Problem Details, OpenAPI/Swagger UI, injecao de dependencia, migracao na inicializacao, endpoint de autenticacao e endpoints protegidos de pedidos.

Minimal APIs foram escolhidas porque o contrato final tem cinco rotas focadas. Route groups e delegates finos mantem o transporte conciso, enquanto o MediatR executa os casos de uso e o Domain protege as invariantes de negocio. Controllers adicionariam cerimonia sem uma necessidade atual de filtros ou tratamentos da requisição customizados.

A direcao de dependencias e `Domain <- Application <- Api` e `Domain <- Application <- Infrastructure <- Api`. Domain e Application nao referenciam ASP.NET Core, EF Core, SQLite ou pacotes de implementacao JWT. Nao existe repositorio generico: os casos de uso de pedidos chamam `IOrderWriter` e `IOrderReader`.

## Execucao local

A partir da raiz do repositorio:

```powershell
$env:Jwt__SigningKey = 'local-development-key-with-at-least-32-bytes'
dotnet restore
dotnet run --project src/EcommerceApi.Api/EcommerceApi.Api.csproj
```

A API escuta na URL configurada pelo ASP.NET Core. Em Development, o Swagger UI fica disponivel em `/swagger` e o documento OpenAPI gerado fica disponivel em `/openapi/v1.json`. Os endpoints expostos atualmente sao `POST /auth/login`, `POST /api/orders` protegido, `GET /api/orders` protegido, `GET /api/orders/{id}` protegido e `PATCH /api/orders/{id}/cancel` protegido.

O padrao local e `ConnectionStrings:Orders=Data Source=data/ecommerce.db` e, em Development, `data/ecommerce.development.db`. O caminho relativo do SQLite e resolvido a partir do content root do processo. O diretorio `data` intencionalmente nao e commitado.

## Docker Compose

Configure uma chave temporaria apenas de desenvolvimento e inicie a API:

```powershell
$env:JWT_SIGNING_KEY = 'local-development-key-with-at-least-32-bytes'
docker compose build
docker compose up
```

O Compose inicia apenas a API. Ele armazena o SQLite em `/app/data/ecommerce.db` no volume nomeado `ecommerceapi-data`, entao recriar o container preserva o banco de dados. As portas do host sao `8080` para HTTP e `8081` para HTTPS.

O Docker Compose roda com `ASPNETCORE_ENVIRONMENT=Development` para facilitar a avaliacao. Ele expoe HTTP em `http://localhost:8080` e HTTPS em `https://localhost:8081`. Um certificado HTTPS de desenvolvimento e gerado durante o build da imagem Docker e usado apenas pelo container. O Swagger UI fica disponivel em `/swagger`, e acessar a URL base redireciona para `/swagger`.

Se o navegador alertar sobre o certificado HTTPS, use a URL HTTP ou confie um certificado de desenvolvimento local pelo processo normal da sua estacao. Nao reutilize o certificado gerado no container em producao.

Para resetar intencionalmente o banco Docker, pare a stack e remova o volume nomeado:

```
docker compose down -v
```

Isso apaga permanentemente o banco de dados do volume nomeado. Para um reset local, pare a API e remova intencionalmente o arquivo `data/ecommerce*.db` selecionado.

## Migrations e inicializacao

A migration commitada e `20260902145400_InitialOrderSchema`. Ela cria `Orders` e `OrderItems`, o relacionamento obrigatorio, indices, check constraints e nenhum campo `TotalAmount`. `Database.MigrateAsync()` roda antes de `app.Run()`; uma falha de migration e registrada como Critical e relancada para que o processo nao passe a servir requisicoes. Inicializacoes repetidas encontram o historico de migrations existente e mantem schema/dados intactos. `EnsureCreated` nao e usado.

A fonte da migration e o model snapshot estao versionados em `src/EcommerceApi.Infrastructure/Persistence/Migrations`.

O host da API valida issuer, audience, lifetime, assinatura e uma chave de assinatura JWT com pelo menos 32 bytes. A chave de assinatura e fornecida apenas por configuracao/ambiente e nao deve ser usada como segredo real nem registrada em logs.

## Swagger/OpenAPI

A API mantem a geracao de documento por componentes first-party com `Microsoft.AspNetCore.OpenApi` e usa `Swashbuckle.AspNetCore.SwaggerUI` apenas para servir a UI interativa. O titulo do documento OpenAPI e `EcommerceApi`, a versao e `v1` e as tags sao `Authentication` e `Orders`.

Em Development:

- Swagger UI: `/swagger`
- OpenAPI JSON: `/openapi/v1.json`
- Redirecionamento da URL base: `/` redireciona para `/swagger`

O Swagger UI define um security scheme HTTP JWT reutilizavel chamado `Bearer`. Use `POST /auth/login` com as credenciais fixas do avaliador, copie o `accessToken` retornado, clique em `Authorize` e informe:

```http
Bearer <accessToken>
```

A UI nao preenche nem persiste valores de autorizacao automaticamente. As credenciais fixas aparecem apenas como exemplos de teste exigidos pelo teste pratico. Senhas enviadas, JWTs e signing keys nao devem ser registrados em log nem commitados.

## Autenticacao

`POST /auth/login` e anonimo e encaminha a requisicao para a camada Application por meio do MediatR. O FluentValidation roda no pipeline do MediatR, entao o endpoint nao compara credenciais nem cria token.

| Resultado | Status | Corpo |
| --- | --- | --- |
| Credenciais fixas validas | `200 OK` | `{ "accessToken": "...", "expiresAtUtc": "..." }` |
| Email/senha ausentes ou em formato invalido | `400 Bad Request` | Validation Problem Details com erros por campo |
| Email ou senha incorretos | `401 Unauthorized` | Problem Details, sem token |

Apenas para o avaliador, as credenciais fixas em memoria sao `dev@martech.com` / `Senha@123`. Elas deliberadamente nao sao persistidas no SQLite nem modeladas como uma conta de usuario. Isso e uma feature de teste e nao um desenho de gerenciamento de identidade para producao.

Use o token recebido nas rotas protegidas:

```http
Authorization: Bearer <accessToken>
```

O documento OpenAPI descreve request/response de login, erros de validacao e autenticacao, e o security scheme JWT `Bearer`.

## Criacao de pedido

`POST /api/orders` exige `Authorization: Bearer <accessToken>`. O endpoint encaminha a requisicao para o MediatR, o FluentValidation valida o formato no pipeline, o Domain constroi o agregado `Order` e seus itens, e a Infrastructure persiste o agregado com EF Core/SQLite em uma unica chamada `SaveChangesAsync()`.

Request:

```json
{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "items": [
    {
      "productName": "Keyboard",
      "quantity": 2,
      "unitPrice": 150.00
    }
  ]
}
```

Resposta de sucesso: `201 Created`, `Location: /api/orders/{id}` e corpo contendo `id`, `customerId`, `status`, `createdAt`, `items` e `totalAmount` calculado pelo Domain.

Falhas de validacao e de regra de dominio retornam `400 Bad Request` Problem Details. Tokens bearer ausentes, malformados, invalidos ou expirados retornam `401 Unauthorized`. Lookup de catalogo de produto, validacao de estoque, lookup de preco, pagamento, confirmacao, persistencia de cliente, descontos e chaves de idempotencia nao estao implementados.

## Consulta de pedidos

Os dois endpoints de leitura exigem `Authorization: Bearer <accessToken>`. Eles sao implementados como queries MediatR e nao expoem entidades EF Core nem queryables fora da Infrastructure.

`GET /api/orders?page=1&pageSize=10` retorna `200 OK` com este envelope de paginacao:

```json
{
  "items": [
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "customerId": "11111111-1111-1111-1111-111111111111",
      "status": "Pending",
      "createdAt": "2026-09-02T12:00:00Z",
      "itemCount": 2,
      "totalAmount": 300.00
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

`page` tem padrao `1`, `pageSize` tem padrao `10` e ambos devem ser inteiros positivos. `pageSize` e limitado a `100`. Paginacao invalida retorna `400 Bad Request` Validation Problem Details. Os resultados sao ordenados por `createdAt` mais recente primeiro, com `id` como criterio estavel de desempate, e a paginacao e aplicada pelo EF Core no nivel da consulta ao banco.

`GET /api/orders/{id}` retorna `200 OK` com `id`, `customerId`, `status`, `createdAt`, `items` e `totalAmount` calculado pelo Domain. Um GUID malformado ou vazio retorna `400 Bad Request` Problem Details. Um GUID bem formado mas desconhecido retorna `404 Not Found` Problem Details.

## Cancelamento de pedido

`PATCH /api/orders/{id}/cancel` exige `Authorization: Bearer <accessToken>` e nao tem corpo de request. O endpoint interpreta o identificador da rota e envia um comando MediatR. O handler carrega o agregado rastreado pela porta de escrita pertencente a Application, chama `Order.Cancel()` e persiste o status alterado com EF Core no mesmo contexto. `Status` e configurado como concurrency token no EF Core, entao um save de cancelamento obsoleto e traduzido para o mesmo resultado `409 Conflict` de estado invalido em vez de reportar um segundo sucesso.

Resposta de sucesso: `200 OK` com a representacao do pedido, incluindo `status: "Cancelled"` e `totalAmount` calculado pelo Domain.

| Resultado | Status | Corpo |
| --- | --- | --- |
| Pedido pendente existente | `200 OK` | Representacao do pedido cancelado |
| GUID malformado | `400 Bad Request` | Problem Details |
| GUID valido desconhecido | `404 Not Found` | Problem Details |
| Pedido ja cancelado ou confirmado | `409 Conflict` | Problem Details |
| Token bearer ausente, malformado, invalido ou expirado | `401 Unauthorized` | Problem Details |

Nenhum endpoint confirma pedidos atualmente. O Domain expoe uma transicao de confirmacao apenas para que o agregado consiga representar e proteger o estado `Confirmed` exigido pelo ciclo de vida do pedido; o cancelamento ainda permite somente `Pending -> Cancelled`.

### Configuracao JWT

| Chave | Finalidade |
| --- | --- |
| `Jwt__Issuer` | Issuer que esta API cria e aceita. |
| `Jwt__Audience` | Audience que esta API cria e aceita. |
| `Jwt__LifetimeMinutes` | Lifetime positivo, em minutos, dos access tokens. |
| `Jwt__SigningKey` | Chave simetrica secreta com pelo menos 32 bytes UTF-8; forneca fora do controle de versao em ambientes reais. |

`appsettings.json` contem defaults seguros para issuer, audience e lifetime, alem de uma signing key local apenas para desenvolvimento coerente com os exemplos deste README. Substitua `Jwt__SigningKey` por configuracao de ambiente ou secret store fora do controle de versao em qualquer ambiente real. Variaveis de ambiente usam dois underscores para configuracao aninhada.

## Testes e quality checks

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
docker compose config
docker compose build
```

Os testes usam xUnit e um banco SQLite temporario real para comportamento de migration e persistencia; eles nao usam EF Core InMemory. Domain, pipelines MediatR de validacao/logging, handler de login, handler de criacao de pedido, handler de cancelamento, query handlers, assinatura/validacao JWT, migration, migration de startup, persistencia/leitura/cancelamento de pedidos via EF, comportamento da API de pedidos e metadados do contrato OpenAPI estao cobertos em `tests/EcommerceApi.Tests`.

## Observabilidade opcional

Um pipeline behavior de logging MediatR com Serilog no host da API. O behavior registra tipo da request, resultado e duracao para comandos e queries. Ele nao registra payloads de request, senhas enviadas, valores JWT, signing keys ou corpos completos de comandos.

O Serilog e configurado durante a inicializacao do host e escreve logs estruturados no console. A camada Application ainda depende apenas de `Microsoft.Extensions.Logging.Abstractions`; Serilog permanece como provider de logging do host da API, nao como dependencia de negocio.

O repositorio ja inclui testes de integracao de API com `WebApplicationFactory`.

## Limitacoes e premissas

- Endpoints de criacao, leitura e cancelamento de pedidos estao implementados. Comportamentos de confirmacao, pagamento, catalogo, estoque, cliente, desconto e idempotencia estao fora do escopo atual.
- Nao existe usuario persistido, registro, reset de senha, refresh token ou fluxo OAuth.
- A observabilidade opcional se limita ao logging de requests MediatR com Serilog.
- A exposicao Swagger/OpenAPI e apenas em Development por padrao, com `OpenApi__Enabled=true` como opt-in explicito para outros ambientes. O Docker Compose roda a API intencionalmente em Development para que o avaliador consiga abrir o Swagger imediatamente.
- A migration de startup e intencionalmente simples para este teste pratico de processo unico e nao e um coordenador de migrations multi-instancia.
- O SQLite armazena valores `decimal` usando a representacao do provider; o calculo monetario de total permanece exclusivamente no comportamento de Domain.
