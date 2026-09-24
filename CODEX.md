# CODEX — BarberApp API

> Referência viva do projeto. Todo novo código, decisão arquitetural ou padrão adotado deve atualizar este arquivo na mesma mudança.

## Propósito: aprendizado ativo

O objetivo principal é **aprendizado ativo**. O código não deve apenas funcionar: cada linha precisa ser lida, entendida e questionada antes de ser aceita. Mudanças devem ser pequenas e revisáveis, com nomes claros, decisões explicadas e testes que demonstrem o comportamento.

O mantenedor quer ver, revisar e compreender o código. Não esconda decisões em automações opacas nem entregue grandes blocos gerados sem explicação. Ao propor uma solução, apresente o problema, as alternativas relevantes, o motivo da escolha e seus efeitos. Declare incertezas, hipóteses e dívidas técnicas.

## Fluxo humano de aprendizado e participação

O mantenedor é o autor-aprendiz e deve participar de **toda etapa que envolva código**, inclusive testes. Por padrão, agentes não escrevem nem corrigem código de implementação ou de teste no lugar dele. Antes de cada alteração, o agente deve explicar o conceito, o comportamento esperado, os arquivos envolvidos, os riscos e um passo pequeno para o mantenedor implementar com as próprias mãos.

Depois que o mantenedor escrever o código, os agentes podem inspecionar o diff, executar build/testes/lint e ensinar a interpretar os resultados. Se houver falha, devem explicar a causa, apontar a região relevante e orientar a próxima modificação; não devem aplicar a correção automaticamente. Um agente só pode editar código quando o mantenedor der autorização explícita e pontual para aquela edição. Autorizações anteriores não valem para tarefas futuras.

O ciclo padrão é:

1. Definir junto com o mantenedor o comportamento e os critérios de aceitação.
2. Ensinar os conceitos e mostrar o menor próximo passo, sem entregar uma implementação opaca.
3. O mantenedor escreve ou modifica os testes e o código.
4. Os agentes revisam o diff e executam as verificações.
5. Em caso de erro, os agentes ensinam a diagnosticar e pedem que o mantenedor faça a correção.
6. Repetir em passos pequenos até os testes passarem.
7. Apresentar o diff completo e os riscos residuais para revisão explícita do mantenedor.

Mesmo quando múltiplos agentes forem solicitados, eles atuam como professores, revisores e operadores das verificações. A coordenação multiagente não remove nenhum ponto de participação ou aprovação humana.

## Gates permanentes: qualidade, segurança e custo

### Qualidade

- Nenhuma mudança é considerada pronta apenas porque compila.
- Os testes relevantes devem passar, com quantidade de testes descobertos, aprovados, falhos e ignorados informada.
- Antes de conclusão, o agente deve apresentar o diff para o mantenedor e resumir riscos, limitações, hipóteses e itens não testados.
- A tarefa só pode ser considerada concluída depois que o mantenedor revisar e aprovar explicitamente o diff.

### Segurança

- Antes de apresentar o trabalho, inspecione o diff e os logs em busca de senhas, tokens, API keys, connection strings com credenciais e outros segredos.
- Valores sensíveis devem vir de variáveis de ambiente, Secret Manager ou cofre do ambiente; nunca de arquivos versionados, exemplos reais ou valores padrão inseguros.
- Não imprima segredos em comandos, logs, mensagens de erro, documentação ou relatórios. Exemplos devem usar placeholders inequivocamente falsos.
- Se um segredo for encontrado no histórico ou no código, pare, informe sem reproduzir o valor e proponha remoção e rotação.

### Custo e uso de agentes

- Ao final de tarefa que usou loops, tentativas repetidas ou múltiplos agentes, informe uma estimativa compreensível do esforço: agentes envolvidos, rodadas de revisão, execuções relevantes e retrabalho.
- O relatório deve sugerir como reduzir custo na próxima tarefa, por exemplo limitar iterações, restringir arquivos/escopo, evitar agentes redundantes ou executar somente testes afetados durante o ciclo.
- Use o menor número de agentes e iterações que preserve aprendizado, segurança e qualidade. Não paralelize trabalho dependente nem repita análise já validada.

