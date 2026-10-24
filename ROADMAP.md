# Roadmap — BarberApp API

Este documento organiza a evolução do backend até um MVP seguro, testado, utilizável pelo frontend e preparado para deploy. Ele deve ser atualizado sempre que prioridade, escopo ou estado de uma etapa mudar.

O roadmap não substitui o `CODEX.md`: o `CODEX.md` define como trabalhamos; este arquivo define o que ainda construiremos.

## Método de trabalho

O projeto tem aprendizado ativo como prioridade. Em cada etapa:

1. Definimos juntos o comportamento e os critérios de aceitação.
2. O agente explica os conceitos e divide o trabalho em seções pequenas.
3. O mantenedor escreve os testes e o código.
4. Os agentes revisam o diff e executam build, testes e lint.
5. Se houver falha, os agentes explicam a causa e orientam; o mantenedor corrige.
6. Antes da conclusão, o mantenedor revisa e aprova o diff e os riscos.

Nenhum agente escreve ou corrige código sem autorização explícita e pontual.

## Legenda

- `[ ]` Não iniciado
- `[~]` Em andamento
- `[x]` Concluído e aprovado
- `[!]` Bloqueado por risco ou decisão

## Estado atual — 26/09/2026

### Execução autônoma autorizada — 03/10/2026

- A autorização de implementação cobre o roadmap, mantendo TDD e materiais de aprendizado por assunto. Não pressupõe aprovação final dos diffs.
- Alvo de entrega: MVP local. Hospedagem ainda indefinida; preparar infraestrutura reproduzível sem publicar.
- Frontend consultado em `C:\Users\joset\www\barberapp-web`, somente para leitura. Jornadas existentes: cliente (cadastro/login/perfil/reservas/pagamento), barbeiro (agenda/expediente) e admin (serviços/profissionais/clientes/agendamentos/indicadores).
- O cliente HTTP atual do frontend não envia `X-API-Key`. A integração precisa preservar a chave em componente servidor, sem expô-la em variável pública ou navegador. Mudanças naquele repositório exigem escopo próprio.
- Pagamento real solicitado; provedor ainda não informado. Manter o mock claramente identificado até especificar e testar a integração escolhida.
- Refresh token, recuperação de senha, confirmação de email e invalidação no servidor ainda precisam de decisão; o alvo local não decide esses recursos automaticamente.
- Primeiro incremento: projeto unitário e invariantes de disponibilidade. RED válido: 14 casos, 10 falhas esperadas e 4 aprovados. Correção escrita depois do RED; build sem avisos/erros. GREEN bloqueado por Smart App Control (`0x800711C7`) ao carregar `BarberApp.Domain.dll`, confirmado nos eventos do Windows mesmo após recompilação fora do sandbox.
- A suíte de integração passou com 45 testes antes da alteração. Após a alteração, as falhas de carregamento não demonstram regressão funcional nem comprovam sucesso.
- Atualização de 03/10/2026: mantenedor executou a solução no próprio terminal e confirmou GREEN com 59 testes aprovados, zero falhas/ignorados. O bloqueio observado pelo agente permanece como limitação daquele ambiente.
- Relatório e estudo em `docs/execucao-roadmap.md` e `docs/estudos/01-disponibilidade-e-tdd.html`.

- A API está dividida em `Domain`, `Application`, `Infrastructure` e `API`.
- PostgreSQL, EF Core, Identity, JWT e Swagger estão integrados.
- A autenticação global por API key está implementada em uma branch de feature.
- A suíte de integração cobre API key, compatibilidade JWT, OpenAPI, configuração segura e bootstrap administrativo.
- A connection string sensível e a senha administrativa fixa foram removidas do estado atual dos arquivos rastreados; rotação dos valores expostos, tratamento do histórico e migração/validação da configuração local continuam pendentes.
- O frontend está parcialmente iniciado e ainda precisa orientar o fechamento do contrato do MVP.

## Etapa 0 — concluir a feature de API key com segurança

**Estado:** `[~] Em andamento`

