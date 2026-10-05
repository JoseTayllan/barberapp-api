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

### Exceção autorizada — execução autônoma do roadmap

Em 03/10/2026, o mantenedor autorizou implementar autonomamente o roadmap usando TDD. Durante esse trabalho, o agente pode escrever testes, confirmar RED, implementar GREEN e refatorar sem solicitar autoria manual a cada passo. Esta exceção se limita ao roadmap e não remove o propósito de aprendizado ativo nem o fluxo humano padrão descrito acima.

- Cada incremento deve produzir material de estudo dividido por assunto em `docs/estudos`, com problema, decisão, exemplos, exercícios e respostas.
- Mantenha um registro de RED/GREEN e um relatório de diff, riscos e verificações. Não enfraqueça testes para contornar erro de ambiente.
- Apresente os incrementos para revisão; autorização de implementação não presume aprovação final, merge, publicação, rotação de credenciais ou reescrita do histórico.
- A entrega pretendida é um MVP funcionando localmente. Hospedagem ainda não foi escolhida. O pagamento escolhido é Mercado Pago Checkout Pro, com conta do dono conectada por OAuth.
- Decisões de produto pendentes são registradas em `ROADMAP.md`; não transforme hipóteses em recursos aprovados.

### Incremento de disponibilidade — 03/10/2026

Foi criado `BarberApp.UnitTests`, com xUnit e referências à Application/Domain, registrado em `BarberApp.slnx`. Execute `dotnet test BarberApp.UnitTests/BarberApp.UnitTests.csproj --no-restore` para o recorte unitário.

A disponibilidade deve satisfazer `0 <= início < fim <= 24h` e usar um dia definido no enum. O limite de 24h representa o final do dia. A validação ocorre antes de modificar qualquer propriedade, preservando o estado se a atualização for rejeitada. GREEN confirmado pelo mantenedor em 03/10/2026: 59 testes aprovados, zero falhas/ignorados. O ambiente do agente encontrou bloqueio de carregamento pelo Smart App Control (`0x800711C7`); essa limitação não ocorreu no terminal do mantenedor. O diff ainda precisa de revisão final.

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

Conferido contra o repositório em **26/09/2026**:

- Cinco projetos `net10.0`, com `Nullable` e `ImplicitUsings` habilitados: os quatro projetos da aplicação e `BarberApp.IntegrationTests`.
- `dotnet build BarberApp.slnx --no-restore` passa com zero erros e zero avisos.
- `BarberApp.IntegrationTests` usa xUnit, `WebApplicationFactory` e EF Core InMemory. A suíte cobre API key, compatibilidade com JWT, OpenAPI, configuração versionada sem credenciais e bootstrap administrativo.
- Não há `.editorconfig`, analyzers adicionais ou lint dedicado. O lint adotado abaixo é `dotnet format --verify-no-changes`, que acusa problemas de formatação já existentes.
- O frontend está parcialmente iniciado, fora deste repositório.
- A connection string com credencial e a senha administrativa fixa foram removidas do estado atual dos arquivos rastreados. A rotação das credenciais anteriormente expostas, o tratamento do histórico Git e a migração/validação dos segredos locais ainda são obrigações pendentes; remover o valor do arquivo atual não invalida um segredo já divulgado.

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
    ├── Security/          configuração segura e bootstrap administrativo
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

### Bootstrap administrativo