## Estado auditado

Conferido contra o repositório em **23/09/2026**:

- Cinco projetos `net10.0`, com `Nullable` e `ImplicitUsings` habilitados: os quatro projetos da aplicação e `BarberApp.IntegrationTests`.
- `dotnet build BarberApp.slnx --no-restore` passa com zero erros e zero avisos.
- `BarberApp.IntegrationTests` usa xUnit, `WebApplicationFactory` e EF Core InMemory. A suíte atual executa **21 casos** de integração sobre API key, compatibilidade com JWT, riscos do middleware e contrato OpenAPI.
- Não há `.editorconfig`, analyzers adicionais ou lint dedicado. O lint adotado abaixo é `dotnet format --verify-no-changes`, que acusa problemas de formatação já existentes.
- O frontend está parcialmente iniciado, fora deste repositório.
- Há dívidas de segurança: uma credencial PostgreSQL está versionada em `appsettings.json` e o bootstrap cria um administrador com senha fixa. Não reproduza isso; a correção deve ser isolada e incluir rotação dos segredos expostos.

## Arquitetura e stack

O BarberApp é uma API REST para clientes, barbeiros, serviços, agenda semanal, exceções de agenda, agendamentos, perfis e pagamentos simulados. Usa ASP.NET Core Web API, C#/.NET 10, controllers MVC, EF Core 10, PostgreSQL/Npgsql, ASP.NET Core Identity, API key global, JWT Bearer, FluentValidation e Swagger/OpenAPI. O gateway ativo é um mock.

A solução é inspirada em Clean Architecture. `Domain` concentra entidades, enums, transições de estado e contratos. `Application` orquestra casos de uso em services e define DTOs e validators. `Infrastructure` implementa persistência, Identity e o gateway. `API` é a borda HTTP e o composition root em `Program.cs`.

Não é Clean Architecture estrita: `Infrastructure` referencia `Application`; alguns services retornam entidades para mapeamento nos controllers; e controllers de autenticação, perfil e barbeiro usam Identity diretamente. Isso descreve o estado atual, não autoriza aumentar o acoplamento. Melhorias devem ser incrementais, testadas e registradas aqui.

```text
HTTP -> API -> Application -> Domain
         \-> Infrastructure -> PostgreSQL

Dependências atuais:
API -> Application + Infrastructure
Infrastructure -> Application -> Domain
```

O pipeline HTTP aplica CORS e, em seguida, `ApiKeyAuthenticationMiddleware` antes de autenticação e autorização JWT. Toda rota da API, inclusive login, registro e endpoints antes classificados como públicos, exige `X-API-Key`. Header ausente encerra a requisição com `401` e `WWW-Authenticate: ApiKey`; valor inválido encerra com `403`; valor válido permite que o fluxo anterior continue. A API key identifica o consumidor da aplicação e **não substitui** usuário, token JWT ou role. Em `Development`, Swagger/OpenAPI é registrado antes do middleware e permanece público; o preflight CORS também é atendido antes da barreira de API key.

## Estrutura e responsabilidades

```text
BarberApp.slnx
├── BarberApp.API/
│   ├── Controllers/       rotas, claims, roles e respostas HTTP
│   ├── Middleware/        autenticação global do consumidor por API key
│   ├── OpenApi/           requisitos ApiKey/Bearer por operação
│   └── Program.cs         composição, DI e pipeline HTTP
├── BarberApp.Application/
│   ├── DTOs/              contratos Request/Response
│   ├── Services/          orquestração dos casos de uso
│   └── Validators/        validação de entrada
├── BarberApp.Domain/
│   ├── Entities/          estado e comportamento do domínio
│   ├── Enums/             valores fechados
│   └── Interfaces/        contratos de repositório e pagamento
├── BarberApp.Infrastructure/
│   ├── Data/              AppDbContext e mapeamentos
│   ├── Identity/          ApplicationUser
│   ├── Migrations/        histórico versionado do schema
│   ├── Payment/           gateway mock
│   └── Repositories/      consultas e persistência EF Core
└── BarberApp.IntegrationTests/
    ├── Authentication/    contrato, riscos e OpenAPI da API key
    └── Infrastructure/    WebApplicationFactory e banco InMemory isolado
```

