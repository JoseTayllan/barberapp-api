# Backup do keyring — mesmo Windows e usuário

## Escopo escolhido e estado

O mantenedor escolheu começar pela opção 1: recuperação de arquivos do keyring no mesmo Windows/usuário, com o contexto DPAPI preservado. Não inclui formatação, troca de computador/usuário, perda do perfil ou desastre no disco. Não muda a proteção da API para certificado/cofre.

Estado atualizado em 24/10/2026: caracterização aprovada/publicada em 932921a e prova nativa posterior executada pelo mantenedor, com autorizações separadas. Dois XMLs do perfil foram copiados e restaurados em outra pasta; api-recuperar usando o restaurado recuperou o texto fictício com saída 0. Não houve restauração sobre o keyring ativo. Detalhes e limites abaixo.

Validação automática anterior no WSL: 213 aprovados (22 unitários + 191 integração), zero falhas/ignorados; build sem avisos/erros e formatação do recorte aprovada. Nenhum código de implementação foi necessário. Esta atualização apenas documental não repete esses testes; seu diff aguarda revisão.

## O que o teste verifica

Em ApiDataProtectionProcessTests, o novo caso inicia o provider pelo contêiner da API com banco/configuração/keyring fictícios. Protege o texto num processo, copia os XMLs para backup, copia desse backup para outra pasta restaurada e recupera em novo processo apontado explicitamente para a pasta restaurada. Mantém content root e propósito.

Confere quantidade de XMLs e igualdade de bytes nos originais, backup e restaurado após a leitura. Somente material criado pelo próprio teste é lido; nenhuma chave do perfil. Asserções recebem apenas booleanos/mensagens fixas, não imprimem bytes ou XMLs. Todos os temporários dessa fixture são descartáveis e limitados à pasta exclusiva do teste.

Passou de primeira por usar funcionalidades já existentes. Não houve RED funcional nem alteração do auxiliar para forçar resultado. Um aviso CS8714 de nulabilidade na preparação do teste foi corrigido antes da revisão; não era RED. Os quatro testes anteriores não foram alterados.

## Limites da prova automática

- Foi executada no WSL: chaves fictícias em diretório explícito, sem DPAPI em disco. Não certifica restauração do perfil Windows.
- O keyring gerado nesse caso não possui histórico de rotação/revogação. O filtro copia todos os XMLs, mas esse teste não simula revogações e não certifica a política de restaurar backups históricos.
- Igualdade de bytes na fixture usa apenas chaves fictícias. A prova Windows inicial comparou metadados; a operação posterior de backup comparou SHA-256 localmente durante as cópias. São evidências diferentes; não houve comparação após a recuperação nativa final.
- Não é backup de banco, User Secrets ou identidade DPAPI. Não testa tokens reais, queda de energia ou pagamento.

## Condições para copiar material real — exigidas a cada nova operação

1. Aprovar este diff/procedimento e autorizar explicitamente a cópia dos XMLs protegidos. Backup acessa/copia material criptográfico; a autorização anterior de inspeção não cobre essa cópia.
2. Escolher pasta de backup persistente fora do repositório, fora do temporário e sem sincronização pública. Não versionar XMLs, compactados ou arquivos de chaves. Backup no mesmo disco não protege contra perda desse disco.
3. Conferir ACL do destino antes de copiar: acesso restrito ao usuário, SYSTEM e administradores conforme ambiente aprovado. Não presumir que a ACL do arquivo original acompanha a cópia. Não enfraquecer permissões para contornar erro.
4. Reconhecer que o keyring padrão do perfil pode atender outros projetos. Uma cópia integral pode incluir suas chaves também; isso precisa ser aceito explicitamente. Não tentar identificar chaves do BarberApp apenas pelo nome do arquivo.
5. Manter a API parada e evitar outros processos que modifiquem o repositório compartilhado durante a captura. Se o conjunto mudar, parar/reavaliar, sem apagar chaves para estabilizar a comparação.

## Procedimento de referência