**Objetivo:** remover segredos do código versionado, compreender integralmente a feature de API key e deixar a branch pronta para revisão e merge.

### Aprendizado

- Pipeline de middlewares do ASP.NET Core.
- Configuração do .NET e precedência das fontes.
- Variáveis de ambiente e Secret Manager.
- Diferença entre API key, JWT, autenticação e autorização.
- Rotação de credenciais expostas.
- Revisão segura de diff e histórico Git.

### Checklist

- `[x]` Testes da API key escritos e executados.
- `[x]` Middleware global implementado.
- `[x]` Contrato OpenAPI atualizado.
- `[x]` Documentação e exemplos atualizados.
- `[~]` Estudar o middleware e sua ligação no `Program.cs`.
- `[x]` Remover a connection string sensível da configuração versionada.
- `[x]` Remover a senha fixa do administrador do código.
- `[x]` Implementar bootstrap administrativo opt-in, fail-fast, idempotente e sem promoção automática.
- `[ ]` Migrar e validar os segredos locais/de deploy em variável de ambiente, Secret Manager ou cofre.
- `[ ]` Rotacionar todas as credenciais já expostas.
- `[ ]` Definir e executar o tratamento apropriado dos segredos no histórico Git.
- `[ ]` Garantir que arquivos locais sensíveis estejam ignorados pelo Git.
- `[ ]` Remover do diff arquivos de cache gerados por ferramentas.
- `[x]` Executar build e toda a suíte descoberta no estado atual.
- `[ ]` Corrigir o baseline e fazer `dotnet format --verify-no-changes` passar.
- `[ ]` Fazer varredura final de segredos no diff e nos logs.
- `[ ]` Apresentar diff e riscos para aprovação do mantenedor.
- `[ ]` Adicionar commit corretivo à branch.
- `[ ]` Fazer merge somente após aprovação explícita.

### Critério de conclusão

Nenhum segredo permanece em código/configuração versionada; credenciais expostas foram rotacionadas; testes passam; diff foi revisado e aprovado.

## Etapa 1 — ampliar a estratégia de testes

**Estado:** `[ ] Não iniciado`

**Objetivo:** cobrir as regras centrais da barbearia, não apenas a infraestrutura de autenticação.

### Escopo

- Criar projeto de testes unitários.
- Testar entidades e transições de estado.
- Testar services com dependências isoladas.
- Testar validators.
- Ampliar integração para registro, login, roles, agenda e pagamentos.
- Adicionar integração com PostgreSQL real para comportamentos de persistência.

### Aprendizado

- xUnit e Arrange–Act–Assert.
- Testes unitários versus integração.
- Fakes, mocks, fixtures e isolamento.
- Testes determinísticos e testes de regressão.

### Critério de conclusão

Regras críticas possuem testes de sucesso, falha e limites; a saída confirma todos os testes descobertos e executados.

## Etapa 2 — corrigir regras e bugs do domínio

**Estado:** `[ ] Não iniciado`

**Objetivo:** transformar suspeitas observadas na auditoria em testes reproduzíveis e correções pequenas.

### Riscos a investigar com testes

- Escopo de autorização no cancelamento de agendamentos por barbeiro.
- Consulta usada na atualização de disponibilidade.
- Duração usada para detectar sobreposição entre serviços diferentes.
- Uso de horário local em dados persistidos.
- Compatibilidade do identificador de fuso entre Windows e Linux.
- Consistência entre criação de usuário Identity e perfil de domínio.
- Nomes incorretos em entidades/contratos e impacto de compatibilidade.

### Critério de conclusão

Cada bug confirmado possui teste de regressão; correções passam sem alterar contratos de forma acidental.

## Etapa 3 — padronizar o tratamento de erros

**Estado:** `[ ] Não iniciado`

**Objetivo:** substituir `try/catch` repetido e exceptions genéricas por um contrato previsível.

### Escopo