Os `Class1.cs` ainda presentes são resíduos de template, não um padrão. As migrations ficam em `Infrastructure/Migrations`.

Fluxo esperado: o controller recebe/autoriza e delega; o service coordena o caso de uso; entidades protegem suas transições; o repositório consulta/persiste; e o controller converte o resultado em resposta HTTP. Regras locais ficam na entidade (`Confirmar`, `Cancelar`, `Concluir`, `Atualizar`, `Desativar`); regras entre agregados ficam no service; LINQ, `Include`, filtros e ordenação ficam no repositório.

## Convenções obrigatórias

### Nomenclatura e organização

- Use português para o domínio e preserve os sufixos técnicos adotados: `Controller`, `Service`, `Repository`, `Request`, `Response`, `Validator` e `Async`.
- Tipos, records, métodos, propriedades e membros públicos usam `PascalCase`; parâmetros/locais, `camelCase`; campos privados, `_camelCase`.
- Interfaces começam com `I`. Operações assíncronas terminam em `Async`, retornam `Task`/`Task<T>` e nunca usam `.Result`, `.Wait()` ou `async void`.
- Nomes expressam intenção. Não perpetue erros existentes como `CridoEm`, `StatusPagemento`, `Reenbolsar`, `DisponibildadeResponse` e `BarberiroComLoginRespnse`; antes de renomear algo persistido ou público, avalie compatibilidade e migration.
- Prefira um tipo principal por arquivo e namespace file-scoped em arquivos novos. Remova `using` não utilizado.

### Domain

- Não referencia ASP.NET Core, EF Core, Identity, banco, `Application` ou `Infrastructure`.
- Entidades protegem invariantes: setters privados/protegidos e mudanças por métodos de negócio.
- O construtor vazio para EF Core deve ser `protected`, salvo justificativa documentada.
- Timestamps persistidos usam `DateTime.UtcNow`; não introduza `DateTime.Now`.
- Em código novo, prefira exceções específicas a `Exception` genérica.

### Application

- Services dependem de interfaces, nunca de `AppDbContext` ou repositórios concretos.
- Services não conhecem HTTP, headers, `IActionResult` ou status codes.
- Novas entradas/saídas públicas usam DTOs imutáveis (`record`) com `Request`/`Response`; não exponha entidades como novo contrato da API.
- Validação estrutural fica em `Validators`; regra de negócio fica na entidade ou service.
- I/O novo aceita e propaga `CancellationToken` quando a cadeia permitir.

### API e controllers

- Controllers recebem, autorizam, delegam, mapeiam e respondem; não abrigam regra de negócio ou consulta EF.
- Rotas seguem `api/[controller]` ou o padrão aninhado `api/barbeiros/{barbeiroId:guid}`; IDs usam `:guid`.
- Declare `[Authorize]`, `[Authorize(Roles = "...")]` ou `[AllowAnonymous]`. Além da role, valide propriedade do recurso.
- Nunca confie em ID, email ou role enviados no body para identidade/autorização; use claims e valide o vínculo.
- Use status e DTOs estáveis (`200`, `201` com `CreatedAtAction`, `204`, `400`, `401`, `403`, `404`, `409`).
- Não devolva stack trace, segredo, exceção interna ou entidade EF. Enquanto não houver middleware global, traduza somente falhas esperadas na borda HTTP.
- Todo consumidor deve enviar `X-API-Key`; endpoints protegidos continuam exigindo também `Authorization: Bearer {token}`.
- A API key deve vir de `ApiKey:Value` ou da variável de ambiente `ApiKey__Value`. Nunca use valor padrão, fallback inseguro ou chave real versionada.
- A ausência de configuração deve impedir a inicialização. Não transforme falha de configuração em bypass de autenticação.
- Preserve os contratos: API key ausente retorna `401` com `WWW-Authenticate: ApiKey`; inválida, vazia ou múltipla retorna `403`; válida apenas libera o fluxo preexistente.
- Swagger deve continuar público somente em `Development`. No OpenAPI, operações públicas exigem `ApiKey`; operações com `[Authorize]` exigem `ApiKey` e `Bearer` no mesmo requisito.

