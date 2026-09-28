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
