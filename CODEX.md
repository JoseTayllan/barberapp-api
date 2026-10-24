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
- Tokens recebidos são credenciais dinâmicas de terceiro: seu armazenamento exige proteção criptográfica/cofre e chave de proteção fora do banco. A persistência protegida foi adicionada no incremento de 05/10/2026; renovação manual adicionada em 19/10/2026. Revogação, recuperação das chaves, coordenação concorrente e recuperação de rotação sem persistência continuam gates antes de uso real. Nunca gravar plaintext.
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
- Renovação automática, desconexão/revogação, concorrência na substituição da conta, checkout/webhooks, frontend e sandbox remoto permanecem pendentes. Renovação manual foi adicionada em 19/10/2026, conforme seção própria. A constraint evita duas linhas, mas não define qual reconexão concorrente vence. Antes de cobrança real, resolver esses gates. Os testes atuais não certificam o fluxo completo de pagamentos.

### Status local da conexão — GREEN técnico, diff em revisão (05/10/2026)

- Implementado após aprovação dos testes: `GET /api/integracoes/mercado-pago/status`, API key + JWT Admin com identificador, sem exceção de autenticação herdada do callback.
- Resposta contém somente `status`, `contaId`, `expiraEm` e `sandbox`. Sem conta: `NaoConectado`, demais campos null. Com conta: `Conectado` se expiração > UTC agora; caso contrário `Expirado`, preservando os metadados. Não devolver Admin de origem, ciphertext ou tokens.
- Trata-se de status local, não comprovação de token aceito pelo provedor: não criar cliente HTTP, descriptografar, renovar ou alterar dados. Funciona sem configurar/habilitar OAuth; qualquer Admin identificado consulta a conta única da instalação.
- Sucesso e falha esperada de consulta usam no-store. Falha do driver na consulta: `503 MercadoPagoConsultaFalhou`, sem detalhes internos. Swagger exige ApiKey e Bearer juntos.
- `IConsultaConexaoMercadoPagoRepository` separa leitura do contrato de gravação existente. Implementação EF projeta somente `MetadadosConexaoMercadoPago` com AsNoTracking; não seleciona ciphertext/Admin nem expõe IQueryable. DTO imutável fica na Application; service compara relógio e controller autoriza/traduz HTTP. Sem nova migration ou mudança nos consumidores do contrato de gravação.
- RED: 16 executados no WSL, 13 falhas esperadas e três aprovados. Após aprovação, GREEN dos mesmos 16, sem alterar testes; suíte completa com PostgreSQL: 141 aprovados (22 unitários + 119 integração), zero falhas/ignorados. Build sem avisos/erros; lint dos oito arquivos C# do recorte aprovado, não lint global. Diff aguarda aprovação final; não é autorização de uso real.

### Renovação manual — GREEN técnico, diff em revisão (19/10/2026)

- Incremento de status aprovado e registrado pelo mantenedor no commit `86cba24`. Após aprovação dos 28 testes, implementado `POST /api/integracoes/mercado-pago/renovacao`, somente API key + JWT Admin identificado. Não recebe tokens do navegador.
- Usar refresh token recuperado do payload protegido da conta única, POST JSON no host OAuth fixo com grant_type refresh_token e credenciais privadas. Não reutilizar code/verifier do callback; enviar test_token conforme ambiente validado contra a conexão. Sucesso somente depois de proteger e salvar os dois tokens novos, preservando conta, sandbox e Admin de origem; resposta apenas status/contaId/expiraEm/sandbox, com no-store.
- Sem conta: 409 MercadoPagoNaoConectado. Conexão desabilitada: 503 MercadoPagoConexaoDesabilitada; credenciais da aplicação ausentes: 503 MercadoPagoNaoConfigurado; ambiente diferente do registro: 503 MercadoPagoAmbienteIncompativel. Impedir chamada externa nesses casos.
- Ciphertext inválido, JSON local inválido ou refresh ausente: 503 MercadoPagoCredenciaisIndisponiveis. HTTP remoto não exitoso: 502 MercadoPagoProvedorFalhou; invalid_grant estruturado em 400: 409 MercadoPagoReautorizacaoNecessaria. Resposta inválida ou conta diferente: 502 MercadoPagoRespostaInvalida; timeout: 503 MercadoPagoIndisponivel. Não copiar bodies/segredos nem fazer retry automático.
- Falha de proteção/gravação: 503 MercadoPagoProtecaoFalhou/MercadoPagoPersistenciaFalhou, preservando o registro anterior sem declarar sucesso. Isso não comprova validade remota do token antigo: a emissão remota e a gravação local não são uma transação distribuída.
- RED dos 28 testes: 26 falhas esperadas por rota ausente e dois aprovados. Após aprovação, os mesmos 28 passaram sem alteração; SHA-256 do arquivo preservado durante GREEN. Suíte completa no WSL com PostgreSQL isolado: 169 aprovados (22 unitários + 147 integração), zero falhas/ignorados, build sem erros/avisos e lint dos sete arquivos C# do recorte aprovado. Não é lint global nem validação sandbox real.
- Serviço scoped específico RenovacaoMercadoPagoService, contrato na Application e leitura de credenciais por ICredenciaisConexaoMercadoPagoRepository. Essa leitura traz a entidade protegida com AsNoTracking somente para o caso de uso; a consulta de status mantém a projeção sem ciphertext. Repositório de gravação preexistente e schema preservados. Cliente OAuth compartilha transporte/validação entre code e refresh; somente a renovação interpreta invalid_grant. Sem retry, dependência nova, migration ou configuração real ativada.
- Operações simultâneas, recuperação após rotação sem persistência, revogação e execução automática continuam gates antes de uso real; este recorte não os resolve. Testes HTTP usam InMemory/HTTP simulado; o caso PostgreSQL existente cobre a persistência/migration/constraint anterior, não o novo fluxo HTTP de renovação contra PostgreSQL. Diff aguarda revisão final.