- O bootstrap é lógica de inicialização no composition root, não endpoint nem caso de uso público.
- `BootstrapAdmin:Enabled` ausente ou `false` não cria usuário; as roles continuam sendo garantidas. Valor inválido deve causar fail-fast.
- Quando habilitado, `BootstrapAdmin:NomeCompleto`, `BootstrapAdmin:Email` e `BootstrapAdmin:Password` são obrigatórios. Em ambiente, use `BootstrapAdmin__Enabled`, `BootstrapAdmin__NomeCompleto`, `BootstrapAdmin__Email` e `BootstrapAdmin__Password`.
- Habilite somente para a criação inicial. Após confirmar a conta com role `Admin`, desabilite o bootstrap e remova senha, nome e email do ambiente de execução e do mecanismo de deploy/cofre.
- Conta preexistente com o email configurado e sem role `Admin` é conflito de segurança: falhe sem promovê-la. Conta preexistente já Admin torna a operação idempotente.
- Falhas de criação de roles, usuário ou vínculo de role interrompem a inicialização e devem divulgar apenas códigos Identity, nunca valores ou descrições que possam conter dados sensíveis.
- Em disputa concorrente por email, aceite somente a conta que outra instância já concluiu como Admin. O retry atual faz até 10 consultas e nove esperas de 100 ms (aproximadamente 900 ms); ele nunca promove a conta concorrente.
- Se vincular a role falhar após criar o usuário, tente compensar apagando o usuário. Falha retornada ou exception do provider durante o cleanup implica estado possivelmente parcial: interrompa, inspecione banco/Identity e corrija manualmente antes de tentar novamente.

### Pagamentos — decisão de produto e contrato em desenvolvimento

- Cada instalação atende uma única barbearia e uma única conta recebedora do dono. Não haverá split, comissões da plataforma nem repasses aos barbeiros; o dono paga os profissionais fora da aplicação.
- Checkout Pro é a experiência escolhida. O cliente escolhe entre os meios disponibilizados pelo Mercado Pago; não garantir suporte a qualquer cartão de débito.
- A conexão recebedora pertence à instalação, não ao barbeiro do agendamento. Somente usuário autenticado com role `Admin` inicia conexão/desconexão.
- OAuth deve usar URL oficial fixa, `state` imprevisível, expiração, uso único e PKCE S256. O callback deve validar a tentativa antes de trocar o código por tokens. Nunca retornar `ClientSecret`, access token, refresh token ou verifier ao navegador.
- Tokens recebidos são credenciais dinâmicas de terceiro: seu armazenamento exige proteção criptográfica/cofre e chave de proteção fora do banco. A persistência protegida foi adicionada no incremento de 05/10/2026; renovação/revogação e recuperação das chaves continuam gates antes de uso real. Nunca gravar plaintext.
- Primeiro contrato proposto: `POST /api/integracoes/mercado-pago/autorizacao`, exige API key + JWT Admin, retorna `urlAutorizacao` e `expiraEm`, com `Cache-Control: no-store`. Configuração inválida retorna `503` sem detalhes. Ausência de identificador autenticado retorna `401`.
- Configuração privada por `MercadoPago__ClientId`, `MercadoPago__ClientSecret` e `MercadoPago__RedirectUri`; o redirect precisa ser estático e corresponder à aplicação cadastrada no provedor. Nunca aceitar URL de redirecionamento enviada pelo cliente.
- O início da autorização está implementado com interface/DTO em Application, serviço em Infrastructure e controller na API. RED no WSL: 14 falhas por rota inexistente e 1 aprovado. GREEN: 74 testes da solução aprovados (14 unitários + 60 integração), sem alterar a especificação OAuth. Procedimento em `docs/ambiente-wsl.md`.
- A tentativa OAuth é temporária em `IMemoryCache`, por identificador Admin: uma nova tentativa substitui a anterior, expira em dez minutos e se perde no reinício. `TentativasMercadoPagoStore` é singleton e consome state atomicamente sob lock local. Expiração, replay, vínculo, substituição e concorrência do consumo são testados. Isso não coordena múltiplas instâncias. O pagamento/checkout ativo continua mock.

### Incremento callback OAuth — em revisão

