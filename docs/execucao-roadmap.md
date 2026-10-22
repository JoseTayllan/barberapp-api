# Execução do roadmap

## Incremento 1 — disponibilidade e testes unitários

Estado: GREEN confirmado pela execução do mantenedor; diff aguardando revisão final.

O mantenedor autorizou a execução autônoma em 03/10/2026, preservando o aprendizado ativo e o TDD. Não houve commit, push, merge ou alteração de credenciais.

### Especificação

- A disponibilidade não admite início negativo, início igual/posterior ao fim ou fim além de 24h.
- Dias fora do enum são rejeitados.
- Intervalos válidos incluem meia-noite e encerramento em 24h.
- Atualização inválida preserva todos os valores anteriores e o timestamp.
- Atualização válida aplica horários/atividade e registra timestamp UTC.

### Evidência TDD

1. Baseline: 45 testes de integração aprovados.
2. Escrito apenas projeto/fixture unitária e 14 casos de disponibilidade.
3. RED executado: 10 falhas por ausência de exceção; 4 casos válidos aprovados.
4. Implementação mínima: validar enum e intervalo no construtor; validar antes de atribuir propriedades na atualização.
5. Build: seis projetos, zero erros/avisos.
6. A execução do agente encontrou bloqueio da DLL pelo Windows (`0x800711C7`), confirmado nos eventos CodeIntegrity. Nenhuma política de segurança foi alterada.
7. GREEN confirmado no terminal do mantenedor em 03/10/2026: `dotnet test BarberApp.slnx --no-restore`, 59 testes descobertos, 59 aprovados, zero falhas/ignorados. Essa execução valida a implementação sem alterar os testes.

### Diff e riscos

- `BarberApp.slnx`: adiciona suíte unitária.
- `BarberApp.UnitTests/`: dependências já usadas na suíte anterior e testes da entidade.
- `DisponibilidadeBarbeiro.cs`: validação centralizada antes da mutação.
- `CODEX.md` e `ROADMAP.md`: autorização, regra proposta e estado verdadeiro.
- `docs/estudos/`: material offline com exercícios/respostas.
- A linha em branco já modificada pelo mantenedor em `Program.cs` foi preservada.
- Não há migration nem nova rota. Dados inválidos já persistidos precisam de auditoria antes de endurecer a persistência.
- O status HTTP de entradas rejeitadas ainda precisa de teste de integração; o tratamento global de erros será etapa separada.
- Ainda faltam testes de services/validators, regras de agenda, PostgreSQL, erros HTTP, integração frontend e demais etapas. Este incremento não conclui o roadmap.

### Retomada

O mantenedor executou com sucesso o incremento de disponibilidade (59 casos). Posteriormente, ao compilar os novos testes OAuth, a DLL de integração foi bloqueada também no terminal do mantenedor. O sucesso anterior permanece válido para o estado anterior; não comprova que a DLL atual execute. O problema precisa de ambiente autorizado ou solução de assinatura/política validada pelo responsável pela máquina. Não alterar proteções como efeito colateral do roadmap.

### Ambiente WSL — 03/10/2026

Após autorização explícita, Ubuntu 24.04 LTS e SDK .NET 10 foram instalados no WSL. Testes executados como `barberdev`, em cópia separada sem artefatos Windows nem arquivos locais de segredos. Nenhuma proteção do Windows foi desativada. Procedimento: [ambiente-wsl.md](ambiente-wsl.md).

RED OAuth obtido no Linux: 15 casos executados, 14 falhas por ausência da rota e 1 aprovado. A implementação de OAuth permanece pendente; o bloqueio de carregamento deixou de impedir o ciclo TDD nesse ambiente.

### Esforço

Um agente principal, um ciclo de RED e implementação, baseline mais tentativas de execução para diagnosticar bloqueio de ambiente. Houve uma tentativa de comando inválida ao tentar rebuild pelo CLI de testes; corrigida com `dotnet build --no-incremental`. Redução de custo: resolver o carregamento antes de novos ciclos e usar o recorte unitário durante desenvolvimento.

## Incremento 2 — início OAuth Mercado Pago

Estado: GREEN técnico; diff aguardando aprovação humana.

- RED no WSL: 15 executados, 14 falhas esperadas por rota inexistente e 1 aprovado. Especificação mantida sem alterações na implementação.
- GREEN: `dotnet test BarberApp.slnx` no Ubuntu, 14 unitários + 60 integração aprovados, zero falhas/ignorados.
- Build: zero erros/avisos. `dotnet format --verify-no-changes --no-restore --include` dos cinco arquivos C# novos: exit code 0.
- Diff: cinco arquivos novos (controller, DTO, interface, exceção e serviço), três registros DI no Program e documentação. A linha em branco preexistente do mantenedor foi preservada; não foi feita formatação global.
- Revisão do código novo: host OAuth fixo, parâmetros escapados, geração criptográfica, PKCE S256, validação de role/identificador e configuração privada. Sem logs novos, credenciais reais ou requests ao provedor.
- Riscos: callback inexistente; contexto em memória perdido no reinício; uso único/expiração/concorrência do consumo ainda não testados. O mock permanece ativo. Não há migration ou integração frontend neste incremento.
- Um agente principal, um ciclo de implementação, suíte completa e build/lint focado. Sem agentes adicionais ou commit/push. Reduzir retrabalho mantendo incrementos pequenos e testes focados antes da suíte final.

## Incremento 3 — validação do callback (GREEN técnico)

- Escritos 21 casos novos antes da implementação. RED: 17 falhas por barreira de API key/ausência do contrato e 4 cenários de proteção aprovados.
- Implementação: exceção exata GET callback no middleware; processamento do retorno no serviço singleton com lock, indexação temporária state/Admin, expiração e remoção; respostas seguras no controller; proposta de OpenAPI sem exigência de headers.
- Resultado da solução no WSL: 14 unitários aprovados; integração 80 aprovados e 1 falha, total 95 executados, zero ignorados.
- Build da solução: zero erros/avisos. Verificação de formatação do recorte do callback: exit code 0. Isso não substitui o GREEN pendente.
- Falha restante: o serializador OpenAPI omite coleção security vazia, herdando API key global. Não remover teste: ajuste equivalente para requisito vazio `[{}]` aguarda aprovação do mantenedor.
- Corrigido aviso xUnit2031 usando o overload com predicado de Assert.Single. A expectativa de uma única resposta aceita foi preservada.
- Não houve tokens, chamadas externas, logs novos, migrações ou commit/push. Diferença de arquitetura: serviço scoped passou a singleton para compartilhar lock e consumo atômico local. Não resolve múltiplas instâncias.
- Um agente principal, RED, implementação e suíte final. Retrabalho restrito à representação Swagger; reduzir custo testando serialização no recorte antes de nova suíte completa.

