# Testes no WSL

## Motivo e limites

Ambiente autorizado pelo mantenedor em 03/10/2026. O Windows bloqueou o carregamento da DLL de testes com `0x800711C7`. Instalamos Ubuntu 24.04 LTS e SDK .NET 10 no WSL, sem desativar Smart App Control nem modificar políticas de segurança.

Os testes rodam como usuário Linux `barberdev`, não como root. A cópia de trabalho fica em `/home/barberdev/barberapp-api`; o repositório original continua no Windows. Não edite fontes nas duas cópias: edite no repositório original e sincronize antes de testar.

## Sincronizar e executar

No PowerShell, a partir do repositório:

```powershell
wsl --distribution Ubuntu-24.04 --user barberdev --exec /bin/bash -lc 'mkdir -p /home/barberdev/barberapp-api && rsync -r --exclude=.git --exclude=bin --exclude=obj --exclude=.aws --exclude=.codex --exclude=.agents --exclude=.vscode --exclude=node_modules --exclude="*.lscache" --exclude=".env*" --exclude="appsettings.Development.json" /mnt/c/Users/joset/www/barberapp-api/ /home/barberdev/barberapp-api/'

wsl --distribution Ubuntu-24.04 --user barberdev --exec /bin/bash -lc 'cd /home/barberdev/barberapp-api && dotnet test BarberApp.slnx'
```

O primeiro teste restaura dependências; depois é possível usar `--no-restore`. A maior parte dos testes usa configuração fictícia em memória e banco InMemory. O teste específico de migration PostgreSQL exige o ambiente isolado abaixo; sem configuração, ele é explicitamente ignorado (não é prova de persistência PostgreSQL).

A sincronização não copia `.git`, artefatos Windows ou os arquivos locais de segredos indicados. Isso não substitui uma auditoria de outros arquivos: não acrescente credenciais a arquivos versionados. Sem `--delete`, arquivos removidos no Windows podem permanecer na cópia Linux; confira esse risco antes de validar remoções/renomeações.

## Evidência inicial

- SDK instalado: 10.0.112, runtime 10.0.12.
- Recorte `MercadoPagoConnectionTests`: 15 casos executados; 14 falhas esperadas por endpoint inexistente (`404`), 1 aprovado pela barreira global de API key.
- Esse é RED válido: a DLL foi carregada e as asserções executadas. Não é GREEN nem significa que a conexão Mercado Pago esteja implementada.
- Nenhum segredo real foi necessário e nenhum pagamento foi realizado.

## PostgreSQL isolado — 05/10/2026

Instalados PostgreSQL 16 e CLI EF 10.0.5 no Ubuntu de testes, com autorização de execução. O banco `barberapp_test_mercadopago_20261005` pertence ao usuário Linux/DB `barberdev`; usa socket Unix e autenticação peer, sem senha versionada ou conexão ao banco Windows. O teste verifica prefixo do nome e host Unix antes de migrar. Os dados de teste são revertidos por transação; schema/migrations permanecem nesse banco isolado.

```powershell
wsl --distribution Ubuntu-24.04 --user barberdev --exec /bin/bash -lc 'cd /home/barberdev/barberapp-api && env BARBERAPP_TEST_POSTGRES="Host=/var/run/postgresql;Database=barberapp_test_mercadopago_20261005;Username=barberdev" dotnet test BarberApp.slnx --no-restore'
```

Sincronize antes de executar. Sem `BARBERAPP_TEST_POSTGRES`, o teste PostgreSQL será ignorado; reporte esse fato e não declare o gate de migration validado. A execução com a variável passou em 125 casos, zero falhas/ignorados. Nenhuma migration foi aplicada ao banco real da aplicação; revise seu SQL e o ambiente antes de aplicar manualmente.

## Aprendizado

Compilar gera a DLL; executar testes exige que o sistema permita carregá-la. O erro de política do Windows era uma falha de ambiente, não um teste demonstrando falta de comportamento. No Ubuntu, os mesmos testes chegaram às asserções: agora podemos seguir TDD e implementar o mínimo.
