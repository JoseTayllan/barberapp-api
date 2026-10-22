# Prova local do keyring Windows — procedimento e registro

Estado registrado em 22/10/2026: **barreiras aprovadas/publicadas em d0bdd68 e prova local executada pelo mantenedor, com autorizações separadas para inspeção e criptografia**. Texto fictício protegido e recuperado em chamadas separadas no Windows; duas chaves antes/depois e zero diferenças nos metadados observados. Evidência fornecida pelo mantenedor, não execução do agente. A suíte anterior no WSL teve 212 aprovados, zero falhas/ignorados; não foi repetida nesta atualização apenas documental. Os modos antigos proteger/api-proteger podem gerar chaves e não devem ser apontados para o perfil real.

## Objetivo e limite

Proteger um texto fixo explicitamente fictício, encerrar o processo e recuperá-lo em outro processo Windows, usando o provider do contêiner inicializado pelo Program.cs, o content root da instalação e o keyring padrão do perfil. Não conectar PostgreSQL, ler User Secrets, criar endpoint, chamar Mercado Pago ou efetuar pagamento. O host de diagnóstico deve continuar usando bootstrap desabilitado e configurações/banco fictícios.

O resultado relatado demonstra recuperação do texto fictício nesse contexto de máquina/usuário/root e versão. Não comprova restauração em outra máquina, crash de disco, backup de identidade DPAPI, validade remota dos tokens ou prontidão para pagamentos. O host continua com dependências/configuração fictícias: não é uma jornada end-to-end de tokens reais da aplicação.

## Evidência operacional fornecida pelo mantenedor

1. Inspeção retornou Inspecionado e código 0: geracaoAutomatica false, repositorioExplicito false e criptografiaExplicita false. Hash do discriminador: `151FD836BBDE1D2597700D4C2183C013D39E8C0D76B1446F4F29DFB2595A0E5F`. Esse hash identifica o contexto da aplicação, não uma chave secreta.
2. Uma tentativa inicial retornou DestinoJaExiste; o arquivo não foi sobrescrito. Recuperação posterior leu um arquivo já existente, mas sua origem não foi estabelecida. Essa sequência preliminar não foi contada como prova nova de proteção/recuperação.
3. Criada outra pasta temporária exclusiva. Test-Path do novo payload retornou False; metadados anteriores registraram duas chaves.
4. Nova chamada perfil-proteger retornou Protegido, processo 25756, com o mesmo hash da inspeção. O código de saída dessa chamada não foi reenviado pelo mantenedor.
5. Na chamada posterior, separada, perfil-recuperar retornou `Recuperado` e código 0, conforme relato do mantenedor. O JSON completo com PID/hash dessa última recuperação não foi reenviado; não inferir esses valores.
6. Comparação final informada: duas chaves depois, zero diferenças em Name, Length e LastWriteTimeUtc.Ticks. Não houve leitura manual, cópia ou hash do conteúdo dos XMLs pelo agente.

Zero diferenças significa apenas **nenhuma alteração detectada nesses metadados**, não identidade bit a bit, auditoria de todas as operações ou garantia contra alteração de conteúdo com metadados preservados. Nenhum token real, chamada ao Mercado Pago ou pagamento foi usado nessa prova.

Os artefatos temporários foram preservados, inclusive o arquivo de origem não estabelecida. Não foi executada limpeza nesta atualização; qualquer remoção futura exige identificar exatamente os arquivos/pastas da prova, sem tocar no keyring ou em arquivos de outros projetos.

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

## Sequência revisada para uma nova prova — não repetir sem necessidade

1. Revisar os testes das barreiras; implementar e validar sem alterar os testes aprovados. Nenhum teste automático deste recorte deve proteger/recuperar usando o perfil real.
2. Revisar diff e riscos. Manter API parada e mesmo usuário Windows, SDK/runtime e content root da execução observada. Não mudar permissões/identidade nem desativar o Smart App Control.
3. Registrar somente metadados dos arquivos de chaves (nomes, tamanhos e última escrita) antes da prova, sem abrir XML, calcular hash do conteúdo ou publicar a listagem. Não copiar as chaves para WSL.
4. Criar a pasta temporária exclusiva para o ciphertext fictício. Rodar inspeção e conferir geração false. Se falhar, parar: não habilitar geração nem limpar keyring.
5. Rodar proteção em um processo; conferir saída/encerramento. Depois rodar recuperação em outro processo, com mesmo root e propósito. Não imprimir payload ou chaves.
6. Comparar metadados antes/depois. Se houver diferença, parar e investigar; outra aplicação do mesmo usuário pode ter usado o keyring compartilhado. Não apagar/reverter chaves para fazer a comparação passar.
7. Guardar somente resultado e hash de identidade no relatório. Limpar apenas o arquivo e a pasta temporária criados para a prova, após revisão. Não remover o keyring nem arquivos de outros projetos.

A execução registrada foi orientada no chat, um passo por vez, após revisão e autorizações. Este documento não é uma automação para repetir a operação. No PowerShell interativo, throw encerra o bloco atual, não impede o operador de executar outro comando manualmente: diante de falha, parar e revisar a saída antes de continuar. Não adaptar os modos antigos ao keyring real. Se houver falha ao gravar o ciphertext fictício, um arquivo incompleto pode permanecer; investigar antes de tentar novamente. Nenhum modo apaga o arquivo automaticamente.

## Cobertura desta especificação

Onze casos em WindowsProfileProtectionSafetyTests: inspeção de opções, três recusas por confirmação ausente, duas por propósito arbitrário, duas por destino não permitido, content root ausente, proteção sem sobrescrita e recuperação sem payload. Executados no WSL sem operação criptográfica no perfil Windows. As falhas iniciais por modo ausente eram da ferramenta, não RED funcional do BarberApp. Após aprovação, os mesmos onze casos passaram; suíte inteira: 212 aprovados (22 unitários + 190 integração), zero falhas/ignorados. Build sem avisos/erros; formatação dos quatro C# do recorte aprovada.

Restrição de plataforma e verificações de links/caminhos foram revisadas no código, não exercitadas por todos esses onze casos. CreateNew protege contra sobrescrever um arquivo criado concorrentemente; checagens prévias de diretórios não equivalem a sandbox resistente a um processo hostil da mesma identidade que altere diretórios durante a operação. A comparação operacional não detectou diferenças nos metadados, dentro do limite descrito acima. Backup/restauração das chaves e identidade DPAPI, mudança de máquina/usuário/root e recuperação de crash continuam pendentes. Não declarar segurança absoluta ou backup validado por esse resultado.
