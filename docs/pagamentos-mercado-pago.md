# Mercado Pago — plano e contrato

## Decisão confirmada

Uma instalação para uma barbearia. A conta recebedora é do dono e é conectada pelo administrador. Não existe divisão automática entre barbeiros. O checkout escolhido é Checkout Pro.

## Incremento 1: início da autorização

Contrato implementado: `POST /api/integracoes/mercado-pago/autorizacao`.

| Cenário | Resultado |
| --- | --- |
| Sem API key | 401 |
| Sem JWT | 401 |
| Cliente ou Barbeiro | 403 |
| Admin sem identificador | 401 |
| Admin válido, configuração válida | 200, URL OAuth e expiração; no-store |
| Configuração inválida | 503, título MercadoPagoNaoConfigurado, sem valores privados |

A URL deve apontar exclusivamente a `https://auth.mercadopago.com/authorization` e incluir client_id, response_type=code, platform_id=mp, redirect_uri estático, state aleatório e challenge PKCE S256. State e challenge devem variar por tentativa. Não incluir client_secret ou code_verifier.

ClientId deve ser identificador numérico. RedirectUri precisa de HTTPS e não pode conter credenciais nem fragmento. ClientSecret deve existir no servidor para que o fluxo completo possa prosseguir.

## Evidência TDD

- Baseline executado pelo agente: 14 unitários + 45 integração aprovados.
- Escritos 15 casos HTTP em `MercadoPagoConnectionTests` antes de implementação.
- Inicialmente o Smart App Control bloqueou a DLL de integração (`0x800711C7`) antes de descobrir os casos. Após autorização do WSL, RED funcional obtido no Ubuntu: 14 falhas por rota inexistente e 1 aprovado.
- A tentativa do mantenedor no próprio terminal também foi bloqueada: resumo com zero testes executados. Portanto, o problema não é exclusivo do ambiente do agente. Eventos locais 3077/3118 de CodeIntegrity identificam o Smart App Control e o carregamento da DLL pelo testhost. Não contornar a política alterando testes, DLLs, registro ou configuração de segurança.
- Comando: `dotnet test BarberApp.IntegrationTests/BarberApp.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~MercadoPagoConnectionTests`.
- Após implementação mínima, solução no WSL: 74 aprovados (14 unitários + 60 integração), zero falhas/ignorados. Os 15 testes OAuth não foram alterados para obter GREEN. Build: zero avisos/erros.

### Como funciona o código

O controller exige Admin e identificador no JWT, aplica no-store e traduz somente a falha esperada de configuração para ProblemDetails 503. A interface e o DTO pertencem à Application; a Infrastructure valida configuração, gera state e verifier criptograficamente aleatórios e calcula challenge SHA-256 codificado em Base64URL. A URL usa host oficial fixo e parâmetros escapados.

Uma tentativa por administrador fica em memória durante dez minutos; iniciar novamente substitui a anterior. Reiniciar a API perde as tentativas. O store singleton valida expiração e consome state atomicamente; o serviço scoped usa repositório scoped para salvar a conexão. Não adequado a múltiplas instâncias sem armazenamento coordenado.

Configuração privada: `MercadoPago__ClientId`, `MercadoPago__ClientSecret` e `MercadoPago__RedirectUri`, por ambiente/cofre. A conexão é opt-in com `MercadoPago__ConexaoEnabled=true`; exige `MercadoPago__Sandbox` booleano explícito. Começar com sandbox após revisar migration, key ring e retorno HTTPS. Não colocar credenciais reais em arquivos, exemplos ou logs; não ativar produção nesta entrega.

## Próximos incrementos

### Validação do callback em revisão

Implementado parcialmente `GET /api/integracoes/mercado-pago/callback`: exceção exata à API key, sem JWT, no-store/no-referrer, state vinculado ao contexto Admin no servidor e consumo atômico sob lock compartilhado. Expiração, substituição, replay, recusa, separação de administradores e concorrência estão cobertos. Memória continua limitada a uma instância; reinício perde tentativas.

Recusa válida retorna 200 com status `AutorizacaoRecusada`; retorno inválido, 400 `MercadoPagoCallbackInvalido`. Com conexão desabilitada, código válido mantém 503 `MercadoPagoTrocaTokenPendente`. Com conexão habilitada, `Conectado` só é retornado depois da troca e da gravação protegida. Durante os testes, todas as chamadas externas são simuladas.

