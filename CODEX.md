# CODEX — BarberApp API

> Referência viva do projeto. Todo novo código, decisão arquitetural ou padrão adotado deve atualizar este arquivo na mesma mudança.

## Propósito: aprendizado ativo

O objetivo principal é **aprendizado ativo**. O código não deve apenas funcionar: cada linha precisa ser lida, entendida e questionada antes de ser aceita. Mudanças devem ser pequenas e revisáveis, com nomes claros, decisões explicadas e testes que demonstrem o comportamento.

O mantenedor quer ver, revisar e compreender o código. Não esconda decisões em automações opacas nem entregue grandes blocos gerados sem explicação. Ao propor uma solução, apresente o problema, as alternativas relevantes, o motivo da escolha e seus efeitos. Declare incertezas, hipóteses e dívidas técnicas.

## Estado auditado

Conferido contra o repositório em **22/09/2026**:

- Quatro projetos `net10.0`, com `Nullable` e `ImplicitUsings` habilitados; SDK usado na auditoria: `10.0.401`.
- `dotnet build BarberApp.slnx --no-restore` passa com zero erros e zero avisos.
- Não existem projetos, arquivos ou dependências de teste. `dotnet test BarberApp.slnx` termina com sucesso, mas atualmente **não executa testes**.
- Não há `.editorconfig`, analyzers adicionais ou lint dedicado. O lint adotado abaixo é `dotnet format --verify-no-changes`, que acusa problemas de formatação já existentes.
- O frontend está parcialmente iniciado, fora deste repositório.
- Há dívidas de segurança: uma credencial PostgreSQL está versionada em `appsettings.json` e o bootstrap cria um administrador com senha fixa. Não reproduza isso; a correção deve ser isolada e incluir rotação dos segredos expostos.

## Arquitetura e stack

O BarberApp é uma API REST para clientes, barbeiros, serviços, agenda semanal, exceções de agenda, agendamentos, perfis e pagamentos simulados. Usa ASP.NET Core Web API, C#/.NET 10, controllers MVC, EF Core 10, PostgreSQL/Npgsql, ASP.NET Core Identity, JWT Bearer, FluentValidation e Swagger/OpenAPI. O gateway ativo é um mock.

A solução é inspirada em Clean Architecture. `Domain` concentra entidades, enums, transições de estado e contratos. `Application` orquestra casos de uso em services e define DTOs e validators. `Infrastructure` implementa persistência, Identity e o gateway. `API` é a borda HTTP e o composition root em `Program.cs`.

Não é Clean Architecture estrita: `Infrastructure` referencia `Application`; alguns services retornam entidades para mapeamento nos controllers; e controllers de autenticação, perfil e barbeiro usam Identity diretamente. Isso descreve o estado atual, não autoriza aumentar o acoplamento. Melhorias devem ser incrementais, testadas e registradas aqui.

```text
HTTP -> API -> Application -> Domain
         \-> Infrastructure -> PostgreSQL

Dependências atuais:
API -> Application + Infrastructure
Infrastructure -> Application -> Domain
```

## Estrutura e responsabilidades

```text
BarberApp.slnx
├── BarberApp.API/
│   ├── Controllers/       rotas, claims, roles e respostas HTTP
│   └── Program.cs         composição, DI e pipeline HTTP
├── BarberApp.Application/
│   ├── DTOs/              contratos Request/Response
│   ├── Services/          orquestração dos casos de uso
│   └── Validators/        validação de entrada
├── BarberApp.Domain/
│   ├── Entities/          estado e comportamento do domínio
│   ├── Enums/             valores fechados
│   └── Interfaces/        contratos de repositório e pagamento
└── BarberApp.Infrastructure/
    ├── Data/              AppDbContext e mapeamentos
    ├── Identity/          ApplicationUser
    ├── Migrations/        histórico versionado do schema
    ├── Payment/           gateway mock
    └── Repositories/      consultas e persistência EF Core
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

Ainda não há suíte automatizada. Saída vazia de `dotnet test` não comprova comportamento. Até os projetos de teste existirem, declare que a verificação foi limitada a build, lint e/ou teste manual.

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

Em `Development`, HTTP usa `http://localhost:5087` e Swagger fica em `/swagger`. São necessários PostgreSQL e configuração local de `ConnectionStrings:DefaultConnection` e `JwtSettings`.

### Testes

```powershell
# Atualmente não encontra projetos de teste.
dotnet test BarberApp.slnx --no-restore

# Use quando houver suíte para enxergar os testes executados.
dotnet test BarberApp.slnx --logger "console;verbosity=normal"
```

Não documente comandos para `BarberApp.UnitTests` ou `BarberApp.IntegrationTests` até eles existirem na solução.

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
- **NUNCA** ocultar hipótese, trade-off, erro conhecido, teste ausente ou limitação da verificação.
- **NUNCA** entregar mudança grande e opaca quando puder ser dividida em passos estudáveis.
- **NUNCA** mudar padrão arquitetural sem atualizar este `CODEX.md`.

### Arquitetura e contratos

- **NUNCA** fazer `Domain` depender de camadas externas.
- **NUNCA** acessar `AppDbContext` em controller/service; use repositório.
- **NUNCA** colocar regra de negócio no controller ou regra HTTP no Domain/Application.
- **NUNCA** expor entidade do domínio/EF como novo contrato público.
- **NUNCA** alterar/remover rota, verbo, status, campo, tipo ou semântica pública sem avaliar consumidores, versionar incompatibilidades e testar o contrato.
- **NUNCA** criar endpoint sem teste automatizado. Como a infraestrutura ainda não existe, criá-la faz parte da primeira nova mudança de endpoint.

### Segurança e dados

- **NUNCA** versionar senha, token, JWT secret, connection string com credencial ou chave. Use variável de ambiente, Secret Manager ou cofre.
- **NUNCA** criar admin com credencial fixa nem logar dado sensível.
- **NUNCA** confiar no request para autorização, expor stack trace ou revelar detalhes internos.
- **NUNCA** aplicar migration destrutiva sem backup, SQL revisado, rollback e confirmação do ambiente.
- **NUNCA** apagar/reescrever migration compartilhada ou aplicada; crie uma corretiva.
- **NUNCA** usar `DateTime.Now` para dado persistido ou converter local/UTC implicitamente.

### Qualidade

- **NUNCA** considerar `dotnet test` verde sem confirmar testes descobertos e executados.
- **NUNCA** ignorar teste/lint para “fazer passar” sem entender a causa.
- **NUNCA** corrigir bug sem teste de regressão, salvo impedimento explicitamente aceito.
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

Se alguma resposta for “não”, a mudança ainda não está pronta ou a exceção deve ser explicada e aceita conscientemente.
