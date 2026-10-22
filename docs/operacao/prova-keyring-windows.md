# Prova local do keyring Windows — procedimento proposto

Estado em 22/10/2026: **modos perfil-* implementados após aprovação dos testes; GREEN automático, diff em revisão**. Suíte WSL com PostgreSQL isolado: 212 aprovados, zero falhas/ignorados; testes aprovados preservados. Não executar o roteiro operacional antes da revisão do diff e autorização da execução local. Nenhum Protect/Unprotect foi executado com o perfil real. Os modos antigos proteger/api-proteger podem gerar chaves e não devem ser apontados para o perfil real.

## Objetivo e limite

Proteger um texto fixo explicitamente fictício, encerrar o processo e recuperá-lo em outro processo Windows, usando o provider do contêiner inicializado pelo Program.cs, o content root da instalação e o keyring padrão do perfil. Não conectar PostgreSQL, ler User Secrets, criar endpoint, chamar Mercado Pago ou efetuar pagamento. O host de diagnóstico deve continuar usando bootstrap desabilitado e configurações/banco fictícios.

Isso comprovaria recuperação nesse contexto de máquina/usuário/root e versão. Não comprova restauração em outra máquina, crash de disco, backup de identidade DPAPI, validade remota dos tokens ou prontidão para pagamentos.

## Contrato implementado no auxiliar

Todos os modos exigem a confirmação literal `--confirmar-leitura-do-perfil-windows`. A confirmação é reconhecimento humano da operação, **não autenticação nem autorização de acesso**. Os direitos continuam sendo os do usuário Windows.

- `perfil-inspecionar`: obtém somente opções do contêiner, sem Protect/Unprotect ou enumeração das chaves. Informa geração automática false, ausência de sobrescritas explícitas do repositório/encryptor e hash do discriminador. Pode ser verificado no WSL porque não é operação criptográfica sobre o perfil.
- `perfil-proteger`: somente Windows, após barreiras; usa o texto fictício fixo, não recebe segredos do operador. Arquivo novo exclusivo em pasta temporária `barberapp-profile-probe-*`, diretamente nessa pasta. Não sobrescrever arquivos existentes.
- `perfil-recuperar`: somente Windows, após barreiras; lê o ciphertext daquela prova, usa o mesmo propósito e identidade da aplicação e informa apenas sucesso/rejeição. Não deve apagar o arquivo.

Formato dos argumentos: modo, confirmação, caminho do payload, content root existente, propósito fixo `BarberApp.MercadoPago.Tokens.v1`. O payload deve se chamar payload-protegido.txt e estar diretamente numa pasta barberapp-profile-probe-* filha do temporário do usuário. Não há argumento de pasta de chaves: preserva o mecanismo padrão do perfil. Não usa SetApplicationName, PersistKeysToFileSystem ou ProtectKeysWithDpapi para reconstruir manualmente o provider dessa prova.

Os modos de perfil compartilham a mesma configuração de factory, com keyring null e **AutoGenerateKeys false em todas as fases**, inclusive proteção e inspeção. Isso desabilita geração automática/rotação por criação de novas chaves; nenhuma API de criação/revogação/remoção é chamada. Se o provider não conseguir selecionar uma chave utilizável, a operação falha sem habilitar geração. Os modos originais com keyring fictício mantêm seu comportamento.

Na inspeção, XmlRepository/XmlEncryptor sem sobrescrita explícita não significam ausência de persistência ou criptografia: deixam o framework selecionar os padrões do ambiente. O log da API já informou perfil e DPAPI no Windows; o teste de opções não substitui essa evidência operacional.

## Barreiras antes de inicializar ou acessar material

1. Confirmar modo, confirmação explícita e propósito fixo.
2. Validar paths absolutos, content root existente, destino direto na pasta temporária exclusiva e recusar links/reparse points. Não criar content root ausente.
3. Em proteção, recusar arquivo já existente e usar FileMode.CreateNew/FileShare.None na escrita para evitar sobrescrita entre validação e criação. Em recuperação, exigir arquivo existente, sem link, e tamanho de até 16 KiB. Não ler conteúdo para produzir uma mensagem de validação.
4. Inspeção pode ser portável; operações criptográficas do perfil recusam plataforma diferente de Windows, antes de resolver o provider.
5. Só então montar o host de diagnóstico com fontes de configuração privadas excluídas, logs removidos e banco InMemory. Resolver opções/provider da API, sem substituto efêmero nem discriminador de teste.

Recusa de segurança: código 6 e JSON contendo apenas status específico. Modo desconhecido: código 2, não prova de barreira. Falha de infraestrutura: código 5, nunca tratada como rejeição criptográfica. Nenhum erro deve divulgar dados privados, plaintext, ciphertext ou conteúdo de XML.

## Sequência operacional futura — ainda não executar

1. Revisar os testes das barreiras; implementar e validar sem alterar os testes aprovados. Nenhum teste automático deste recorte deve proteger/recuperar usando o perfil real.
2. Revisar diff e riscos. Manter API parada e mesmo usuário Windows, SDK/runtime e content root da execução observada. Não mudar permissões/identidade nem desativar o Smart App Control.
3. Registrar somente metadados dos arquivos de chaves (nomes, tamanhos e última escrita) antes da prova, sem abrir XML, calcular hash do conteúdo ou publicar a listagem. Não copiar as chaves para WSL.
4. Criar a pasta temporária exclusiva para o ciphertext fictício. Rodar inspeção e conferir geração false. Se falhar, parar: não habilitar geração nem limpar keyring.
5. Rodar proteção em um processo; conferir saída/encerramento. Depois rodar recuperação em outro processo, com mesmo root e propósito. Não imprimir payload ou chaves.
6. Comparar metadados antes/depois. Se houver diferença, parar e investigar; outra aplicação do mesmo usuário pode ter usado o keyring compartilhado. Não apagar/reverter chaves para fazer a comparação passar.
7. Guardar somente resultado e hash de identidade no relatório. Limpar apenas o arquivo e a pasta temporária criados para a prova, após revisão. Não remover o keyring nem arquivos de outros projetos.

Ainda não há comandos operacionais de perfil neste documento porque o diff e a execução local aguardam revisão. Não adaptar os comandos antigos ao keyring real. Se houver falha ao gravar o ciphertext fictício, um arquivo incompleto pode permanecer; investigar antes de tentar novamente. Nenhum modo apaga o arquivo automaticamente.

## Cobertura desta especificação

Onze casos em WindowsProfileProtectionSafetyTests: inspeção de opções, três recusas por confirmação ausente, duas por propósito arbitrário, duas por destino não permitido, content root ausente, proteção sem sobrescrita e recuperação sem payload. Executados no WSL sem operação criptográfica no perfil Windows. As falhas iniciais por modo ausente eram da ferramenta, não RED funcional do BarberApp. Após aprovação, os mesmos onze casos passaram; suíte inteira: 212 aprovados (22 unitários + 190 integração), zero falhas/ignorados. Build sem avisos/erros; formatação dos quatro C# do recorte aprovada.

Restrição de plataforma e verificações de links/caminhos foram revisadas no código, não exercitadas por todos esses onze casos. CreateNew protege contra sobrescrever um arquivo criado concorrentemente; checagens prévias de diretórios não equivalem a sandbox resistente a um processo hostil da mesma identidade que altere diretórios durante a operação. A prova de não alteração do keyring real exige comparação operacional de metadados. Não declarar segurança absoluta ou backup validado por relatório de opções.