### Concorrência local — GREEN técnico, diff em revisão (19/10/2026)

- Renovação manual aprovada e registrada pelo mantenedor em `9f760ae`. Após aprovação dos nove testes, implementada uma única operação alteradora de credenciais por vez nesta instância da API, compartilhada por renovação e troca do código no callback.
- Primeira operação admitida mantém exclusividade até terminar gravação ou falhar. Concorrente recebe imediatamente 409 MercadoPagoOperacaoEmAndamento, com no-store e sem tokens, ciphertext ou state, antes de chamar o provedor. Não enfileirar nem fazer retry automático. Esse status adicional precisa ser tratado pelos consumidores.
- O callback válido rejeitado por ocupação deve manter a tentativa disponível para reenvio manual enquanto válida, pois seu código ainda não foi enviado ao provedor. Depois de aceito, state continua de uso único. Callback inválido permanece 400; consulta de status permanece disponível e sem acesso a tokens.
- Nova solicitação após a conclusão deve ler as credenciais atuais: renovação não reutiliza refresh capturado antes da reconexão. Liberar a exclusividade também após falha externa ou de persistência; isso prova disponibilidade local, não validade remota do token preservado.
- Nove casos HTTP controlam a ordem com TaskCompletionSource e interceptor de SaveChangesAsync, sem Thread.Sleep/Task.Delay. WaitAsync de dez segundos é proteção de diagnóstico contra deadlock, não garantia de tempo ou mecanismo de ordenação.
- RED funcional: oito falhas e um aprovado; após aprovação, GREEN dos mesmos nove sem alterar o arquivo de testes. Suíte completa no WSL com PostgreSQL isolado, sem filtro: 178 aprovados (22 unitários + 156 integração), zero falhas/ignorados. Build sem erros/avisos; lint dos seis arquivos C# do recorte aprovado. Não é lint global nem teste distribuído.
- CoordenadorOperacoesMercadoPago é singleton sem dependências scoped ou credenciais; usa admissão atômica Interlocked.CompareExchange e devolve escopo IDisposable com liberação idempotente. Services scoped usam using até concluir I/O. Renovação adquire antes de ler credenciais; callback consulta a tentativa, adquire e então consome novamente sob lock. Recusa/fluxo desabilitado preservam seus contratos sem alterar credenciais. Status não adquire exclusividade. Sem fila, lock de banco, nova dependência ou migration.
- Limite explícito: uma instância/processo. Não garante exclusão entre réplicas, escrita externa no banco, efeitos remotos após timeout/cancelamento ou recuperação de rotação sem persistência. Não habilitar pagamentos reais nem apresentar como coordenação distribuída.

### Recuperação durável — GREEN técnico, diff em revisão (20/10/2026)