- Exceções específicas de domínio/aplicação.
- Middleware global de erros.
- Respostas `ProblemDetails`.
- Mapeamento consistente para `400`, `401`, `403`, `404`, `409` e `500`.
- Testes do contrato de erro e proteção contra vazamento de detalhes internos.

### Critério de conclusão

Controllers não repetem tratamento genérico; erros esperados têm status e corpo estáveis; falhas internas não expõem stack trace ou segredo.

## Etapa 4 — completar os casos de uso exigidos pelo frontend

**Estado:** `[ ] Não iniciado`

**Objetivo:** implementar somente endpoints necessários às telas e jornadas reais.

### Possíveis lacunas a validar com o frontend

- Atualizar/desativar serviço.
- Atualizar/desativar barbeiro.
- Atualizar disponibilidade.
- Atualizar exceção de agenda.
- Concluir agendamento.
- Consultar status de pagamento.
- Paginar e filtrar agendamentos.
- Operações administrativas necessárias.

### Critério de conclusão

Cada tela do MVP possui os contratos necessários, com autorização e testes; nenhum CRUD foi criado sem caso de uso.

## Etapa 5 — garantir consistência e concorrência no banco

**Estado:** `[ ] Não iniciado`

**Objetivo:** impedir inconsistências quando requisições concorrentes alteram os mesmos dados.

### Escopo

- Transações e limites da unidade de trabalho atual.
- Concorrência em reservas do mesmo horário.
- Índices e constraints de negócio.
- Idempotência em operações sensíveis.
- Migrations revisadas e reproduzíveis.
- Testes de integração com PostgreSQL.

### Critério de conclusão

Duas requisições concorrentes não criam reserva conflitante; migrations funcionam a partir de banco vazio; alterações destrutivas possuem plano seguro.

## Etapa 6 — decidir e completar a autenticação do MVP

**Estado:** `[ ] Não iniciado`

**Objetivo:** implementar somente os recursos de identidade necessários à primeira entrega.

### Decisões

- Refresh token entra no MVP?
- Haverá recuperação de senha?
- Email precisa ser confirmado?
- Tokens precisam de invalidação/logout no servidor?
- Qual política de bloqueio por tentativas será usada?
- Quais ações administrativas precisam de auditoria?

### Critério de conclusão

Fluxos escolhidos estão documentados, testados e não dependem de credenciais fixas ou configuração insegura.

## Etapa 7 — definir o escopo de pagamentos

**Estado:** `[ ] Não iniciado`

**Objetivo:** decidir se o mock atende ao MVP ou se um gateway real será integrado.

### Decisões confirmadas — 03/10/2026

- Uma instalação por barbearia e uma única conta recebedora, pertencente ao dono. Repasse aos barbeiros é responsabilidade dele, fora do BarberApp.
- Provedor escolhido: Mercado Pago, Checkout Pro; conectar a conta pelo painel Admin via OAuth.
- Implementar primeiro em testes, sem cobranças reais. Meios de pagamento dependem da disponibilidade do provedor para a conta/checkout.
- Primeiro incremento de conexão: início OAuth implementado (API key, JWT, role, configuração, URL fixa, state, PKCE, ausência de segredos e no-store). RED no WSL: 14 falhas esperadas e 1 aprovado; GREEN da solução: 74 aprovados. Diff aguarda revisão. Callback, tokens e checkout real ainda não implementados.
- Incremento seguinte: validação do callback escrita após RED (17 falhas, 4 aprovações). Ajuste da representação de security e do teste aprovado pelo mantenedor. GREEN: 95 aprovados, zero falhas/ignorados; build sem avisos/erros. Troca de tokens e conexão real permanecem pendentes; diff aguarda revisão final. Material em `docs/estudos/03-callback-state-e-concorrencia.html`.
- 05/10/2026: especificação HTTP da troca de tokens adicionada, 16 casos (RED: 14 falhas esperadas e 2 aprovados). Não houve implementação. Opt-in de conexão proposto para preservar o contrato atual. Persistência protegida e falha de gravação ainda precisam de testes antes de implementar sucesso. Estudo em `docs/estudos/04-troca-de-tokens-e-falhas.html`.
- Atualização de 05/10/2026 após aprovação: troca de tokens e gravação criptografada implementadas; conexão opt-in e sandbox explícito. Testes de persistência/proteção/invariantes/driver indisponível e migration em PostgreSQL WSL: GREEN com 125 aprovados, zero falhas/ignorados; modelo sem mudanças pendentes. Nenhuma conta real ou cobrança. Migration ainda não aplicada ao banco da aplicação. Pendentes: status para painel, renovação, desconexão/revogação, reconexão concorrente e fluxo Checkout Pro/webhooks. Revisar diff antes de avançar. Estudo em `docs/estudos/05-protecao-de-tokens-e-persistencia.html`.
- Callback, armazenamento protegido, renovação/desconexão, criação do checkout, notificações assinadas e reconciliação serão incrementos separados.
- A política de confirmação/cancelamento de agendamento, prazo de pagamento e reembolso ainda precisa ser definida antes de automatizar essas transições.