### Ajuste OpenAPI aprovado — 03/10/2026

O mantenedor aprovou explicitamente a representação `security: [{}]` e a correção equivalente da asserção. O teste exige exatamente um requisito vazio no callback e mantém API key + Bearer na autorização; não foi removido nem relaxado para aceitar ausência de security.

Após o ajuste: 14 unitários e 81 integração aprovados, total 95, zero falhas/ignorados. Build: zero erros/avisos. Formatação dos dois arquivos ajustados: verificação aprovada, exit code 0. O diff desse ajuste contém uma linha de representação (mais comentário) e três linhas de asserção. Sem segredos ou logs novos, chamadas externas, commit ou push. Um agente principal e uma rodada de validação; o diff completo ainda precisa de revisão final.

## Incremento 4 — especificação da troca de tokens (05/10/2026)

Somente novo arquivo de testes e documentação; sem alterações de implementação ou dos testes anteriores.

- `MercadoPagoTokenExchangeTests`: 16 casos HTTP com handler externo simulado e valores inequivocamente fictícios.
- RED no WSL: 14 falhas esperadas e 2 aprovados, zero ignorados. A API ainda devolve `MercadoPagoTrocaTokenPendente`, em vez do sucesso ou das falhas externas especificadas.
- Testes exigem host fixo, POST JSON, parâmetros OAuth, verifier correspondente ao challenge, sandbox, resposta sem segredos, validação estrutural, falhas externas seguras e ausência de retry/replay.
- Opt-in proposto para preservar o contrato desabilitado: `MercadoPago__ConexaoEnabled`; ainda não implementado.
- Risco: o novo contrato `Conectado` exige persistência protegida. Ainda é preciso escrever testes específicos desse armazenamento e de falha de gravação antes de implementar o sucesso; estes casos HTTP sozinhos não provam proteção das credenciais.
- Material de aprendizado: `docs/estudos/04-troca-de-tokens-e-falhas.html`. Diff em revisão, não é entrega GREEN nem autorização de uso real.
- Um agente principal, uma execução RED e verificação separada do baseline. Economia: recorte focado para diagnóstico, sem múltiplos agentes ou tentativas de sandbox real.
- Baseline conferido no WSL com filtro `FullyQualifiedName!~MercadoPagoTokenExchangeTests`: os 95 testes anteriores passaram (14 unitários + 81 integração), zero falhas/ignorados. Essa execução exclui explicitamente os 16 novos casos; não significa que a suíte completa esteja verde. Formatação do novo arquivo: exit code 0. Inspeção do diff: somente valores fictícios nos testes, sem segredos reais ou implementação nova.

## Incremento 5 — troca de tokens e persistência protegida (05/10/2026)

Estado: implementação em GREEN técnico; diff aguardando revisão humana. Os 16 testes aprovados no incremento anterior e os 95 testes anteriores não tiveram suas expectativas alteradas.

### Evidência e verificações

- Armazenamento: quatro casos inicialmente RED, por ausência do sucesso e da gravação protegida. Cobrem ciphertext reversível somente com propósito correto, metadados, substituição da única conta, falha de gravação e falha criptográfica.
- Entidade: oito casos, inicialmente sete falhas de invariantes e um aprovado; validação adicionada antes de atribuição. Atualizar preserva o ID fixo da instalação.
- Revisão do driver: um novo teste reproduziu NpgsqlException escapando do tratamento; corrigido para `503 MercadoPagoPersistenciaFalhou`, sem detalhes internos nem replay. São cinco casos de armazenamento ao final.
- Correção da fixture nova: a factory anterior gerava nome de banco InMemory dentro da resolução de opções, produzindo bancos distintos por escopo. A fixture específica deste incremento usa nome estável por execução; as expectativas não foram relaxadas. Falha de fixture não foi contada como RED funcional.
- GREEN final no WSL com PostgreSQL: **125 aprovados, zero falhas, zero ignorados** (22 unitários + 103 integração). Sem a variável `BARBERAPP_TEST_POSTGRES`, o caso PostgreSQL é explicitamente ignorado; não anunciar a mesma cobertura nessa execução.
- Build sem erros/avisos; modelo EF sem alterações pendentes em relação à migration. Verificação final de formatação dos 16 arquivos C# do recorte aprovada, exit code 0. Não é lint global: preserva o baseline e a linha em branco do mantenedor em Program.cs. Diff check dos documentos versionados, projeto, contexto e snapshot também aprovado.
- PostgreSQL 16 no WSL, banco isolado `barberapp_test_mercadopago_20261005`, usuário não superuser via socket/peer. Migration aplicada apenas nesse banco, constraint verificada com SQL real e linhas de teste revertidas por transação. Schema mantido; banco da aplicação Windows não foi alterado.

### Diff para revisão: o que mudou e por quê

- Domain: entidade `ConexaoMercadoPago` e contrato do repositório; protege invariantes e representa uma conta recebedora por instalação.
- Application: processamento assíncrono do callback e códigos seguros de falha; não conhece HTTP nem recebe tokens na resposta pública.
- Infrastructure: cliente OAuth com host fixo, timeout/limite de resposta e redirects desabilitados; criptografia Data Protection antes de salvar; repositório EF e store singleton de tentativas. Serviço passa a scoped para usar o contexto de banco com vida útil correta.
- API/Program: aguarda a gravação antes de `200 Conectado`, traduz erros esperados e registra dependências. Fluxo desabilitado e barreiras API key/JWT anteriores permanecem.
- Projeto Infrastructure: FrameworkReference Microsoft.AspNetCore.App para Data Protection e IHttpClientFactory, sem novo pacote NuGet. Ajuste de dependência foi necessário para compilar, não constitui RED funcional.
- Schema: nova tabela, PK fixa e CHECK Id=1; migration e snapshot juntos. SQL revisável em `docs/sql/20261005172851_AddConexaoMercadoPago.sql`, sem aplicar ao banco do mantenedor. Script não idempotente: requer baseline correto.
- Testes: cinco de proteção/persistência, oito da entidade e um PostgreSQL; somados aos 16 de troca já aprovados. Provedor externo sempre simulado, nenhuma cobrança nem credencial real.
- Documentação: CODEX, README, ROADMAP, contrato de pagamentos, ambiente WSL e capítulo offline `docs/estudos/05-protecao-de-tokens-e-persistencia.html`, com exercícios e respostas. Capítulos anteriores identificam suas evidências históricas.