- Concorrência local aprovada e registrada em `0d485a8`. O mantenedor aprovou evoluir a regra de recuperação e revisar explicitamente os testes antigos conflitantes; essa autorização não é permissão para alterar testes durante GREEN sem nova revisão.
- Implementado após aprovação da especificação: marcador interno persistido `RequerReautorizacao`, bool não nulo com default false para compatibilidade dos registros anteriores. Migration `20261020213112_AddEstadoRecuperacaoMercadoPago`, designer e snapshot versionados juntos. Marcador true representa credenciais cuja segurança de reutilização não está confirmada, não revogação remota comprovada.
- Depois de validar as credenciais locais e antes de enviar refresh ao provedor, persistir a pendência. Se não conseguir, retornar 503 MercadoPagoPendenciaNaoRegistrada e não enviar o refresh. Tokens/ciphertext/metadados anteriores permanecem intactos. Essa etapa altera somente o marcador, não os tokens.
- Enquanto a pendência estiver persistida, GET status mantém os quatro campos e retorna ReautorizacaoNecessaria; POST renovacao retorna 409 MercadoPagoReautorizacaoNecessaria sem chamada externa. É uma sinalização preventiva desde o início da operação, inclusive temporariamente durante I/O; uma renovação que termine com tokens novos salvos remove a pendência e volta a Conectado, sem exigir outro OAuth nesse caso.
- Após timeout, HTTP 5xx, resposta inválida ou falha em proteger/salvar tokens recebidos, manter a pendência mesmo em novo host. Somente persistência bem-sucedida dos tokens novos pode limpar o marcador; recusa ou falha de uma nova autorização não libera credenciais antigas. Nova autorização OAuth bem-sucedida permite recuperação sem retry do refresh incerto.
- Revisão autorizada da especificação anterior: dois casos de liberação após falha agora exigem bloqueio da próxima renovação e recuperação por callback; exclusividade local continua sendo liberada. Interceptors antigos foram refinados para falhar/pausar na substituição do ciphertext, não no registro prévio da pendência. As expectativas de erro, preservação e ausência de vazamento continuam exigidas.
- RED da especificação: 191 executados, 177 aprovados e 14 falhas esperadas. Após aprovação, implementação sem alterar os quatro arquivos de testes; GREEN da solução no WSL com PostgreSQL isolado: 191 aprovados (22 unitários + 169 integração), zero falhas/ignorados. Build sem erros/avisos; verificação de formatação dos 11 arquivos C# do recorte aprovada; EF confirma modelo sem mudanças pendentes. Não é lint global nem teste sandbox remoto.
- Novo host com InMemoryDatabaseRoot compartilhado testa independência do singleton e das instâncias de serviço, não reinício real de processo/disco. Teste PostgreSQL especifica leitura por outro contexto na mesma transação e default para INSERT legado; não prova backup de chaves nem coordenação distribuída. Rollback da futura coluna pode apagar a informação de incerteza: revisar SQL/risco antes de qualquer reversão.
- Registro por IPendenciaConexaoMercadoPagoRepository, scoped, sem alterar contratos de gravação anteriores. Entidade marca a pendência; repositório grava só esse campo antes da chamada. O fluxo de sucesso passa entidade com tokens novos/flag false ao SaveChangesAsync existente: limpeza e ciphertext são persistidos juntos. Status projeta metadados e marcador sem selecionar ciphertext; DTO público mantém exatamente quatro campos.
- SQL revisado em docs/sql/20261020213112_AddEstadoRecuperacaoMercadoPago.sql: somente ADD boolean NOT NULL DEFAULT FALSE e histórico EF, não idempotente, requer baseline anterior. Aplicado apenas ao banco de testes WSL; novo caso PostgreSQL confirmou INSERT legado/default e leitura true por outro contexto, com rollback dos dados. Não aplicado ao banco da aplicação. Down remove a coluna e exige análise do risco de reativação antes de qualquer uso.

### Qualificação das chaves entre processos — 22/10/2026

