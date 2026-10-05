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

Limites: não há refresh/revogação/desconexão, status para painel, política de reconexão concorrente, checkout/webhooks ou validação sandbox real. Não declarar pagamentos prontos. Perder as chaves inviabiliza ler ciphertext; falha de gravação após emissão de tokens pode exigir revogar a autorização no provedor e iniciar novamente. Resolver esses pontos antes de dinheiro real.

1. Emissão da URL e contexto temporário implementados; revisar diff.
2. Callback e troca com PKCE implementados e testados com HTTP simulado; revisar diff.
3. Tokens protegidos gravados. Próximos: status Admin, renovação, desconexão/revogação e coordenação de reconexões.
4. Gerar checkout: autorização por propriedade do agendamento, preço consultado no servidor e idempotência.
5. Validar notificações e consultar status no provedor antes de atualizar pagamento.
6. Exercitar ambiente de teste real, com aplicação cadastrada e URL HTTPS acessível.

O callback externo não envia API key e já possui exceção restrita ao GET exato, protegida por state. Notificações ainda precisam de autenticação específica e exceção restrita/testada; nunca liberar genericamente rotas de pagamento.

Nenhuma conta foi conectada, nenhuma credencial real foi solicitada e nenhuma chamada ou cobrança foi feita. O mock existente permanece identificado como mock enquanto o fluxo real não estiver completo.

## Referências oficiais

- [Authorization code, state e PKCE](https://www.mercadopago.com.br/developers/pt/docs/security/oauth/creation).
- [Boas práticas OAuth](https://www.mercadopago.com.br/developers/en/docs/security/oauth/best-practices).
- [Notificações de pagamento](https://www.mercadopago.com.br/developers/pt/docs/checkout-pro-preferences/payment-notifications).

## Revisão e esforço

Este incremento acrescenta rota, contrato, serviço, DI e documentação. Nenhuma mudança no pagamento mock vigente. O risco principal é confundir emissão de URL com integração pronta. Um agente principal; RED, implementação mínima, suíte completa e build/formatação do recorte. Reduzir esforço usando testes focados durante desenvolvimento e a suíte completa no gate final. Nenhum commit/push realizado; revisão humana ainda pendente.