### Riscos e decisões pendentes

1. Antes de sandbox remoto, validar keyring persistente, permissões, proteção em disco e recuperação de backup. Provider efêmero dos testes não prova recuperação após reinício nem proteção em Linux. Não apagar chaves antigas.
2. Uma linha no banco não resolve qual reconexão concorrente deve prevalecer. Política de substituição e seu teste precisam preceder uso real.
3. Status da conexão, renovação, desconexão/revogação, checkout, webhooks e frontend não estão implementados; pagamento atual continua mock.
4. O Down da migration apaga a tabela e as credenciais. Não executar em dados reais sem backup, revisão e aprovação.
5. Não há logs novos de tokens nem segredos reais nos arquivos alterados; proxies/instrumentação precisam de redaction de query/body antes de produção. O arquivo local ignorado de configuração sensível não foi lido nem copiado ao WSL.

### Esforço e redução de custo

Um agente principal neste incremento, sem novos subagentes; três recortes RED funcionais, verificações da suíte completa, build, PostgreSQL/EF e formatação. Retrabalho concentrado na dependência de framework, isolamento da fixture e whitespace do código novo. Não há medição monetária/token precisa disponível; essa é uma estimativa operacional. Na próxima etapa: estabilizar a fixture antes de ampliar casos, usar o recorte afetado durante RED/GREEN, limitar revisão a uma rodada focada e reservar a suíte completa para o gate final. Sem commit, push, ativação real ou conclusão presumida do roadmap.

## Incremento 6 — especificação de status local (05/10/2026)

Incremento anterior aprovado pelo mantenedor, commit/push realizados por ele: `522e535`. Neste incremento foram escritos somente testes e documentação. Nenhuma alteração na implementação, schema ou testes anteriores.

- `MercadoPagoConnectionStatusTests`: 15 casos do endpoint e um de validação da fixture. API key, JWT, roles e identificador; ausência de conta; sandbox/produção como metadado; limite UTC antes/igual/depois; outro Admin; consulta sem HTTP externo/descriptografia; ausência de mutação; falha segura do driver e OpenAPI.
- Primeiro RED: 15 executados, 13 falhos e dois aprovados. Revisão endureceu a fixture: interceptação por tipo da entidade, não por texto da expressão; acrescentado teste que prova a falha seletiva e permite consulta de roles. RED final: 16 executados, 13 falhos, três aprovados, zero ignorados.
- Motivos corretos: 12 casos esperavam respostas do novo contrato e receberam 404; um esperava a rota no Swagger, ainda ausente. Os dois casos da API key já são atendidos pelo middleware global; o terceiro aprovado valida somente a fixture.
- Baseline com `FullyQualifiedName!~MercadoPagoConnectionStatusTests`: 125 anteriores aprovados (22 unitários + 103 integração), zero falhas/ignorados, PostgreSQL isolado incluído. Isso não significa que a suíte completa esteja GREEN.
- Formatação do único arquivo C# novo: `dotnet format ... --verify-no-changes --include ...`, exit code 0. Compilação dos testes sem novos avisos. Sem credenciais reais ou chamadas externas, sem mudança nos três caches preexistentes.
- Diff para revisar: um arquivo de testes, capítulo offline 06 e atualizações CODEX/README/ROADMAP/contrato/registro. README também corrigido para reconhecer o projeto unitário já existente. Nenhum arquivo de implementação modificado, nenhuma migration nem commit/push do agente.
- Risco semântico: Conectado informa expiração local, não aceitação remota, integridade do ciphertext ou pagamento pronto. ContaId é dado administrativo, exposto somente a Admin. A consulta não abre tokens; validação remota e renovação serão etapas distintas.
- Esforço estimado: um agente, duas execuções focadas RED, uma baseline e uma verificação de formatação. Refinamento restrito à fixture antes de congelar a especificação; nenhuma asserção de comportamento foi enfraquecida. Reduzir custo mantendo o recorte até GREEN e executando a suíte completa no gate final. A implementação aguarda aprovação destes testes.

### GREEN após aprovação da especificação

- Mantenedor aprovou os testes. Os 16 casos e sua fixture foram preservados integralmente durante a implementação.
- Diff de implementação: DTO StatusConexaoMercadoPagoResponse; record de metadados e interface de leitura no Domain; projeção AsNoTracking no repositório; comparação UTC/erro seguro no serviço; GET autenticado no controller e um registro DI no Program. A interface de gravação e seus consumidores não foram alterados.
- SELECT projetado não inclui TokensProtegidos/AdministradorId. Nenhum Unprotect, chamada HTTP, escrita, configuração sensível, dependência NuGet ou migration foi adicionado ao fluxo de consulta.
- GREEN focado: 16 aprovados. GREEN final no WSL com PostgreSQL isolado: **141 aprovados, zero falhas/ignorados** (22 unitários + 119 integração). Build sem avisos/erros; verificação de formatação dos oito arquivos C# do recorte exit code 0. Não foi feita formatação global nem alteração dos caches preexistentes.
- Revisão de diff: nenhum segredo ou log novo. Cancelamento propagado até SingleOrDefaultAsync, sem convertê-lo em ausência de conta. Erro esperado do driver vira 503 e não revela detalhes. Autorização por role e identificador, sem aceitar Admin no request. Swagger herda exigência ApiKey + Bearer, não a exceção do callback.
- Riscos: status não prova revogação remota, integridade do ciphertext ou prontidão para pagamentos. Teste PostgreSQL existente valida armazenamento/migration/constraint, não a nova projeção por HTTP contra PostgreSQL. Fluxo HTTP novo foi validado com InMemory; não confundir os escopos de cobertura. Recuperação de chaves, renovação/revogação, reconexão concorrente e checkout ainda pendentes.
- Esforço adicional: um agente, uma implementação, uma execução GREEN focada e uma verificação final de suíte/build/lint. Sem retrabalho funcional ou mudança nos testes aprovados. Continuar com recortes pequenos e leitura segregada para reduzir custo. Diff final aguarda aprovação; commit/push ficam com o mantenedor.

