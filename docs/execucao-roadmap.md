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