- O mantenedor confirmou a aplicação das duas migrations Mercado Pago ao banco local, com backup prévio, histórico de oito migrations e coluna RequerReautorizacao conferidos por psql. Confirmou também login Admin/status 200 NaoConectado. Essas são evidências fornecidas pelo mantenedor, não execução do agente contra o banco local.
- API habilitada para User Secrets pelo UserSecretsId adicionado pelo mantenedor ao csproj. O identificador não é segredo. Chave local gravada por JSON na entrada padrão de dotnet user-secrets set, sem valor literal em comando/log. O mantenedor removeu a variável de sessão, iniciou a API em Development e validou novo 200. Não rodar user-secrets list em relatórios. User Secrets mantém valores fora do repositório, mas não os criptografa nem substitui cofre de produção.
- Metadados informados pelo mantenedor: dois arquivos na pasta padrão Windows com encryptedSecret/DPAPI; ACL da pasta restrita ao usuário, SYSTEM e administradores. Isso não associa esses arquivos à API nem comprova recuperação de tokens, ACL individual de cada arquivo ou backup recuperável em outra máquina.
- Novos DataProtectionProcessTests qualificam o componente .NET em processos filhos distintos: mesmo keyring/aplicação/propósito recupera texto fictício; aplicação diferente, propósito diferente, keyring vazio e ciphertext adulterado são rejeitados; cópia das chaves recupera sob o mesmo contexto de usuário. A leitura desabilita geração automática: chave ausente não é substituída silenciosamente.
- No primeiro recorte, BarberApp.TestSupport.DataProtectionProbe qualificava somente o framework, sem iniciar a API. Continua exclusivamente infraestrutura de teste. ProjectReference sem referência de assembly e target MSBuild copiam o executável e metadados para ProtectionProbe no output dos testes. Cada processo termina antes do próximo; limite de 20 segundos é diagnóstico, não ordenação por relógio. Só códigos/indicadores fixos em stdout no modo original, nunca plaintext/ciphertext/chaves. Chaves e payload fictícios ficam em pasta temporária exclusiva, removida com validação de escopo. Não ler/copiar/remover o keyring real.
- Aplicação/propósito/keyring da prova são explícitos e isolados. No Windows o probe solicita DPAPI; no WSL as chaves fictícias não têm proteção equivalente em disco. GREEN observado apenas no WSL: seis casos passaram de primeira, sem RED funcional. É caracterização de recurso já fornecido pelo framework, não implementação nova de produção; não fabricar falha ou enfraquecer teste para simular TDD. Mudanças futuras no comportamento da aplicação continuam exigindo RED antes de GREEN.
- Suíte completa WSL com PostgreSQL isolado: 197 aprovados (22 unitários + 175 integração), zero falhas/ignorados. Compilação sem avisos/erros e dotnet format do recorte de dois C# aprovado. Nenhuma implementação da API alterada pelo agente. O probe não usa o composition root nem o discriminador padrão real do BarberApp; não certifica proteção Windows, crash, recuperação das chaves do mantenedor ou migração entre máquinas. Estudo 10 e registro em docs/execucao-roadmap.md; diff aguardando revisão.

### Especificação do diagnóstico pelo provider da API — 22/10/2026