### Decisões

- Mock ou gateway real no MVP?
- Qual provedor será usado?
- Webhook será necessário?
- Como garantir idempotência?
- Quando o agendamento muda de estado após o pagamento?
- Como serão tratados reembolso e conciliação?

### Critério de conclusão

O fluxo escolhido possui testes, idempotência e transições de estado claras; o mock não é confundido com integração de produção.

## Etapa 8 — observabilidade, CI e deploy

**Estado:** `[ ] Não iniciado`

**Objetivo:** tornar a API operável em ambiente de homologação/produção.

### Escopo

- Health checks da API e PostgreSQL.
- Logs estruturados sem dados sensíveis.
- Correlation ID.
- CORS por ambiente.
- Rate limiting.
- HTTPS e reverse proxy.
- Dockerfile e configuração reproduzível.
- Pipeline de CI para build, testes, lint e varredura de segredos.
- Estratégia de migrations, backup e rollback.

### Critério de conclusão

Deploy de homologação é reproduzível, observável e validado; falhas de dependências aparecem nos health checks; CI bloqueia regressões.

## Etapa 9 — documentação e entrega do MVP

**Estado:** `[ ] Não iniciado`

**Objetivo:** fechar contratos, documentação e validação conjunta com o frontend.

### Checklist

- Swagger corresponde à implementação.
- README ensina configuração e execução sem expor segredos.
- `CODEX.md` e este roadmap refletem o projeto real.
- Arquivo `.http` funciona com configuração privada.
- Migrations constroem um banco vazio.
- Frontend valida as jornadas do MVP.
- Build, lint e testes passam no CI.
- Diff final e riscos são aprovados pelo mantenedor.

## Incremento em revisão — status local Mercado Pago (05/10/2026)

O incremento anterior foi revisado pelo mantenedor e enviado no commit `522e535`. Próxima especificação: GET Admin para consultar apenas status e metadados da conta única, sem ler tokens ou consultar o provedor. Estados: NaoConectado, Conectado e Expirado; expiração igual ao instante atual já conta como expirada.

RED no WSL: 16 casos, 13 falhas por rota/contrato inexistentes e três aprovados. Especificação aprovada pelo mantenedor; implementação sem alterar os testes, somente consulta/projeção local e contrato HTTP. GREEN: 141 testes da solução aprovados, zero falhas/ignorados, incluindo PostgreSQL; build sem avisos/erros e lint do recorte aprovado. Sem nova migration, chamadas externas ou pagamentos reais. Diff aguarda revisão final; estudo em `docs/estudos/06-status-local-da-conexao.html`. Renovação/desconexão, gestão de chaves, reconexão concorrente e checkout/webhooks continuam pendentes.

## Incremento em revisão — renovação manual (19/10/2026)

Status local aprovado e registrado em `86cba24`. Testes propostos para POST Admin de renovação: utilizar refresh protegido, rotacionar ambos os tokens, preservar conta/Admin/ambiente e retornar somente metadados após gravação. Bloquear configuração incompatível, conta trocada, credenciais locais inválidas e falhas externas sem vazamento ou retry automático.

