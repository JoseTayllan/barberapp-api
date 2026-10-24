# Backup do keyring — mesmo Windows e usuário

## Escopo escolhido e estado

O mantenedor escolheu começar pela opção 1: recuperação de arquivos do keyring no mesmo Windows/usuário, com o contexto DPAPI preservado. Não inclui formatação, troca de computador/usuário, perda do perfil ou desastre no disco. Não muda a proteção da API para certificado/cofre.

Estado: um teste de caracterização com chaves e texto fictícios passou no WSL; nenhuma chave real foi copiada ou restaurada. Suíte completa: 213 aprovados (22 unitários + 191 integração), zero falhas/ignorados; build sem avisos/erros e formatação do recorte aprovada. Nenhum código de implementação foi necessário. O diff e este procedimento aguardam revisão; não executar cópia real antes de autorização específica e escolha do destino.

## O que o teste verifica

Em ApiDataProtectionProcessTests, o novo caso inicia o provider pelo contêiner da API com banco/configuração/keyring fictícios. Protege o texto num processo, copia os XMLs para backup, copia desse backup para outra pasta restaurada e recupera em novo processo apontado explicitamente para a pasta restaurada. Mantém content root e propósito.

Confere quantidade de XMLs e igualdade de bytes nos originais, backup e restaurado após a leitura. Somente material criado pelo próprio teste é lido; nenhuma chave do perfil. Asserções recebem apenas booleanos/mensagens fixas, não imprimem bytes ou XMLs. Todos os temporários dessa fixture são descartáveis e limitados à pasta exclusiva do teste.

Passou de primeira por usar funcionalidades já existentes. Não houve RED funcional nem alteração do auxiliar para forçar resultado. Um aviso CS8714 de nulabilidade na preparação do teste foi corrigido antes da revisão; não era RED. Os quatro testes anteriores não foram alterados.

## Limites da prova automática

- Foi executada no WSL: chaves fictícias em diretório explícito, sem DPAPI em disco. Não certifica restauração do perfil Windows.
- O keyring gerado nesse caso não possui histórico de rotação/revogação. O filtro copia todos os XMLs, mas esse teste não simula revogações e não certifica a política de restaurar backups históricos.
- Igualdade de bytes é verificada apenas nas chaves fictícias. A prova Windows anterior comparou somente nomes, tamanhos e datas de escrita das chaves reais; não confundir essas evidências.
- Não é backup de banco, User Secrets ou identidade DPAPI. Não testa tokens reais, queda de energia ou pagamento.

## Antes de copiar material real — decisões pendentes

1. Aprovar este diff/procedimento e autorizar explicitamente a cópia dos XMLs protegidos. Backup acessa/copia material criptográfico; a autorização anterior de inspeção não cobre essa cópia.
2. Escolher pasta de backup persistente fora do repositório, fora do temporário e sem sincronização pública. Não versionar XMLs, compactados ou arquivos de chaves. Backup no mesmo disco não protege contra perda desse disco.
3. Conferir ACL do destino antes de copiar: acesso restrito ao usuário, SYSTEM e administradores conforme ambiente aprovado. Não presumir que a ACL do arquivo original acompanha a cópia. Não enfraquecer permissões para contornar erro.
4. Reconhecer que o keyring padrão do perfil pode atender outros projetos. Uma cópia integral pode incluir suas chaves também; isso precisa ser aceito explicitamente. Não tentar identificar chaves do BarberApp apenas pelo nome do arquivo.
5. Manter a API parada e evitar outros processos que modifiquem o repositório compartilhado durante a captura. Se o conjunto mudar, parar/reavaliar, sem apagar chaves para estabilizar a comparação.

## Procedimento proposto, sem execução real nesta etapa

1. Registrar apenas contexto e metadados necessários; nunca publicar conteúdo de XMLs/payloads. Guardar o texto cifrado fictício da prova de forma privada, separado dos resultados públicos.
2. Criar um destino de backup novo e restrito, sem sobrescrever backups anteriores. Preservar os XMLs do repositório: chaves e registros de revogação existentes. Não selecionar só a chave mais recente nem descartar expiradas. Recusar links, formatos inesperados ou mudanças durante a captura até revisão.
3. Criar uma pasta restaurada nova e isolada. Copiar somente do backup para essa pasta, não da origem novamente. Não substituir, renomear, apagar ou mesclar a pasta original do perfil.
4. Conferir a integridade da cópia localmente, sem imprimir material criptográfico. Qualquer leitura/comparação dos XMLs reais deve fazer parte da autorização de backup, não de uma inspeção só de opções.
5. Na verificação futura usar somente api-recuperar, apontando explicitamente para a pasta restaurada, com mesmo Windows/usuário, content root e propósito. Esse modo já desabilita geração automática. Não usar api-proteger/proteger na cópia ou no perfil real.
6. Não usar perfil-recuperar para provar o backup: ele consulta o keyring padrão original, podendo passar mesmo que o backup esteja incorreto. A prova precisa ler a pasta restaurada.
7. Conferir recuperação e metadados após a leitura. Registrar somente resultado/contexto e limitações, sem atribuir sucesso a uma recuperação feita da origem. Não automatizar limpeza nem alterar a configuração da API para usar a cópia.

Não há comandos de cópia real neste documento: destino, ACL, alcance do keyring compartilhado e autorização específica ainda estão pendentes. Depois da revisão, a operação será guiada um passo por vez.

## Regras de restauração

- Recuperar a prova numa pasta isolada não é autorizar restaurar sobre a instalação ativa.
- Não reintroduzir chaves revogadas por restauração de um backup anterior à revogação. Avaliar histórico completo e registros atuais antes de qualquer restauração efetiva; não sobrepor o keyring ativo para fazer o teste passar.
- A cópia pode conter chaves de outros projetos: tratar como material sensível mesmo com DPAPI. Não enviar para chat, Git ou storage público.
- Se o contexto DPAPI for perdido, esta opção não garante recuperação. Não prometer portabilidade por copiar os XMLs e não exportar master keys do Windows como atalho.
- Os gates de backup/restauração do perfil Windows real e de desastre permanecem abertos até evidência específica. Esta escolha reduz o escopo; não transforma risco pendente em garantia.

## Fontes oficiais

- [Criptografia das chaves e Windows DPAPI](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-encryption-at-rest?view=aspnetcore-10.0).
- [Formato XML das chaves e registros de revogação](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-format?view=aspnetcore-10.0).
- [Ciclo de vida, expiração e revogação](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-management?view=aspnetcore-10.0).