- Log de inicialização fornecido pelo mantenedor confirmou que a API Windows usa %LOCALAPPDATA%/ASP.NET/DataProtection-Keys e Windows DPAPI. Isso associa a pasta à execução observada, mas não comprova recuperação de ciphertext. Nenhuma chave real foi aberta/copiada pelo agente.
- Adicionados somente quatro testes ApiDataProtectionProcessTests e documentação. Especificam futuros modos api-proteger/api-recuperar no auxiliar existente, resolvendo IDataProtectionProvider no contêiner inicializado pelo Program.cs, não DataProtectionProvider.Create nem substituto efêmero. Banco InMemory, bootstrap desabilitado, configuração fictícia e keyring temporário serão substituições exclusivas do diagnóstico; não alterar o Program.cs nem o discriminador para fazer passar.
- Casos: mesmo content root/keyring/propósito recupera em outro processo; outro content root isola aplicações no comportamento padrão atual; outro propósito é rejeitado; pasta vazia é rejeitada sem geração automática na leitura. Saída JSON estrita com origem ApiServices, status, processoId e hash SHA-256 do discriminador (não de uma chave). Zero plaintext/ciphertext/segredo em stdout. Limite de 30 segundos é diagnóstico, não mecanismo de ordenação.
- Na fase inicial, execução WSL compilou e executou os testes: quatro falhas por modo não suportado (exit 2 em vez de 0 na preparação). A lacuna era do contrato do auxiliar, não RED funcional da API; nenhum teste alcançava recuperação/rejeição criptográfica. Não confundir preparação ausente com bug no framework. Suíte nessa fase: 201 executados, 197 aprovados/quatro falhos, zero ignorados, PostgreSQL isolado. O mantenedor aprovou a especificação antes da implementação abaixo.
- GREEN após aprovação, sem alterar ApiDataProtectionProcessTests (SHA-256 94FF424876D60C76E5106E8C1D2E4D1C5795945172CAB0605EA43BF305C94E77). Modos api-* implementados só no auxiliar: ApiProbeFactory inicializa entry point real via WebApplicationFactory com ambiente ProtectionProbe, content root temporário, configuração fictícia, bootstrap/conexão desabilitados, logs removidos e PostgreSQL substituído por InMemory. ApiProtectionScenario resolve IDataProtectionProvider e DataProtectionOptions da API; não define SetApplicationName nem troca por provider efêmero. Diretório de chaves é isolado; leitura desabilita geração automática. A proteção Windows é solicitada explicitamente para esse keyring fictício; não comprovada pela execução WSL.
- Auxiliar passou a referenciar API/Mvc.Testing/InMemory nas versões já utilizadas (10.0.5); nenhum pacote de produção novo. Cópia para output/ProtectionProbe restrita a DLLs, deps/runtimeconfig e manifest de teste, sem appsettings. Alterar o filtro não remove artefatos antigos: validação usou configuração nova ProbeVerification nos dois projetos de testes, sem apagar diretórios. 201 aprovados (22 unitários + 179 integração), zero falhas/ignorados, PostgreSQL isolado; build final da solução sem erros/avisos e formatação dos quatro C# do recorte aprovada. Não é lint global. Nenhum arquivo de implementação da API alterado. Diff final em revisão.
- Este recorte automatizado não usa o keyring real do Windows. Leitura local futura deve ser explícita, sem gerar/rotacionar chaves e sem acessar banco, contas ou segredos reais. Não habilitar esse modo antes de revisão. Ainda não há certificação de DPAPI/reinício real da instalação ou backup entre máquinas. Material em docs/estudos/11-provider-da-api-e-especificacao.html.

### Modo seguro para o perfil Windows — barreiras aprovadas e prova local relatada (22/10/2026)