RED: 26 falhas esperadas por rota ausente e dois aprovados. Testes aprovados pelo mantenedor; implementação mínima sem alterá-los. GREEN no WSL com PostgreSQL isolado: 169 aprovados (22 unitários + 147 integração), zero falhas/ignorados; build sem avisos/erros e lint do recorte aprovado. Serviço próprio de renovação, leitura protegida separada do status e transporte OAuth compartilhado. Sem migration, credenciais reais ou chamadas ao provedor. Diff final em revisão. Renovação automática, coordenação de operações simultâneas e recuperação após rotação remota sem persistência não estão resolvidas; permanecem gates antes de uso real. Estudo: `docs/estudos/07-renovacao-e-rotacao-de-tokens.html`.

## Incremento em revisão — concorrência local de credenciais (19/10/2026)

Renovação manual aprovada em `9f760ae`. Nove novos casos HTTP propõem primeira operação admitida com exclusividade até gravação/falha e rejeição imediata de concorrentes com 409 MercadoPagoOperacaoEmAndamento, sem segunda chamada externa. Abrange renovação/renovação, callback/callback e ambas as ordens mistas; preserva state do callback rejeitado antes do envio do código e permite consultar status.

RED funcional: oito falhas e um aprovado. Especificação aprovada e implementada sem alterar os testes: coordenador singleton com escopo descartável, consulta de state antes de admissão e consumo depois; proteção até concluir escrita/falha. GREEN no WSL com PostgreSQL isolado: 178 aprovados (22 unitários + 156 integração), zero falhas/ignorados; build sem avisos/erros e lint do recorte aprovado. Sem migration ou chamada real. Diff final em revisão. Escopo: uma instância da API. Coordenação distribuída, efeitos remotos incertos e recuperação de rotação sem persistência continuam pendentes. Estudo: `docs/estudos/08-concorrencia-e-ordem-das-operacoes.html`.

## Incremento em revisão — recuperação durável (20/10/2026)

Concorrência local aprovada em `0d485a8`. Mantenedor aprovou mudar a regra após resultado incerto e revisar testes conflitantes. Proposta: persistir RequerReautorizacao antes de enviar refresh; impedir chamada se o registro falhar; não apresentar credenciais incertas como Conectado; bloquear novas renovações até tokens novos serem salvos ou uma nova autorização ser concluída e persistida.

12 testes HTTP novos, um PostgreSQL novo e dois casos antigos evoluídos. RED: 191 executados, 177 aprovados/14 falhos. Especificação aprovada; implementação sem alterar esses testes. GREEN completo no WSL com PostgreSQL isolado: 191 aprovados, zero falhas/ignorados; build/lint do recorte aprovados e EF sem mudanças pendentes. Marcador persistido e limpeza junto dos novos tokens; migration `20261020213112_AddEstadoRecuperacaoMercadoPago` aplicada só ao banco de testes, com SQL revisado. Banco da aplicação inalterado; rollback exige avaliar perda da proteção. InMemory compartilhado verifica novo host, não crash real ou keyring. Coordenação distribuída e pagamentos completos permanecem pendentes. Diff final em revisão. Estudo: `docs/estudos/09-recuperacao-duravel-e-testes-vivos.html`.

## Definição de pronto do backend MVP

O backend será considerado pronto quando:

- não houver segredo versionado;
- autenticação e autorização estiverem testadas;
- casos de uso exigidos pelo frontend estiverem implementados;
- regras de agenda e pagamentos escolhidos tiverem cobertura;
- erros tiverem formato consistente;
- migrations forem reproduzíveis;
- CI, health checks e deploy de homologação funcionarem;
- documentação estiver atualizada;
- o mantenedor compreender e aprovar o diff final.

## Decisões de produto pendentes