### Repositórios, EF Core e banco

- Interfaces ficam no `Domain`; implementações, em `Infrastructure/Repositories`.
- Repositórios concentram EF/LINQ e não expõem `IQueryable`; consultas são materializadas de forma assíncrona.
- Em leitura nova, considere `AsNoTracking` e documente quando tracking for necessário.
- O padrão atual chama `SaveChangesAsync` em cada escrita. Não misture outro Unit of Work sem decisão registrada.
- Mapeamentos, constraints, conversões e índices ficam em `AppDbContext.OnModelCreating`.
- Mudança de schema exige migration descritiva e revisão do código/SQL; versione migration e snapshot juntos.

### Datas e agenda

- Instantes persistidos são UTC; horários exibidos/agendados seguem Brasília na regra atual.
- Nunca compare horário local e UTC sem conversão e `DateTimeKind` explícitos.
- Exceção por data prevalece sobre agenda semanal.
- O atendimento deve caber integralmente no expediente e não sobrepor agendamento ativo.
- Alterações exigem testes de abertura, fechamento, duração, sobreposição, passado, virada de dia e exceção.

### Formatação

- `dotnet format` é o formatador oficial; código novo não aumenta violações existentes.
- Não deixe blocos grandes comentados, `todo` sem contexto, código morto ou resíduos de template.
- Comentários explicam o porquê, não repetem o código.

## Estratégia de testes

### Situação atual

`BarberApp.IntegrationTests` é uma suíte xUnit real, registrada em `BarberApp.slnx`. Ela usa `Microsoft.AspNetCore.Mvc.Testing`/`WebApplicationFactory`, substitui PostgreSQL por um banco EF Core InMemory isolado e configura segredos exclusivos de teste em memória. Os **21 casos** atuais cobrem: `401` sem API key e seu challenge; `403` para chave inválida, vazia ou múltipla; chave válida em endpoint público; preservação de JWT; falha de inicialização sem configuração válida; CORS preflight; rota inexistente; login; Swagger público em Development; e os esquemas/requisitos de segurança OpenAPI.

Essa suíte não substitui testes de persistência contra PostgreSQL para mudanças de schema, constraints, índices ou comportamento específico do provedor.

### Fluxo TDD obrigatório

TDD é obrigatório para **tudo que for criado ou modificado** neste projeto:

1. Escreva primeiro o teste que especifica o comportamento ou reproduz o problema.
2. Execute-o e confirme que falha pelo motivo correto (**RED**), sem implementação antecipada.
3. Pare para revisão dos testes quando a tarefa exigir aprovação humana; os testes são a especificação.
4. Implemente somente o mínimo para passar, sem enfraquecer ou reescrever o teste (**GREEN**).
5. Refatore em passos pequenos, mantendo toda a suíte verde (**REFACTOR**).

Não confunda teste que falha por erro de compilação, fixture quebrada ou ambiente inválido com RED válido.

### Obrigatório para código novo

- Entidades: unitários para sucesso, transições inválidas e limites.
- Services: unitários com dependências isoladas para sucesso, não encontrado, conflito e falha externa.
- Validators: casos válidos e inválidos relevantes.
- Endpoint ou contrato HTTP: integração cobrindo rota, autorização, status e corpo, inclusive cenários negativos.
- Persistência/migration: integração contra PostgreSQL compatível, incluindo constraint ou índice afetado.
- Bugfix: primeiro teste que reproduz a falha; depois a correção.