## Incremento 7 — especificação da renovação manual (19/10/2026)

Incremento anterior revisado pelo mantenedor e registrado em `86cba24`. Somente testes/documentação novos; nenhuma implementação, migration, dependência ou teste anterior modificado. Os três caches preexistentes foram preservados.

- `MercadoPagoTokenRenewalTests`: 28 casos HTTP. Autorização API key/JWT/Admin/identificador; rotação antes/depois da expiração; ausência de conexão; configuração/ambiente; payload protegido inválido; falhas HTTP/invalid_grant/timeout; resposta inválida ou conta trocada; falha de proteção/gravação sem declarar sucesso. Confere ambos os tokens protegidos e metadados preservados, resposta sem secrets e ausência de retry automático.
- RED funcional no WSL: 28 descobertos/executados, 26 falhos por 404 e dois aprovados pelo middleware global de API key, zero ignorados. Compilação dos testes sem erro ou novos avisos. Sem falhas de fixture/compilação usadas como evidência RED.
- Baseline separado com filtro `FullyQualifiedName!~MercadoPagoTokenRenewalTests`: 141 aprovados (22 unitários + 119 integração), zero falhas/ignorados, incluindo PostgreSQL isolado. Não é GREEN da suíte completa, que também contém os 28 novos casos.
- Formatação do novo arquivo: `dotnet format BarberApp.slnx --verify-no-changes --no-restore --include BarberApp.IntegrationTests/Payments/MercadoPagoTokenRenewalTests.cs`, exit code 0. Diff check dos documentos aprovado. Valores de teste inequivocamente falsos, sem segredos reais ou logs novos.
- Fixture específica por caso: nome InMemory estável entre escopos, relógio fixo, Data Protection efêmero e handler HTTP sem socket. Usa apenas entidades/interfaces já existentes, sem introduzir implementação para compilar os testes.
- Fontes oficiais consultadas em 19/10/2026: [renovação](https://www.mercadopago.com.br/developers/pt/docs/security/oauth/renewal) e [referência OAuth](https://www.mercadopago.com.br/developers/pt/reference/authentication/oauth/_oauth_token/post). O exemplo cURL de renovação usa grant_type refresh_token sem reapresentar code/verifier, apesar de redação ambígua no passo textual. Tokens recebidos devem ser rotacionados; offline_access depende do consentimento remoto, ainda não validado.
- Diff para revisar: arquivo de testes, capítulo offline 07, CODEX, README, ROADMAP, contrato e este registro. Nenhum arquivo de implementação novo ou modificado.
- Riscos/escopo: renovação manual apenas. Rotação remota e escrita local não são uma transação única; preservar o registro antigo não garante que as credenciais antigas sejam utilizáveis. Coordenação de renovação/reconexão/desconexão, recuperação de rotação sem persistência, renovação automática, revogação, scopes e sandbox remoto permanecem gates antes de dinheiro real. Os testes não certificam essas situações.
- Esforço estimado: um agente principal, uma execução RED, uma baseline e lint focado, sem retrabalho funcional ou subagentes. Sem medição monetária/token precisa. Redução de custo: manter esse recorte na fase GREEN e reservar suíte completa para validação final, sem sandbox real nem exploração ampla de checkout agora. Implementação aguarda aprovação dos testes; commit/push somente pelo mantenedor.

### GREEN após aprovação dos 28 testes

- Implementação autorizada pelo mantenedor; arquivo MercadoPagoTokenRenewalTests preservado. Hash SHA-256 antes/depois da verificação GREEN: `3634A9AE1FD57435E06F7BB0B575C35D82C197C8C7F7066404557F9097729355`. Nenhum teste anterior modificado.
- Diff: nova interface IRenovacaoMercadoPagoService e serviço scoped específico; interface de leitura protegida no Domain implementada no repositório EF com AsNoTracking; rota Admin com identificador/no-store/mapeamento de erros; dois registros DI; cliente OAuth com payloads distintos e transporte/validação compartilhados. Sem novas entidades persistidas, migrations, pacotes ou alterações de configuração sensível.
- GREEN focado: 28 aprovados, zero falhas/ignorados. Suíte completa no WSL, sem filtro, PostgreSQL isolado incluído: **169 aprovados, zero falhas/ignorados** (22 unitários + 147 integração). Build: zero erros/avisos. Lint/verificação sem alterações dos sete arquivos C# do recorte: exit code 0. Program foi revisado por diff (somente dois registros DI), sem formatação global.
- Revisão: conta de origem precisa coincidir com a resposta; ambiente precisa coincidir com o registro; novos tokens só chegam ao repositório protegidos. Interface de status continua selecionando apenas metadados, sem abrir ciphertext. Exceções esperadas não transportam body remoto/inner exception para o consumidor. Sem log novo, token em configuração ou chamada real. Cancelamento propagado; não há retry automático.
- Limites mantidos: este GREEN não trata concorrência de renovação/reconexão/desconexão, recuperação após rotação remota e falha de gravação, durabilidade/backup de chaves, scopes, renovação automática ou sandbox remoto. Não autoriza uso com dinheiro real. Testes HTTP novos com InMemory/handler falso; PostgreSQL valida persistência/migration/constraint anterior, não todo o fluxo HTTP de renovação.
- Esforço adicional: um agente principal, uma implementação, uma execução GREEN focada e uma suíte final com build/lint; sem retrabalho funcional. Para reduzir custo, manter o próximo recorte restrito a um desses riscos e testar focado antes da suíte final. Sem custo monetário/token medido. Diff final aguardando revisão; commit/push continuam com o mantenedor.

## Incremento 8 — especificação da concorrência local (19/10/2026)

Renovação manual aprovada pelo mantenedor e registrada em `9f760ae`. Neste incremento foram escritos somente testes/documentação; nenhum arquivo de implementação, migration ou teste anterior modificado. Três caches preexistentes preservados.

- Arquivo MercadoPagoConcurrencyTests: nove casos HTTP. Seis disputas entre renovação e callback, duas liberações após falha e um caso de disponibilidade de status/validação de callback inválido. Verifica ocupação até o final da escrita, ausência de segunda chamada externa, credencial atual após reconexão, state preservado quando rejeitado antes do envio do código e consumo único após aceito.
- Ordem explícita por TaskCompletionSource com RunContinuationsAsynchronously; handler HTTP e SaveChangesInterceptor simulados pausam a primeira operação. Guard de dez segundos apenas para diagnosticar deadlock; sem sleeps, espera arbitrária ou critério de desempenho. Finally libera sinal e aguarda a primeira requisição terminar.
- Primeira tentativa parou em compilação por using Microsoft.IdentityModel.Tokens ausente, corrigido sem mudar asserções. Essa tentativa não conta como RED funcional.
- RED funcional: nove descobertos/executados, oito falhas e um aprovado, zero ignorados. Sete concorrentes retornam 200 e um retorna 503 da falha simulada de gravação, em vez do 409 esperado. Demonstra ausência da exclusividade; não houve timeout ou erro de fixture contado como RED.
- Baseline separado com filtro `FullyQualifiedName!~MercadoPagoConcurrencyTests`: 169 aprovados (22 unitários + 147 integração), zero falhas/ignorados, PostgreSQL isolado incluído. A suíte completa continua RED; não ocultar o filtro nem somar resultados como se todos estivessem verdes.
- Lint: verificação do novo arquivo via dotnet format --verify-no-changes --no-restore --include, exit code 0. Diff check dos documentos aprovado; valores inequivocamente fictícios e sem logs novos/segredos reais. Nenhuma chamada externa, credencial real ou escrita no banco da aplicação.
- Diff para revisão: arquivo de testes, estudo offline 08, CODEX/README/ROADMAP/contrato e este relatório. Proposta pública adicional: 409 MercadoPagoOperacaoEmAndamento com no-store, sem fila ou retry automático; consumidores precisarão tratar o conflito. Aprovação dos testes pendente.
- Limites: exclusão numa instância/processo, não distribuída. Outro processo/escrita externa no banco, efeitos remotos após timeout/cancelamento, durabilidade de chaves, desconexão, recuperação de rotação sem persistência e dinheiro real continuam fora da cobertura. O caso de liberação após falha usa provedor falso que aceita a próxima requisição para comprovar disponibilidade local, não recuperação remota.
- Esforço estimado: um agente principal; uma tentativa de compilação corrigida, uma execução RED funcional e uma baseline com lint focado. Sem novos subagentes ou medição monetária/token. Redução de custo: validar imports ao preparar fixture, manter sinais reutilizáveis e focar este arquivo até GREEN, deixando a suíte completa para o gate final. Sem implementação, commit ou push; próximos passos dependem da aprovação humana.

### GREEN após aprovação dos nove testes

- Mantenedor aprovou a especificação. SHA-256 do arquivo MercadoPagoConcurrencyTests antes/depois da implementação: `F5E99B564A3FD62E45C347DEF0D886601FA05DBAC23B7DBB0DF543F9DDB2900D`. Asserções/fixture e testes anteriores preservados.
- Diff de implementação: novo CoordenadorOperacoesMercadoPago singleton com flag atômica e escopo IDisposable idempotente; um registro DI; uso por renovação e callback até terminar a persistência; consulta não consumidora de tentativa antes da admissão; mapeamento do código de ocupação para 409 em ambas as ações. Sem migration, entidade, dependência NuGet, configuração sensível ou mudança na API key/JWT.
- GREEN focado: nove aprovados, zero falhas/ignorados. Suíte completa no WSL, sem filtro, PostgreSQL isolado incluído: **178 aprovados, zero falhas/ignorados** (22 unitários + 156 integração). Build sem erros/avisos; lint dos seis arquivos C# do recorte exit code 0. Program revisado por diff, somente um registro singleton, sem formatação global. Diff check do código versionado aprovado.
- Revisão: nenhum segredo/log novo. Singleton guarda apenas sinal de ocupação; não captura DbContext, service scoped ou credenciais. Renovação lê o registro depois de entrar, evitando snapshot antigo; callback só consome state depois da admissão e revalida sob lock. Using libera em sucesso/falha de I/O; Dispose idempotente não libera uma operação posterior por descarte duplicado. Estado inválido, recusa e conexão desabilitada preservam contratos anteriores; status não passa pelo coordenador.
- Escopo de cobertura: provas HTTP locais com InMemory/provedor simulado. PostgreSQL da suíte cobre migration/persistência/constraint anterior, não exclusão entre processos ou esse novo fluxo HTTP no provedor relacional. Teste de liberação após erro não certifica validade remota das credenciais preservadas.
- Pendências antes de uso real: resultados remotos incertos após timeout/cancelamento, recuperação de rotação sem persistência, durabilidade/backup de chaves, coordenação entre instâncias, revogação e jornadas checkout/webhook/frontend. Sem pagamentos reais, commit ou push. Diff final aguarda aprovação humana.
- Esforço adicional: um agente principal, uma implementação, uma execução GREEN focada e uma validação completa com build/lint; sem retrabalho funcional. Sem valor monetário/token medido. Próximo recorte deve isolar um risco residual, reutilizando a fixture/sinais já validados e mantendo a suíte completa no gate final para reduzir custo.

## Incremento 9 — especificação da recuperação durável (20/10/2026)

Concorrência local aprovada e registrada pelo mantenedor em `0d485a8`. Antes de escrever código, foram apontados o status baseado só em expiração e o teste que permitia usar o refresh antigo após falha. O mantenedor aprovou a evolução do requisito e a revisão explícita desses testes. Nesta fase não houve implementação, migration ou mudança nos caches preexistentes.

### Diff autorizado e critérios novos

- Novo MercadoPagoRecoveryTests: 12 casos HTTP, incluindo registro antes do envio, falha do registro sem chamada externa, cinco falhas posteriores, sucesso e quatro caminhos de novo OAuth. Fixture compartilha somente armazenamento InMemory/keys de teste entre hosts novos; handlers e serviços são recriados. Não usa conta ou rede real.
- MercadoPagoConcurrencyTests: dois casos de falha agora exigem 409 MercadoPagoReautorizacaoNecessaria sem segunda chamada, depois aceitam callback de recuperação para provar que a exclusividade não ficou presa. Os demais casos/asserções permanecem; interceptor passa a pausar/falhar na modificação de TokensProtegidos, não no registro da pendência.
- MercadoPagoTokenRenewalTests: somente interceptor refinado para falhar na substituição do ciphertext, mantendo as expectativas originais de 503, uma emissão remota e preservação do registro. O novo teste de falha prévia exige zero emissões, separando as fases. Não foram removidas asserções para produzir GREEN.
- MercadoPagoPostgresTests: novo caso exige marcador RequerReautorizacao bool não nulo, default false compatível com INSERT legado, atualização e leitura por outro contexto na mesma transação. Usa o banco WSL isolado com prefixo/host verificados e rollback de dados. Atualmente para no marcador ausente do modelo, antes de migrar/testar o schema novo.
- Proposta conservadora: marcar antes de enviar; se falhar, 503 MercadoPagoPendenciaNaoRegistrada, sem envio. Enquanto marcada, status ReautorizacaoNecessaria, inclusive temporariamente durante a própria operação. Sucesso com tokens novos salvos limpa a marca; falha não. Nova autorização persistida recupera; recusa ou nova falha mantém bloqueio. Preservar os quatro campos públicos e adaptar consumidores para o status adicional.

### Evidência e revisão

- Primeiro recorte: 14 executados (12 novos HTTP e dois casos evoluídos), 13 falhos e um aprovado, zero ignorados. Motivos: status ainda Conectado, renovação ainda aceita após falha e refresh enviado antes de falhar a escrita.
- Suíte completa final no WSL com BARBERAPP_TEST_POSTGRES configurado: **191 executados, 177 aprovados, 14 falhas esperadas, zero ignorados**. Unitários: 22 aprovados. Integração: 155 aprovados e 14 falhos. Não foi usado filtro para esconder falhas. Os 176 casos anteriores fora da evolução passaram; um controle novo de sucesso também passou. O caso relacional adicional falha por ausência do marcador no modelo, não por erro de ambiente.
- Compilação dos testes sem erro/novos avisos. Dotnet format --verify-no-changes --no-restore --include nos quatro arquivos C# de testes: exit code 0. Nenhuma correção automática, mudança em implementação ou migration nova. Diff revisado por sigilo: valores exclusivamente fictícios, sem logs ou credenciais reais.
- Documentação: CODEX/README/ROADMAP/contrato/registro e capítulo offline 09 com exercícios e respostas. Diff dos testes antigos é explícito e decorre da evolução aprovada, não de tentativa de acomodar uma implementação existente.

### Limites e esforço

Teste de novo host em memória não prova crash de sistema operacional, fsync ou recuperação de keyring. O novo caso relacional ainda precisa chegar ao GREEN com uma migration revisada; sua leitura entre contextos usa a mesma transação. Marker default false mantém compatibilidade estrutural, não prova validade remota de dados legados. Down pode perder informação de incerteza e reativar credenciais; requer procedimento explícito. Admissão entre processos, efeitos remotos incertos, backup das chaves e fluxo completo de pagamentos continuam gates antes de uso real.

Um agente principal; uma análise do conflito de requisitos, duas execuções funcionais (recorte RED e suíte completa) e uma verificação de formatação. Sem subagentes, custo monetário/token medido, commit ou push. Para reduzir custo, usar este recorte durante GREEN e reservar suíte completa para o gate final; manter a fixture compartilhada de armazenamento e a falha por etapa. Aprovação desta especificação pendente antes de implementar.

### GREEN após aprovação da especificação de recuperação

- Mantenedor aprovou os testes. Durante a implementação, os quatro arquivos foram preservados; SHA-256 conferidos antes/depois: RecoveryTests `6DEDBC753EF245AE56E5A3BEE15D9FCE030155819449D33B2D43C990151CC124`; PostgresTests `582ED2FCFD09FBE33ABCB29DE55E340D7EA50E5DB4D14DF156A3D1DB0A3662A0`; ConcurrencyTests `F5F42DF430D21F696E9BAC4DD6C25027ABB6C9E19157920861FF10654FCEBD3E`; TokenRenewalTests `B98CDD3B96C157300FDAED84CEC45DDDD13945B3DB0CF0E87B6BF8F518CD3961`.
- Diff: bool privado na entidade e transição para pendência; campo adicional na projeção interna de metadados; interface de registro persistido e implementação scoped no repositório; guarda de reautorização e SaveChanges prévio no serviço de renovação; status prioriza incerteza; um registro DI e configuração EF/default. Nenhuma alteração no DTO público ou no transporte HTTP do provedor.
- Sucesso da renovação/callback grava nova entidade protegida com flag false por Atualizar/SaveChangesAsync, mantendo limpeza e tokens no mesmo UPDATE. Falhas posteriores não executam limpeza; dados anteriores permanecem marcados. Se o registro prévio falhar, não há envio. Status continua sem selecionar ciphertext.
- GREEN focado: 14 casos HTTP aprovados (12 novos + dois evoluídos). Após SQL revisado e migration incorporada, suíte completa sem filtro com PostgreSQL isolado: **191 aprovados, zero falhas/ignorados** (22 unitários + 169 integração). Build sem erros/avisos; lint dos 11 arquivos C# do recorte exit code 0; EF has-pending-model-changes confirma ausência de mudanças pendentes. Program/AppDbContext/snapshot revisados por diff sem formatação global.
- Migration gerada pelo EF 10.0.5 na cópia WSL: `20261020213112_AddEstadoRecuperacaoMercadoPago`, designer e cinco linhas de snapshot; incorporados ao workspace por apply_patch. SQL em docs/sql/20261020213112_AddEstadoRecuperacaoMercadoPago.sql: apenas ADD boolean NOT NULL DEFAULT FALSE e histórico EF. Aplicado somente por testes ao banco WSL isolado. O caso relacional confirmou default/INSERT legado e persistência entre contextos; dados revertidos, schema de teste mantido. Nenhuma migration no banco da aplicação.
- Revisão de segurança: sem segredo/log novo; configuração local ignorada não lida nem sincronizada. Marker não autentica usuários, não substitui API key/JWT e não comprova revogação remota. Não se faz retry automático. Sem mudanças nos caches preexistentes, commit/push ou cobrança.
- Limites: host recriado InMemory não prova crash real/backup de keyring. O teste PostgreSQL lê entre contextos dentro da mesma transação, não simula falha de máquina. Admissão entre réplicas ainda não é atômica/distribuída; não executar múltiplas instâncias como se houvesse essa garantia. Down remove informação de incerteza e pode permitir reuso indevido por versão antiga; exige procedimento aprovado. Consumidores com enum fechado precisam reconhecer ReautorizacaoNecessaria.
- Esforço adicional: um agente principal, uma implementação, GREEN focado e suíte final com build/lint/modelo. EF gerou a migration, mas o comando de inspeção parou porque rg não existe no WSL; usado find para ler os arquivos já gerados, sem gerar migration duplicada ou instalar ferramenta. Custo monetário/token não medido. Para reduzir uso, manter migrations em um fluxo único de geração/revisão e executar o recorte antes da suíte completa. Diff final aguardando revisão humana.

## 10. Caracterização do keyring entre processos — 22/10/2026

### Evidência do mantenedor anterior ao código

- Push confirmado de 1c5960e. Depois de pg_dump com código 0 e leitura do catálogo do backup, o mantenedor aplicou os dois SQL Mercado Pago com ON_ERROR_STOP, COMMIT e saída 0. Consultou oito migrations e schema com bool NOT NULL DEFAULT false. Esses comandos foram executados por ele, não pelo agente.
- Configurou API key local por variável e depois inicializou User Secrets. Valor enviado como JSON por stdin ao CLI; variável removida; API iniciou em Development e status autenticado retornou 200 NaoConectado. Novo UserSecretsId no csproj é edição preexistente dele; preservada, junto dos três caches. O marcador aparentemente alterado na primeira linha do csproj é BOM introduzido pelo comando de inicialização, sem alteração funcional.
- Mantenedor informou existência de dois arquivos de chaves padrão, ambos encryptedSecret com DPAPI, e ACL da pasta para usuário/SYSTEM/administradores. Nenhum XML real ou segredo foi enviado ao agente. Não declarar recuperação real validada por essa evidência parcial.

### Escopo e diff para revisão

- Seis fatos em DataProtectionProcessTests. Novo console BarberApp.TestSupport.DataProtectionProbe, exclusivo de testes, com FrameworkReference Microsoft.AspNetCore.App e sem dependências de produção/NuGet novas. Registrado na solução e ProjectReference ReferenceOutputAssembly=false; target pós-build copia artefatos de runtime para output/ProtectionProbe. Não copiar configuração local nem segredos.
- Primeiro filho gera chaves isoladas e protege texto fictício em arquivo; sai antes do segundo filho recuperar. stdout contém só indicadores fixos, códigos separam rejeição criptográfica de falha de infraestrutura. Teste não aceita falha de processo como prova de rejeição: espera código 3 e REJEITADO. Geração automática desabilitada na leitura. Casos: mesmas chaves/app/propósito; propósito diferente; aplicação diferente; pasta vazia sem criar substitutas; cópia no mesmo contexto; ciphertext adulterado em byte autenticado.
- Processos não herdam credenciais da sessão: ambiente reduzido a variáveis de execução. Argumentos contêm só caminhos de teste, modo e identificadores fictícios. Timeout de 20 segundos é proteção contra travamento; mata/aguarda apenas o processo filho criado pelo teste. Pastas exclusivas em temporário; limpeza valida raiz antes de apagar apenas esse recorte, sem ler/apagar keyring do usuário.
- O helper é infraestrutura de verificação, não nova implementação da API. Não cria endpoint, política de migração de chaves ou configuração de produção. Testes de caracterização passaram desde a primeira execução: não houve RED funcional e não se fabricou falha. Mudanças futuras de comportamento na aplicação permanecem sujeitas a TDD.
- CODEX/README/ROADMAP/contrato atualizados e capítulo offline 10 com seis exercícios/respostas. Evidências antigas de banco inalterado continuam históricas; seção atual registra aplicação posterior pelo mantenedor. Nenhuma atualização do banco local pelo agente, conta real, cobrança, log de segredo, staging, commit ou push.

### Execuções e limites

- Sincronização WSL sem .git/bin/obj/caches/arquivos de segredos conhecidos. Recorte: seis aprovados, zero falhas/ignorados, na primeira execução. Suíte completa com BARBERAPP_TEST_POSTGRES no banco isolado WSL: 197 aprovados (22 unitários + 175 integração), zero falhas/ignorados. Compilação sem erros/avisos; dotnet format --verify-no-changes --no-restore --include dos dois novos C# exit 0. Git diff --check exit 0. Não é lint global.
- WSL usa keyring fictício em disco sem DPAPI; o branch Windows do helper não foi executado pelo agente. Probe usa configuração explícita de aplicação e diretório, não composition root/discriminador efetivo do BarberApp. Não é teste end-to-end de recuperação da API nem prova de crash/disco/keyring real/backup entre máquinas. ACL de cada arquivo e backup aprovado da identidade/chaves Windows continuam pendentes. Não habilitar pagamentos reais com esta prova parcial.
- Esforço: um agente principal, sem subagentes, uma execução focada e uma execução completa com lint; nenhuma rodada de correção de C# ou relaxamento de testes. Custos monetários/tokens não medidos. Para reduzir uso, manter este recorte rápido e evitar copiar/restaurar chaves reais ou iniciar a API/banco em testes de componente. Diff aguarda aprovação humana antes de próximo incremento.

## 11. Contrato do diagnóstico pelo provider da API — testes em revisão (22/10/2026)

### Evidência de ambiente fornecida pelo mantenedor

Após aprovar o diff anterior, o mantenedor iniciou a API com log informativo temporário de Microsoft.AspNetCore.DataProtection. O XmlKeyManager informou uso do repositório padrão no perfil Windows e DPAPI para proteção em disco. Isso confirma a pasta usada nessa execução. Não revela/disponibiliza chaves ao agente e não é evidência de round-trip após reinício. Orientada remoção da variável temporária de logging; não foi alterado arquivo de configuração.

### Somente especificação, sem implementação

- Criado ApiDataProtectionProcessTests com quatro fatos: recuperar em outro processo com mesmo content root/keyring/propósito; rejeitar outro root (isolamento padrão atual); rejeitar outro propósito; rejeitar keyring vazio sem geração automática na leitura. O wrapper não reutiliza nem enfraquece os seis testes anteriores.
- Contrato do futuro auxiliar: api-proteger/api-recuperar com caminhos temporários e propósito. Resolver IDataProtectionProvider de ApiServices após inicialização do Program.cs. Nunca substituir por provider efêmero ou SetApplicationName de teste. Futuro diagnóstico deve isolar banco/configuração/rede; nenhum endpoint de diagnóstico ou mudança de produção está autorizado por esta especificação.
- JSON estrito de origem/status/processoId/identidadeHash. ID deve coincidir com processo lançado; hash SHA-256 do discriminador, não de chave, deve permanecer igual no mesmo root e mudar para outro. Código 0 significa operação bem-sucedida; 3 com Rejeitado é falha criptográfica esperada; 2 é modo não suportado e 5 é infraestrutura, nunca aceitos como rejeição.
- Ambiente do filho sem herança de credenciais; argumentos sem valores reais, stdout capturado sem publicação do payload. Chaves/payload fictícios em pasta temporária exclusiva; limpeza valida limite antes de remover. Processo A termina antes de lançar B; timeout de 30 segundos apenas diagnóstico.
- Nenhuma edição no executável auxiliar, csproj, Program.cs, configuração privada, banco local ou chaves Windows. Preservados os três caches preexistentes. Documentação atualizada em CODEX/README/ROADMAP/contrato e estudo offline 11 com exercícios/respostas.

### Resultados observados e limites

- Compilação sem erros/avisos. Recorte no WSL: quatro executados/quatro falhos/zero ignorados. Todos falham no primeiro api-proteger: esperado exit 0, recebido 2. A ausência é do modo do auxiliar, ainda não implementado; nenhum teste chegou à recuperação ou rejeição criptográfica. Não chamar esse resultado de RED funcional da API nem apresentar como bug no framework. É o contrato pendente de uma ferramenta de diagnóstico.
- Suíte inteira com BARBERAPP_TEST_POSTGRES do banco isolado WSL: 201 executados, 197 aprovados/quatro falhos, zero ignorados (22 unitários aprovados, 175 integração aprovados e quatro novos falhos). Dotnet format --verify-no-changes --no-restore --include do novo C# exit 0. Nenhum teste anterior modificado, sem descarte de teste/alteração de asserções após execução.
- Mesmo após futuro GREEN no WSL, teste usa keyring/content root fictícios. Windows/DPAPI com as chaves reais e o mesmo contexto da instalação requer etapa operacional explícita, geração automática desabilitada e nenhuma cópia/rotação/remoção de chaves. Não há autorização para cobranças, backup/restauração reais, mudança do discriminador ou leitura de User Secrets.
- Esforço: um agente, uma rodada de especificação, uma execução focada, uma completa e uma verificação de formatação; sem implementação, correções iterativas ou custo monetário/token medido. Reduzir custo mantendo o recorte do diagnóstico e evitando repetir toda a exploração. Pausa obrigatória para revisão dos quatro testes antes de implementar o modo; não realizar commit/push da especificação vermelha como entrega concluída.

### GREEN do auxiliar após aprovação humana

- Mantenedor aprovou a especificação. ApiDataProtectionProcessTests preservado integralmente: SHA-256 antes/depois 94FF424876D60C76E5106E8C1D2E4D1C5795945172CAB0605EA43BF305C94E77. Nenhuma asserção, fixture ou teste anterior alterado durante a implementação.
- ApiProbeFactory novo no auxiliar referencia WebApplicationFactory com marcador ServicosController, iniciando o entry point real. Ambiente ProtectionProbe e content root temporário; fontes de configuração substituídas por valores fictícios; logs removidos; bootstrap/conexão desabilitados; DbContext substituído por InMemory. AddDataProtection mantém provider/discriminador registrados pela API e isola somente diretório/chaves. Windows solicita DPAPI, leitura desabilita geração automática. Não usa SetApplicationName nem provider efêmero.
- ApiProtectionScenario novo resolve IDataProtectionProvider e IOptions<DataProtectionOptions> de factory.Services, calcula hash do discriminador e protege/recupera texto fictício. Só Unprotect pode produzir Rejeitado/código 3; falha de inicialização passa pelo código 5 sem detalhes internos. Modos antigos do Program do auxiliar preservados. Nenhuma edição no Program.cs/API de produção, domínio, serviços de pagamento, banco, configurações privadas ou chaves reais.
- Auxiliar adiciona referência à API e Mvc.Testing/InMemory 10.0.5, já presentes no repositório. Nenhuma versão nova de pacote de produção. Target do projeto de integração restringido a DLLs/deps/runtimeconfig/manifest para não copiar appsettings ao runtime do probe. Revisão de segurança não encontrou valores reais/logs novos de segredo; a configuração local ignorada não foi lida ou sincronizada. Filtro não limpa arquivos antigos; não prometer essa limpeza.
- Primeiro GREEN: quatro casos novos aprovados sem alteração. Suíte Debug com PostgreSQL isolado: 201 aprovados, zero falhas/ignorados. Build sem erros/avisos e formatação do recorte aprovada.
- Após restringir o target, tentativa de criar output novo por dotnet test BarberApp.slnx -c ProbeVerification foi rejeitada com MSB4126, pois a solução aceita apenas Debug/Release. Erro de comando/ambiente, não RED. Sem alterar solução/testes para contornar: executados diretamente os dois csproj de testes com -c ProbeVerification, configuração nova aceita por projetos. Resultado final: 179 integração + 22 unitários = 201 aprovados, zero falhas/ignorados, PostgreSQL isolado configurado. Conferências de ausência de appsettings.json e appsettings.Development.json na nova pasta ProtectionProbe aprovadas. Build final da solução Debug sem avisos/erros; dotnet format dos quatro C# do recorte exit 0. Não é lint global, nem execução Windows.
- Limites: provider vem do contêiner real, mas content root/configuração/keyring/banco são substituições de teste. Isso não valida o perfil Windows, as chaves reais, conteúdo de User Secrets, permissões dos arquivos individuais, backup ou recuperação de crash. Os modos atuais não devem ser usados contra o perfil real: geração na proteção ainda pode criar chaves, e uma prova operacional futura exige contrato seguro explícito. Nenhum endpoint público ou cobrança. CODEX/README/ROADMAP/contrato/estudo 11 atualizados; diff final aguarda revisão humana antes de commit/push pelo mantenedor.
- Esforço adicional: um agente principal, sem subagentes; uma implementação, um GREEN focado e duas verificações completas (Debug e output novo). Um retrabalho de comando por configuração não suportada pela solução; testes não modificados. Custo monetário/token não medido. Reduzir uso validando configurações suportadas antes de iniciar verificação limpa e executando o recorte durante desenvolvimento, com suíte completa no fechamento.