1. Pagamento real entra no MVP ou permanece mock?
2. Onde a API será hospedada?
3. Refresh token, recuperação de senha e confirmação de email entram no MVP?
4. Quais telas do frontend já existem e quais contratos exigem?
5. Quais operações administrativas são indispensáveis?

As decisões devem ser registradas aqui antes da implementação correspondente.

## Qualificação operacional local — 22/10/2026

Mantenedor confirmou backup, aplicação das duas migrations Mercado Pago no banco local, histórico de oito entradas/schema correto e status autenticado 200 NaoConectado. User Secrets inicializado por ele e API key local salva sem valor literal no histórico. Metadados Windows indicam duas chaves DPAPI e ACL de pasta restrita; não é validação completa de recuperação.

Seis DataProtectionProcessTests novos, com executável auxiliar exclusivo de testes, verificam persistência entre processos, cópia no mesmo contexto e rejeição por chave ausente/aplicação/propósito/ciphertext incompatíveis. Testes passaram de primeira por caracterizarem Data Protection já existente; sem RED artificial nem mudança da implementação da API. GREEN WSL: 197 aprovados (22 unitários + 175 integração), zero falhas/ignorados; lint dos dois novos C# aprovado. Material em docs/estudos/10-chaves-processos-e-limites.html. Diff aguarda revisão.

Ainda pendentes: round-trip com configuração efetiva da API Windows, permissões dos arquivos individuais, procedimento de backup/recuperação das chaves reais e efeito de mudança de identidade/caminho. O probe usa configuração explícita e keyring de teste; WSL não valida DPAPI. Não habilitar produção/conta real nem declarar o gate operacional concluído com esta prova parcial.

## Diagnóstico pelo contêiner da API — GREEN, diff em revisão (22/10/2026)

Mantenedor aprovou o diff do estudo 10. Depois forneceu log de inicialização confirmando diretório padrão do perfil e DPAPI usados pela API. Ainda não houve recuperação real de tokens.

Quatro testes novos especificam modo do auxiliar que inicializa o Program.cs com dependências isoladas, resolve o provider registrado pela API e preserva seu discriminador padrão. Testes em processos separados: mesmo content root recupera; outro root/propósito ou keyring vazio rejeita. Apenas configuração/chaves fictícias, sem implementação nesta fase.

WSL inicial: quatro falhas no modo ausente do auxiliar, antes da etapa criptográfica; não é bug comprovado do provider nem RED funcional da API. Suíte nessa fase: 201 executados, 197 aprovados/quatro falhos, zero ignorados. Após aprovação, implementação somente do auxiliar, sem alterar os quatro testes ou a API: WebApplicationFactory inicia Program.cs com dependências isoladas; provider/discriminador resolvidos de ApiServices. Sem provider efêmero ou SetApplicationName.

GREEN final: 201 aprovados (22 unitários + 179 integração), zero falhas/ignorados, PostgreSQL isolado. Build sem erros/avisos; formatação dos quatro C# aprovada. Cópia do runtime do auxiliar restrita a binários/metadados, sem appsettings, validada em configuração nova dos projetos de testes. Execução no perfil Windows e política de backup continuam gates separados, sem autorização automática para gerar/alterar chaves reais. Não executar o auxiliar contra o perfil real com este contrato. Estudo 11 atualizado; diff final em revisão.

## Perfil Windows — barreiras aprovadas e recuperação local relatada

Incremento anterior aprovado e publicado em 595e35e. Onze novos casos especificaram confirmação, propósito, destino/root, opções readonly e preservação de arquivos antes de adicionar modo de perfil ao auxiliar. Nenhum teste executa Protect/Unprotect com as chaves reais. A aprovação dos testes veio antes da implementação registrada abaixo; procedimento em docs/operacao/prova-keyring-windows.md e estudo 12.

Execução real só após GREEN/revisão, no Windows e mesmo contexto da API, com geração desabilitada também na proteção. Comparar metadados antes/depois, sem copiar/abrir XML. Recusar Linux para criptografia do perfil; inspeção de opções pode ser portável. Plataforma/links/races/recuperação e backup permanecem gates explícitos, não comprovados por falha de modo ausente.