- Exceção deliberada à API key: somente `GET /api/integracoes/mercado-pago/callback`, para receber o redirect externo. Não ampliar para prefixos, outros verbos ou endpoints. A autorização inicial continua exigindo API key + JWT Admin.
- Serviço OAuth scoped usa o store singleton para consumir state atomicamente nesta instância; contexto associa state ao administrador, verifica expiração e invalida tentativa anterior ao reiniciar. Testes HTTP cobrem recusa, repetição, concorrência, expiração com relógio controlado, separação entre administradores e parâmetros ambíguos. O serviço scoped pode usar repositório/DbContext scoped sem capturar dependências de vida curta em singleton.
- Com conexão desabilitada (padrão), código válido continua retornando `503 MercadoPagoTrocaTokenPendente` e consome a tentativa, preservando o contrato anterior. `access_denied` válido retorna `AutorizacaoRecusada`. Quando habilitada e configurada, troca e gravação protegida devem concluir antes de retornar `Conectado`. Ainda não foi validado com conta real/sandbox remoto.
- RED: 17 falhas e 4 aprovações em 21 casos novos. A falha posterior de serialização OpenAPI foi resolvida com `security: [{}]` após aprovação explícita do mantenedor em 03/10/2026. O teste exige exatamente um requisito vazio no callback e preserva API key + Bearer na autorização. GREEN: 95 testes aprovados (14 unitários + 81 integração), zero falhas/ignorados; build sem avisos/erros. Diff final ainda deve ser revisado.
- Não registrar queries OAuth. Código novo não adiciona logs; mantenha a restrição atual de `Microsoft.AspNetCore` a Warning. Instrumentação e proxies precisam de redaction específica antes de produção.

### Troca de tokens e armazenamento protegido — 05/10/2026

- Testes HTTP em `MercadoPagoTokenExchangeTests`, com `IHttpClientFactory` e handler simulados, sem rede ou credenciais reais. Os 95 testes anteriores e as 16 expectativas aprovadas dessa troca foram preservados.
- Opt-in implementado: `MercadoPago__ConexaoEnabled=true`; ausente/false preserva `503 MercadoPagoTrocaTokenPendente`. Exige `MercadoPago__Sandbox` booleano explícito para a troca; começar com sandbox, nunca habilitar produção como efeito colateral dos testes.
- Com conexão habilitada, usar POST JSON para `https://api.mercadopago.com/oauth/token`, grant `authorization_code`, redirect estático e o verifier correspondente ao challenge original. Em sandbox, `test_token=true`.
- Falha HTTP externa: `502 MercadoPagoProvedorFalhou`; JSON ou campos inválidos: `502 MercadoPagoRespostaInvalida`; timeout: `503 MercadoPagoIndisponivel`. Não copiar corpo do provedor, tokens, código ou verifier para respostas/logs. Não repetir automaticamente a troca de código.
- State deve ser validado e consumido antes da chamada externa. State desconhecido e recusa não contatam o provedor; repetição não envia novamente o código.
- RED da troca: 16 casos, 14 falhas esperadas e 2 aprovados. RED do armazenamento: 4 falhas esperadas; RED das invariantes da entidade: 7 falhas e 1 aprovado; regressão do driver indisponível reproduzida antes da correção. GREEN com PostgreSQL isolado: 125 aprovados (22 unitários + 103 integração), zero falhas/ignorados; build sem avisos/erros. O sucesso `200 Conectado` acontece somente após criptografia e SaveChangesAsync.
- `ConexaoMercadoPago` guarda apenas ciphertext, conta recebedora, Admin de origem, expiração UTC e sandbox. Repositório no Domain/Infrastructure, tabela `ConexoesMercadoPago`: PK fixa `Id=1` e CHECK `Id=1` impõem uma linha por instalação. Nunca adicionar colunas plaintext de access/refresh token.
- Data Protection usa o propósito `BarberApp.MercadoPago.Tokens.v1`. Chaves geradas ficam fora do banco/repositório, no mecanismo do ambiente; os testes usam provider efêmero. Em Windows com perfil disponível, o padrão usa proteção DPAPI. Antes de sandbox remoto/produção, verificar persistência, ACL, proteção das chaves e recuperação/backup. Não presumir proteção em disco no Linux nem apagar chaves antigas; mudar identidade, ambiente ou isolamento da aplicação pode inviabilizar a leitura de dados existentes.
- Migration `20261005172851_AddConexaoMercadoPago` e snapshot são versionados juntos. Aplicada somente ao banco de testes WSL; nunca aplicar automaticamente ao banco do mantenedor. A reversão remove a tabela e perde credenciais; exige backup e aprovação se houver dados.
- Cliente HTTP dedicado tem timeout de 10s, limite de resposta de 64 KiB e redirects desabilitados; não há retry automático. Classes internas de tentativa/tokens não são records para evitar ToString com credenciais. Não ativar logging de bodies/queries ou EnableSensitiveDataLogging.
- Renovação, desconexão/revogação, concorrência na substituição da conta, checkout/webhooks, frontend e sandbox remoto permanecem pendentes. A constraint evita duas linhas, mas não define qual reconexão concorrente vence. Antes de cobrança real, resolver esses gates. Os testes atuais não certificam o fluxo completo de pagamentos.

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