1. Registrar apenas contexto e metadados necessários; nunca publicar conteúdo de XMLs/payloads. Guardar o texto cifrado fictício da prova de forma privada, separado dos resultados públicos.
2. Criar um destino de backup novo e restrito, sem sobrescrever backups anteriores. Preservar os XMLs do repositório: chaves e registros de revogação existentes. Não selecionar só a chave mais recente nem descartar expiradas. Recusar links, formatos inesperados ou mudanças durante a captura até revisão.
3. Criar uma pasta restaurada nova e isolada. Copiar somente do backup para essa pasta, não da origem novamente. Não substituir, renomear, apagar ou mesclar a pasta original do perfil.
4. Conferir a integridade da cópia localmente, sem imprimir material criptográfico. Qualquer leitura/comparação dos XMLs reais deve fazer parte da autorização de backup, não de uma inspeção só de opções.
5. Na verificação futura usar somente api-recuperar, apontando explicitamente para a pasta restaurada, com mesmo Windows/usuário, content root e propósito. Esse modo já desabilita geração automática. Não usar api-proteger/proteger na cópia ou no perfil real.
6. Não usar perfil-recuperar para provar o backup: ele consulta o keyring padrão original, podendo passar mesmo que o backup esteja incorreto. A prova precisa ler a pasta restaurada.
7. Conferir recuperação e metadados após a leitura. Registrar somente resultado/contexto e limitações, sem atribuir sucesso a uma recuperação feita da origem. Não automatizar limpeza nem alterar a configuração da API para usar a cópia.

Novas operações continuam exigindo confirmação do destino, ACL, alcance e autorização. Os passos abaixo registram a operação já autorizada; não autorizam novas cópias ou restauração sobre a instalação ativa.

## Evidência operacional fornecida pelo mantenedor — 24/10/2026

- Autorizou especificamente ajuste de ACL e cópia integral do keyring do perfil, reconhecendo eventuais chaves de outros projetos. Escolheu pasta persistente fora do Git/temporário e confirmou API parada.
- Removeu acesso herdado de CodexSandboxUsers na pasta de backup. Saídas das ACLs das pastas mostraram somente seu usuário, SYSTEM e administradores; permissões individuais dos XMLs não foram auditadas.
- Primeira tentativa falhou por variável de sessão nula. Compare-Object não concluiu e foreach não copiou nada. O destino foi confirmado vazio por inspeção de metadados; não foi considerado backup válido. Corrigiu-se a operação com bloco autocontido, ErrorActionPreference Stop e variáveis locais, sem alterar código da API.
- Relatou dois XMLs copiados, SHA-256 de origem/cópia coincidentes e ausência de mudança de conteúdo detectada nos originais durante essa conferência. Depois copiou somente do backup para outro diretório novo e confirmou integridade de dois XMLs restaurados. Hashes e conteúdo não foram publicados.
- Recuperou o arquivo fictício candidato mais recente, de 22/10 às 19:34, usando api-recuperar com diretório restaurado explícito, root da API e propósito BarberApp.MercadoPago.Tokens.v1. O horário não estabelece proveniência; o auxiliar confirmou o conteúdo fictício esperado.
- Resultado informado: origem ApiServices, status Recuperado, processoId 3140, código de saída 0. identidadeHash 151FD836BBDE1D2597700D4C2183C013D39E8C0D76B1446F4F29DFB2595A0E5F, igual ao contexto da prova anterior. Esse hash identifica o contexto da aplicação; não é hash de chave nem segredo.
- Auxiliar com geração automática desabilitada. Não houve comparação pós-recuperação dos XMLs; não afirmar imutabilidade final, ausência absoluta de escrita ou consistência sob concorrência. Não foram alterados diretório original ou configuração da API. Nenhum arquivo foi apagado e nenhum material foi enviado ao WSL/Git pelo agente.

Conclusão: recuperação do texto fictício a partir da cópia restaurada no mesmo Windows/usuário. Não certifica tokens reais, restauração de banco/User Secrets, crash, perda do contexto DPAPI ou desastre no disco. O conteúdo dos XMLs não foi aberto pelo agente; evidências criptográficas são saídas da execução do mantenedor.

## Regras de restauração

- Recuperar a prova numa pasta isolada não é autorizar restaurar sobre a instalação ativa.
- Não reintroduzir chaves revogadas por restauração de um backup anterior à revogação. Avaliar histórico completo e registros atuais antes de qualquer restauração efetiva; não sobrepor o keyring ativo para fazer o teste passar.
- A cópia pode conter chaves de outros projetos: tratar como material sensível mesmo com DPAPI. Não enviar para chat, Git ou storage público.
- Se o contexto DPAPI for perdido, esta opção não garante recuperação. Não prometer portabilidade por copiar os XMLs e não exportar master keys do Windows como atalho.
- O gate local de recuperação de texto fictício via cópia restaurada foi atendido no contexto relatado. Desastre, perda do contexto DPAPI e recuperação de tokens reais continuam abertos. Não transformar a prova limitada em garantia geral.

## Fontes oficiais

- [Criptografia das chaves e Windows DPAPI](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-encryption-at-rest?view=aspnetcore-10.0).
- [Formato XML das chaves e registros de revogação](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-format?view=aspnetcore-10.0).
- [Ciclo de vida, expiração e revogação](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-management?view=aspnetcore-10.0).