Na fase inicial, especificação executada no WSL: 212 testes, 201 aprovados e 11 novos falhos por modo não suportado, zero ignorados, incluindo PostgreSQL isolado. Naquele momento nenhum teste chegava a inspecionar opções do modo novo; ainda não havia implementação. Não houve criptografia no perfil real.

Após aprovação humana, implementação restrita ao auxiliar e factory: validações antes de iniciar host, mesma configuração readonly para os três modos, sem sobrescrever padrões/discriminador, criação exclusiva do payload, recusa de plataforma não Windows para criptografia. Onze testes intactos passaram; suíte completa WSL: 212 aprovados, zero falhas/ignorados, PostgreSQL isolado. Build sem erros/avisos e formatação dos quatro C# aprovada. Naquela etapa não houve operação nas chaves reais ou mudança da API de produção; revisão do diff e execução Windows ainda eram os próximos gates. Não declarar backup, DPAPI ou recuperação de tokens validados só por esse GREEN.

Atualização posterior a d0bdd68: diff aprovado/publicado e autorizações separadas para inspeção e operação criptográfica. Mantenedor confirmou inspeção com geração false, proteção de texto fictício em destino novo e recuperação em outra chamada com código 0; duas chaves antes/depois e zero diferenças nos metadados observados. Gate de recuperação local desse texto/contexto atendido por evidência do mantenedor. Tentativa preliminar com arquivo já existente, de origem não estabelecida, não conta como prova nova. Registro detalhado no procedimento.

Permanecem abertos: backup/restauração das chaves/identidade DPAPI, crash, mudança de máquina/usuário/root, validade e recuperação de tokens reais, checkout/webhooks e integração remota. Comparação de metadados não é auditoria de identidade bit a bit nem garantia absoluta de imutabilidade. Esta atualização é apenas documental; testes não repetidos e arquivos temporários preservados, sem limpeza ou acesso às chaves pelo agente.

## Backup — opção 1 escolhida, mesma máquina/identidade

Após dbae6f7, mantenedor escolheu recuperação local no mesmo Windows/usuário, sem incluir formatação/troca de computador. Não há mudança arquitetural de DPAPI para outro mecanismo e não há autorização de exportar master keys.

Teste novo de caracterização (sem alterar quatro casos existentes) copia XMLs fictícios para backup, restaura em outra pasta e recupera via provider da API em outro processo, conferindo igualdade de bytes/quantidades e preservação dos originais. GREEN WSL: 213 aprovados (22 unitários + 191 integração), zero falhas/ignorados, PostgreSQL isolado; build sem avisos/erros e formatação aprovada. Sem RED funcional ou implementação nova de produção, sem chaves reais. Rotação/revogação histórica e DPAPI não são certificados por essa fixture.

Gate inicial: revisar diff/procedimento/estudo 13 antes de autorizar cópia real e definir destino fora de Git/temporário com ACL restrita. Keyring compartilhado pode incluir outros projetos; preservar todos os XMLs, inclusive revogações. Restaurar só em pasta isolada, não sobre o original. Verificar lendo o restaurado, não o perfil original. Procedimento em docs/operacao/backup-keyring-mesmo-windows.md.

Atualização de 24/10/2026, após publicação de 932921a: mantenedor autorizou especificamente ACL/cópia e alcance compartilhado, confirmou API parada e forneceu evidências de dois XMLs copiados e restaurados em diretórios privados separados, com integridade local confirmada. api-recuperar apontando o restaurado retornou Recuperado/saída 0, processo 3140 e identidade de contexto igual à anterior. Gate local atendido para texto fictício no mesmo Windows/usuário, não para desastre, mudança de identidade/máquina, tokens reais, crash ou pagamentos. ACL individual dos XMLs e comparação após a recuperação final não foram fornecidas. Nenhum original sobrescrito, configuração alterada ou artefato limpo; suíte anterior de 213 não repetida para documentação.