`BarberApp.IntegrationTests` é uma suíte xUnit real, registrada em `BarberApp.slnx`. Ela usa `Microsoft.AspNetCore.Mvc.Testing`/`WebApplicationFactory`, substitui PostgreSQL por um banco EF Core InMemory isolado e configura valores exclusivos de teste em memória. A cobertura atual inclui: contrato e casos de borda da API key; preservação de JWT; CORS; login; Swagger/OpenAPI; ausência de senha na connection string versionada; configuração, idempotência, concorrência, fail-fast e compensação do bootstrap administrativo.

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

Em `Development`, HTTP usa `http://localhost:5087` e Swagger fica em `/swagger`. São necessários PostgreSQL e configuração local de `ConnectionStrings:DefaultConnection`, `JwtSettings` e `ApiKey:Value`, preferencialmente por variáveis de ambiente/Secret Manager. Para o bootstrap inicial opcional, siga o procedimento seguro abaixo.

### Testes

```powershell
# Toda a solução; confira no resumo quantos testes foram executados.
dotnet test BarberApp.slnx --no-restore

# Saída detalhada da suíte completa.
dotnet test BarberApp.slnx --logger "console;verbosity=normal"

# Somente os testes de integração.
dotnet test .\BarberApp.IntegrationTests\BarberApp.IntegrationTests.csproj --no-restore
```

`BarberApp.UnitTests` contém testes unitários sem banco; `BarberApp.IntegrationTests` exercita a aplicação via HTTP/Identity. Execute ambos pela solução. Testes unitários não substituem verificação HTTP ou persistência PostgreSQL.

### Configuração local da API key

Configure um segredo local sem versioná-lo. A hierarquia de configuração do .NET converte `__` em `:`:

```powershell
$env:ApiKey__Value = "gere-uma-chave-longa-e-aleatoria"
dotnet run --project .\BarberApp.API\BarberApp.API.csproj
```

Também é possível usar Secret Manager ou um cofre do ambiente para preencher `ApiKey:Value`. Nunca coloque a chave real no README, no arquivo `.http`, em `appsettings*.json`, no código ou nos logs.

### Bootstrap inicial do administrador

Use `BootstrapAdmin__Enabled`, `BootstrapAdmin__NomeCompleto`, `BootstrapAdmin__Email` e `BootstrapAdmin__Password` somente no ambiente seguro da criação inicial. Os valores reais não pertencem a comandos versionados, documentação ou logs. Depois de validar a criação e a role `Admin`, defina `Enabled=false` ou remova a configuração e apague os demais valores do ambiente/cofre de deploy. Falha de configuração ou Identity bloqueia o startup; conflito com conta não Admin e falha de compensação exigem intervenção manual, nunca promoção automática ou nova tentativa cega.

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
- **NUNCA** manter `BootstrapAdmin:Enabled=true` depois da criação inicial confirmada, promover automaticamente conta existente ou ignorar falha de compensação.
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