Nomeie testes como `Metodo_DeveResultado_QuandoCondicao`. Eles devem ser determinísticos, independentes de ordem, relógio real, IDs fixos ou dados de outra execução. Ao criar a infraestrutura, registre aqui projetos, bibliotecas, fixtures, banco e comandos específicos.

## Comandos essenciais

Execute na raiz.

### Restaurar, build e execução

```powershell
dotnet restore BarberApp.slnx
dotnet build BarberApp.slnx --no-restore
dotnet run --project .\BarberApp.API\BarberApp.API.csproj
dotnet watch --project .\BarberApp.API\BarberApp.API.csproj run
```

Em `Development`, HTTP usa `http://localhost:5087` e Swagger fica em `/swagger`. São necessários PostgreSQL e configuração local de `ConnectionStrings:DefaultConnection`, `JwtSettings` e `ApiKey:Value` (preferencialmente via `ApiKey__Value`).

### Testes

```powershell
# Toda a solução; confira no resumo quantos testes foram executados.
dotnet test BarberApp.slnx --no-restore

# Saída detalhada da suíte completa.
dotnet test BarberApp.slnx --logger "console;verbosity=normal"

# Somente os testes de integração.
dotnet test .\BarberApp.IntegrationTests\BarberApp.IntegrationTests.csproj --no-restore
```

Não documente nem execute `BarberApp.UnitTests`: esse projeto ainda não existe. Ao adicionar uma nova suíte, registre aqui framework, fixtures, isolamento, banco e comandos.

### Configuração local da API key

Configure um segredo local sem versioná-lo. A hierarquia de configuração do .NET converte `__` em `:`:

```powershell
$env:ApiKey__Value = "gere-uma-chave-longa-e-aleatoria"
dotnet run --project .\BarberApp.API\BarberApp.API.csproj
```

Também é possível usar Secret Manager ou um cofre do ambiente para preencher `ApiKey:Value`. Nunca coloque a chave real no README, no arquivo `.http`, em `appsettings*.json`, no código ou nos logs.

### Lint/formatação

```powershell
# Verifica sem alterar arquivos.
dotnet format BarberApp.slnx --verify-no-changes --no-restore

# Correção local intencional; revise o diff.
dotnet format BarberApp.slnx --no-restore
```

O lint falha no baseline por whitespace existente. Não rode a correção como efeito colateral de outra tarefa; normalize em mudança dedicada.

### EF Core e migrations

```powershell
dotnet ef --version

dotnet ef migrations list --project .\BarberApp.Infrastructure\BarberApp.Infrastructure.csproj --startup-project .\BarberApp.API\BarberApp.API.csproj

dotnet ef migrations add NomeDescritivo --project .\BarberApp.Infrastructure\BarberApp.Infrastructure.csproj --startup-project .\BarberApp.API\BarberApp.API.csproj

dotnet ef migrations script --idempotent --project .\BarberApp.Infrastructure\BarberApp.Infrastructure.csproj --startup-project .\BarberApp.API\BarberApp.API.csproj

dotnet ef database update --project .\BarberApp.Infrastructure\BarberApp.Infrastructure.csproj --startup-project .\BarberApp.API\BarberApp.API.csproj

# Somente se ainda não foi compartilhada/aplicada.
dotnet ef migrations remove --project .\BarberApp.Infrastructure\BarberApp.Infrastructure.csproj --startup-project .\BarberApp.API\BarberApp.API.csproj
```

## Regras de ouro — NUNCA fazer

### Aprendizado e revisão

