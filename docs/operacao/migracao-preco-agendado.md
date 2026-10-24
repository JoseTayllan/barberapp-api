# Migration do preço combinado — revisão antes de aplicar

## Estado

Migration `20261024233033_AddPrecoAgendado` gerada após aprovação dos testes. Aplicada somente pelo teste automatizado ao PostgreSQL isolado WSL. O banco da aplicação não foi atualizado. Este documento e o SQL aguardam revisão; não interpretar a aprovação da implementação como autorização automática para operar o banco local.

SQL: [20261024233033_AddPrecoAgendado.sql](../sql/20261024233033_AddPrecoAgendado.sql). Baseline esperado: `20261020213112_AddEstadoRecuperacaoMercadoPago`, além das migrations anteriores, já presentes no banco local conforme prova anterior do mantenedor. Conferir novamente ambiente/histórico antes de executar.

## O que muda

- Coluna `PrecoAgendado` nullable `numeric(10,2)` em `Agendamentos`, sem default nem backfill. Reservas antigas continuam com preço histórico desconhecido/null.
- Constraint `CK_Agendamentos_PrecoAgendadoPositivo`: null ou valor maior que zero. Não recalcula reservas e não altera a tabela de serviços.
- Registro da migration no histórico EF, na mesma transação. Script não idempotente: não executar duas vezes nem ocultar erro para tentar novamente.

O teste PostgreSQL conferiu tipo/precisão/nulidade/default, inserção legada omitindo coluna, valor com centavos lido em outro contexto e rejeição de zero/negativo. Dados do caso foram revertidos; schema de testes mantido. Não houve ensaio com registros históricos anteriores à migration, commit durável ou crash. O SQL não contém UPDATE que invente preços históricos.

## Antes da operação local

1. Revisar diff e SQL. Confirmar explicitamente banco/usuário/host/histórico; nunca imprimir senha/connection string privada.
2. Manter backup de banco fora do Git, conferir que é legível e preservar o contexto de recuperação já validado. Não apagar backups/chaves como parte desta operação.
3. Parar a API e escolher janela apropriada: ALTER TABLE/constraint podem adquirir locks e validar dados. Não prometer zero downtime.
4. Aplicar somente após aprovação operacional separada, com parada em erro e conferência do código de saída/COMMIT. Não há execução automática por este incremento no banco da aplicação.
5. Conferir coluna/constraint/histórico antes de iniciar API recompilada. O código novo consulta essa coluna; rodar contra schema antigo pode falhar. Conferir criação de reserva e campo novo sem expor dados pessoais.

## Rollback e limites

Down remove constraint e coluna: perde valores combinados que tiverem sido registrados. Restaurar versão antiga da API também pode voltar a cobrar preço atual no mock. Não executar rollback automaticamente; avaliar dados, backup e política antes de uma decisão explícita.

O mock continua simulado: reserva nova usa snapshot; legado null mantém cálculo anterior somente nesse fluxo. Não usar esse fallback como autorização para cobrar dinheiro real de reserva sem preço histórico. Checkout real, regularização de legados, frontend exibindo preço combinado, revogação remota e lint global permanecem pendentes.