- Incremento anterior aprovado e publicado pelo mantenedor em 595e35e. As barreiras foram posteriormente aprovadas/publicadas em d0bdd68; a prova operacional foi executada pelo mantenedor com autorizações separadas. O agente não acessou o keyring real.
- Na etapa de especificação foram adicionados somente WindowsProfileProtectionSafetyTests e documentação/procedimento proposto. Onze casos para confirmação explícita, propósito fixo, destino temporário exclusivo, content root existente, não sobrescrever payload e não recuperar arquivo ausente; inspeção deve obter opções de ApiServices com AutoGenerateKeys false e sem sobrescritas do repositório/encryptor. Não confundir opções sem sobrescrita com criptografia desabilitada: os padrões Windows/DPAPI continuam sendo escolhidos pelo framework.
- Modos perfil-inspecionar/perfil-proteger/perfil-recuperar preservam seleção padrão do keyring/discriminador do contêiner. Manter geração automática desabilitada também na proteção; nunca criar, rotacionar por novas chaves, revogar, copiar, exportar ou apagar chaves reais nesse diagnóstico. Inspeção é só de opções, sem Protect/Unprotect/enumeração; operações criptográficas reais recusam Linux e exigem revisão operacional explícita.
- Os testes de segurança nunca fazem Protect/Unprotect no perfil real, nem mesmo quando executados no Windows. Recusas antecedem inicialização; modo ausente/código 2 não comprova recusa segura/código 6. O procedimento docs/operacao/prova-keyring-windows.md registra o contrato, a execução relatada e os limites; novas provas não são autorizadas automaticamente pelo GREEN. A confirmação literal é reconhecimento humano, não autenticação.
- Critérios de plataforma/links/criação exclusiva e comparação de metadados antes/depois devem ser revisados na implementação e na prova operacional; a especificação inicial não os certifica. Não apontar os modos antigos para o keyring real, pois proteção nesses modos pode gerar chaves. Estudo 12 em docs/estudos.
- Execução inicial da especificação no WSL com PostgreSQL isolado: 212 executados, 201 aprovados/11 falhos, zero ignorados (22 unitários aprovados; 179 integração aprovados/11 novos falhos). Novos casos paravam em modo desconhecido/código 2; não era RED funcional da API nem evidência de barreiras ativas. Mantenedor aprovou a especificação antes da implementação.
- GREEN implementado somente no auxiliar: WindowsProfileScenario valida confirmação/propósito/root/destino/estado do arquivo antes do host; perfil-proteger/recuperar recusam não Windows após as validações. ApiProbeFactory aceita keyring null para manter padrões e aplica somenteLeitura true em todos os modos de perfil. Inspeção lê opções sem Protect/Unprotect ou enumeração. Não há sobrescrita de repositório/encryptor/discriminador; nenhum método de criar/revogar/remover chaves. Criação exclusiva FileMode.CreateNew/FileShare.None; leitura limitada a 16 KiB, sem links. Erros sem detalhes privados.
- Onze testes preservados, SHA-256 0A072FEE71633307135FE9BCD563196919C5C46E110A7BC2755ECBF6D14EE88A. GREEN focado 11; suíte inteira WSL com PostgreSQL isolado: 212 aprovados (22 unitários + 190 integração), zero falhas/ignorados. Build sem erros/avisos; dotnet format dos quatro C# do recorte aprovado. Na etapa automática não houve execução criptográfica Windows ou alteração de produção, banco local, segredo ou chave real. Plataforma/links/races além das barreiras testadas foram inspecionados por código; não apresentar como cobertura automática completa. O diff e a operação foram aprovados separadamente pelo mantenedor depois dessa validação.
- Evidência posterior fornecida pelo mantenedor: inspeção Windows com geração false e seleção padrão preservada; nova pasta com payload ausente; duas chaves antes; proteção do texto fictício com processo 25756/hash igual ao da inspeção; recuperação em chamada separada relatada como Recuperado/código 0; duas chaves depois e zero diferenças em nomes, tamanhos e última escrita UTC. JSON completo da última recuperação e exit code da proteção não foram reenviados; não inventar esses valores. Houve tentativa preliminar DestinoJaExiste/recuperação de arquivo de origem não estabelecida, excluída da prova nova. Registro detalhado em docs/operacao/prova-keyring-windows.md.
- Gate de recuperação local do texto fictício atendido no contexto relatado, não certificação geral: metadados iguais não provam identidade bit a bit ou ausência absoluta de escrita. Não foram usados tokens reais, banco local ou chamadas ao Mercado Pago. Backup/restauração de chaves e identidade DPAPI, crash e mudança de máquina/usuário/root continuam gates antes de uso real. Arquivos temporários preservados; nenhuma limpeza autorizada nesta atualização. O registro é documental e não repete os 212 testes anteriores.

### Backup local — opção 1, caracterização com material fictício

- Após publicação de dbae6f7, o mantenedor escolheu limitar esta etapa ao mesmo Windows/usuário, preservando contexto DPAPI. Não inclui formatação/troca de computador/perda do perfil. Não alterar mecanismo de proteção nem exportar master keys do Windows para contornar esse limite.
- Novo caso em ApiDataProtectionProcessTests protege texto fictício pelo contêiner da API, copia todos os XMLs origem -> backup -> pasta restaurada e recupera em outro processo apontado para o restaurado. Confere conteúdo/quantidades nas três pastas sem publicar bytes; não altera os quatro casos anteriores nem implementação. Passou de primeira, caracterização sem RED funcional. Preparação corrigiu aviso de nulabilidade do seletor de nome, sem mudança em comportamento de produção.
- GREEN completo WSL com PostgreSQL isolado: 213 aprovados (22 unitários + 191 integração), zero falhas/ignorados; build sem avisos/erros e formatação do arquivo aprovado. Só chaves fictícias da fixture. Não valida DPAPI Windows, revogações/rotação históricas ou backup do perfil real. Nenhuma chave real copiada/restaurada; diff em revisão.
- Procedimento em docs/operacao/backup-keyring-mesmo-windows.md e estudo 13. Cópia real requer autorização específica, destino persistente fora de Git/temporário, ACL restrita e aceitação do alcance: keyring do perfil pode conter chaves de outros projetos. Preservar registros de revogação e chaves antigas; nunca restaurar sobre o original para testar. Verificação deve apontar api-recuperar ao restaurado; perfil-recuperar leria o original e não comprovaria o backup. Não usar modos de proteção com material real.
- Opção 1 ainda depende de prova operacional real; opção 2 (perda do contexto/máquina) permanece fora do escopo escolhido, não resolvida. Guardar essa limitação nos gates antes de produção/pagamentos reais. Não copiar segredos para WSL ou declarar desastre recuperável com evidência da fixture.

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