- **NUNCA** aceitar, copiar ou gerar código sem conseguir explicar o que ele faz e por que foi escolhido.
- **NUNCA** escrever ou corrigir código no lugar do mantenedor sem autorização explícita e pontual para aquela edição.
- **NUNCA** avançar para o próximo passo de código sem dar ao mantenedor oportunidade real de escrever, perguntar e revisar o passo atual.
- **NUNCA** ocultar hipótese, trade-off, erro conhecido, teste ausente ou limitação da verificação.
- **NUNCA** entregar mudança grande e opaca quando puder ser dividida em passos estudáveis.
- **NUNCA** mudar padrão arquitetural sem atualizar este `CODEX.md`.

### Arquitetura e contratos

- **NUNCA** fazer `Domain` depender de camadas externas.
- **NUNCA** acessar `AppDbContext` em controller/service; use repositório.
- **NUNCA** colocar regra de negócio no controller ou regra HTTP no Domain/Application.
- **NUNCA** expor entidade do domínio/EF como novo contrato público.
- **NUNCA** alterar/remover rota, verbo, status, campo, tipo ou semântica pública sem avaliar consumidores, versionar incompatibilidades e testar o contrato.
- **NUNCA** criar endpoint sem teste automatizado proporcional ao contrato e aos riscos da mudança.
- **NUNCA** remover ou contornar a API key em uma rota da API para preservar um consumidor antigo; migre o consumidor para enviar `X-API-Key`.
- **NUNCA** tratar API key como substituta de JWT, role, claim ou autorização por propriedade do recurso.

### Segurança e dados

- **NUNCA** versionar senha, token, JWT secret, connection string com credencial ou chave. Use variável de ambiente, Secret Manager ou cofre.
- **NUNCA** considerar uma mudança pronta sem inspecionar diff e logs por possível vazamento de segredos.
- **NUNCA** criar admin com credencial fixa nem logar dado sensível.
- **NUNCA** confiar no request para autorização, expor stack trace ou revelar detalhes internos.
- **NUNCA** aplicar migration destrutiva sem backup, SQL revisado, rollback e confirmação do ambiente.
- **NUNCA** apagar/reescrever migration compartilhada ou aplicada; crie uma corretiva.
- **NUNCA** usar `DateTime.Now` para dado persistido ou converter local/UTC implicitamente.

### Qualidade

- **NUNCA** considerar `dotnet test` verde sem confirmar testes descobertos e executados.
- **NUNCA** considerar uma tarefa concluída antes de apresentar o diff e os riscos ao mantenedor e receber sua aprovação explícita.
- **NUNCA** ignorar teste/lint para “fazer passar” sem entender a causa.
- **NUNCA** corrigir bug sem teste de regressão, salvo impedimento explicitamente aceito.
- **NUNCA** escrever implementação antes do teste para código criado ou modificado; siga RED, revisão quando prevista, GREEN e REFACTOR.
- **NUNCA** misturar refatoração ampla, formatação global, migration e feature sem justificativa.
- **NUNCA** introduzir warning, código morto, dependência sem uso ou duplicação consciente sem registrar a dívida.

## Checklist de mudança

1. O autor explica cada trecho e decisão?
2. As responsabilidades e dependências das camadas foram respeitadas?
3. Contratos e autorização foram preservados ou versionados?
4. Testes proporcionais ao risco foram executados de fato?
5. O build passa sem novos warnings?
6. O lint não ganhou violações?
7. Migration e SQL foram revisados, se aplicável?
8. O diff não contém segredo ou dado sensível?
9. `README.md` e `CODEX.md` continuam verdadeiros?
10. O diff está pequeno, legível e pronto para ser estudado?
11. O diff completo e o resumo de riscos foram apresentados ao mantenedor?
12. O mantenedor revisou e aprovou explicitamente o diff?
13. Se houve múltiplos agentes ou loops, o relatório de esforço e oportunidades de redução de custo foi entregue?

Se alguma resposta for “não”, a mudança ainda não está pronta. Exceções precisam ser explicadas e aceitas conscientemente pelo mantenedor; os gates de revisão humana e de ausência de segredos não podem ser presumidos.