RED novo: 21 testes, 17 falhas e 4 aprovados. Houve uma falha posterior de serialização OpenAPI: a lista vazia era omitida. Após aprovação explícita do mantenedor em 03/10/2026, usamos `[{}]`, válida segundo [OpenAPI 3.0.3](https://spec.openapis.org/oas/v3.0.3.html#operation-object), e verificamos um único requisito vazio no teste, preservando as exigências na autorização. GREEN: 95 testes aprovados, zero falhas/ignorados; build sem avisos/erros. Revisão final do diff pendente.

Sem logs novos. Não ativar logs de query OAuth; revisar também proxies e futura instrumentação antes de ambiente externo.

### Troca e persistência — 05/10/2026

O cliente usa exclusivamente POST JSON em `https://api.mercadopago.com/oauth/token`, PKCE correspondente à tentativa, `test_token` conforme sandbox, timeout de dez segundos e resposta limitada a 64 KiB. Não segue redirects nem repete automaticamente o código. JSON/campos inválidos e HTTP sem sucesso são traduzidos para erros seguros; nenhum body remoto é reproduzido.

| Falha | Resposta |
| --- | --- |
| HTTP sem sucesso | 502 MercadoPagoProvedorFalhou |
| Resposta inválida | 502 MercadoPagoRespostaInvalida |
| Timeout/rede | 503 MercadoPagoIndisponivel |
| Proteção criptográfica | 503 MercadoPagoProtecaoFalhou |
| Gravação/driver PostgreSQL | 503 MercadoPagoPersistenciaFalhou |

Data Protection protege access/refresh token com propósito `BarberApp.MercadoPago.Tokens.v1` antes de chegar ao repositório. A tabela contém ciphertext, conta, Admin de origem, expiração UTC e sandbox. PK + CHECK Id=1 mantêm uma linha por instalação; reconectar substitui a conexão existente. Não há colunas plaintext nem endpoint devolvendo esses tokens.

As chaves ficam fora do banco/repositório, no mecanismo Data Protection do ambiente. Em Windows com perfil disponível, o padrão protege chaves com DPAPI. Linux, containers, mudança de identidade/caminho e backup exigem validação específica; não presumir durabilidade ou proteção em disco. Os testes usam provider efêmero, não validam recuperação operacional de chaves. [Referência Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/default-settings?view=aspnetcore-10.0).

Migration e snapshot: `20261005172851_AddConexaoMercadoPago`. [SQL para revisão](sql/20261005172851_AddConexaoMercadoPago.sql) cria somente a nova tabela; aplicar requer o baseline anterior correto e aprovação do ambiente. Down apaga a tabela e as credenciais. Não foi aplicado ao banco da aplicação.

GREEN no WSL: 125 aprovados (22 unitários + 103 integração), zero falhas/ignorados, incluindo migration/persistência/constraint no PostgreSQL isolado. Testes anteriores e expectativas da troca preservados. A fixture nova foi corrigida para compartilhar o mesmo InMemory entre escopos, sem alterar contratos; o erro original era um banco diferente por resolução.

Limites: não há renovação automática/revogação/desconexão, política de reconexão concorrente, checkout/webhooks ou validação sandbox real. Status local e renovação manual foram implementados nos incrementos abaixo. Não declarar pagamentos prontos. Perder as chaves inviabiliza ler ciphertext; falha de gravação após emissão de tokens pode exigir revogar a autorização no provedor e iniciar novamente. Resolver esses pontos antes de dinheiro real.

1. Emissão da URL e contexto temporário implementados; revisar diff.
2. Callback e troca com PKCE implementados e testados com HTTP simulado; revisar diff.
3. Tokens protegidos gravados, status local Admin e renovação manual implementados. Próximos: recuperação de rotação sem persistência, desconexão/revogação e coordenação de operações concorrentes.
4. Gerar checkout: autorização por propriedade do agendamento, preço consultado no servidor e idempotência.
5. Validar notificações e consultar status no provedor antes de atualizar pagamento.
6. Exercitar ambiente de teste real, com aplicação cadastrada e URL HTTPS acessível.

O callback externo não envia API key e já possui exceção restrita ao GET exato, protegida por state. Notificações ainda precisam de autenticação específica e exceção restrita/testada; nunca liberar genericamente rotas de pagamento.

Nenhuma conta foi conectada, nenhuma credencial real foi solicitada e nenhuma chamada ou cobrança foi feita. O mock existente permanece identificado como mock enquanto o fluxo real não estiver completo.

## Consulta de status local — implementada, diff em revisão

`GET /api/integracoes/mercado-pago/status` requer API key e JWT Admin identificado; não compartilha a exceção pública do callback. Retorna 200 com exatamente `status`, `contaId`, `expiraEm`, `sandbox`, sem tokens/ciphertext ou Admin de origem.

Sem registro: `NaoConectado` e metadados null. Registro com expiração futura: `Conectado`; expiração menor ou igual ao UTC atual: `Expirado`. Metadados da conta expirada são preservados. Estado é da instalação e pode ser consultado por outro Admin, mesmo com OAuth desabilitado ou sem credenciais do provedor.

Consulta estritamente local, sem descriptografar, atualizar ou criar cliente HTTP externo. Não atesta validade remota ou capacidade de pagamento. Falha esperada do driver: 503 com título `MercadoPagoConsultaFalhou`, sem detalhes internos; sucesso e essa falha com no-store.

Após RED de 13 falhas/três aprovações e aprovação humana, os mesmos 16 casos passaram sem alteração. GREEN da solução no WSL: 141 aprovados (22 unitários + 119 integração), zero falhas/ignorados, incluindo PostgreSQL. Build sem avisos/erros; lint do recorte aprovado. Repositório projeta apenas os três metadados com AsNoTracking, sem carregar ciphertext nem Admin; contrato de leitura separado preserva o de gravação. Não há migration nova. Veja [estudo 06](estudos/06-status-local-da-conexao.html).

## Renovação manual — GREEN técnico, diff em revisão (19/10/2026)

Status local revisado pelo mantenedor e registrado em `86cba24`. Após RED de 26 falhas/dois aprovados e aprovação da especificação, implementado `POST /api/integracoes/mercado-pago/renovacao`. Requer API key + JWT Admin identificado e usa somente as credenciais protegidas da instalação, sem tokens fornecidos pelo consumidor.

Contrato externo baseado no [exemplo oficial de renovação](https://www.mercadopago.com.br/developers/pt/docs/security/oauth/renewal): POST JSON para o host OAuth fixo, grant_type refresh_token, refresh anterior e credenciais da aplicação. O provedor informa rotação do refresh token e exige autorização offline_access; validar essa permissão na integração sandbox real. O modelo atual não persiste scopes e os testes simulados não comprovam consentimento remoto.

Sucesso: 200 com exatamente status/contaId/expiraEm/sandbox, no-store, somente após proteger e gravar ambos os tokens novos. Preservar conta, Admin de origem e sandbox; o Admin que solicita pode ser outro. Aceitar renovação antes ou depois da expiração local não garante que o provedor aceite qualquer token antigo.

| Condição | Resultado |
| --- | --- |
| Sem conexão | 409 MercadoPagoNaoConectado |
| Conexão desabilitada | 503 MercadoPagoConexaoDesabilitada |
| ClientId/ClientSecret ausentes | 503 MercadoPagoNaoConfigurado |
| Sandbox configurado diferente do registro | 503 MercadoPagoAmbienteIncompativel |
| Credenciais locais ilegíveis/incompletas | 503 MercadoPagoCredenciaisIndisponiveis |
| 400 remoto com error invalid_grant | 409 MercadoPagoReautorizacaoNecessaria |
| Outro HTTP remoto sem sucesso | 502 MercadoPagoProvedorFalhou |
| Resposta inválida ou user_id diferente | 502 MercadoPagoRespostaInvalida |
| Timeout | 503 MercadoPagoIndisponivel |
| Proteção ou escrita falhou | 503 MercadoPagoProtecaoFalhou / MercadoPagoPersistenciaFalhou |

Falhas mantêm o registro local anterior, sem expor secrets, ciphertext ou bodies externos. Não fazer retry automático. Se a rotação remota ocorreu antes da falha local, o refresh antigo pode não funcionar: preservar banco não é rollback do provedor. Operações simultâneas, recuperação dessa situação e renovação automática exigem outro recorte e testes antes de uso real. Não habilitar dinheiro real após este GREEN isoladamente.

RenovacaoMercadoPagoService é scoped e separado da consulta de status. ICredenciaisConexaoMercadoPagoRepository obtém somente a entidade com ciphertext, sem tracking; não modifica a projeção de status. O cliente OAuth reaproveita transporte/validação do callback, interpretando invalid_grant somente na renovação. A configuração de timeout/limite de resposta/redirects continua a mesma; sem retry ou mudanças de schema.

Os mesmos 28 testes passaram sem alteração; suíte completa no WSL com PostgreSQL isolado: 169 aprovados (22 unitários + 147 integração), zero falhas/ignorados; build sem avisos/erros, lint do recorte aprovado. O HTTP novo usa InMemory e provedor simulado; o teste relacional existente cobre persistência/migration/constraint anterior, não a renovação HTTP contra PostgreSQL. Sem conta real conectada, credenciais reais ou cobranças.

## Exclusividade local de credenciais — GREEN técnico, diff em revisão

Incremento de renovação aprovado pelo mantenedor em `9f760ae`. Após aprovação dos nove testes, implementada uma operação alteradora por vez nesta instância da API, abrangendo renovação e troca de código do callback. A primeira admitida mantém exclusividade até salvar ou falhar; concorrente recebe 409 MercadoPagoOperacaoEmAndamento, no-store, sem segredos/state e sem chamada ao provedor. Não há fila ou retry automático. É um status adicional de conflito que consumidores deverão tratar.

Um callback válido rejeitado por ocupação ainda não enviou seu código: manter state disponível para nova solicitação manual enquanto não expirar/substituir. Após a tentativa ser aceita, manter consumo único e rejeitar replay. Consulta de status e validação de callback inválido continuam disponíveis. Uma renovação admitida depois de reconexão deve ler o refresh atual, não usar um snapshot antigo.

Os testes pausam provedor ou SaveChangesAsync com sinais explícitos, sem sleeps. Seis cenários de disputa; dois de liberação após falha; um de disponibilidade do status/callback inválido. RED: oito falhos e um aprovado; GREEN dos mesmos nove, arquivo preservado. Suíte completa no WSL com PostgreSQL isolado: 178 aprovados, zero falhas/ignorados; build sem avisos/erros e lint do recorte aprovado. Veja [estudo 08](estudos/08-concorrencia-e-ordem-das-operacoes.html).

Coordenador singleton admite atomicamente por CompareExchange e devolve escopo IDisposable idempotente; serviços usam using através dos awaits de I/O. Renovação entra antes da leitura das credenciais. Callback verifica tentativa sem consumir, tenta admissão e só então consome sob lock; recusa/fluxo desabilitado preservam comportamento anterior, sem participar de alteração de credenciais. Não usa transação/lock de banco, não muda schema e não carrega serviços scoped no singleton.

Limites: não é lock distribuído, transação remota/local, controle sobre outro processo ou garantia de token válido após erro. O provedor falso aceita a solicitação seguinte em testes de liberação apenas para provar que a exclusão não ficou presa; isso não comprova recuperação real de refresh rotacionado. Coordenação entre processos e reconciliação de resultados incertos exigem outros testes antes de uso real.

## Referências oficiais

- [Authorization code, state e PKCE](https://www.mercadopago.com.br/developers/pt/docs/security/oauth/creation).
- [Boas práticas OAuth](https://www.mercadopago.com.br/developers/en/docs/security/oauth/best-practices).
- [Notificações de pagamento](https://www.mercadopago.com.br/developers/pt/docs/checkout-pro-preferences/payment-notifications).

## Revisão e esforço

Este incremento acrescenta rota, contrato, serviço, DI e documentação. Nenhuma mudança no pagamento mock vigente. O risco principal é confundir emissão de URL com integração pronta. Um agente principal; RED, implementação mínima, suíte completa e build/formatação do recorte. Reduzir esforço usando testes focados durante desenvolvimento e a suíte completa no gate final. Nenhum commit/push realizado; revisão humana ainda pendente.
