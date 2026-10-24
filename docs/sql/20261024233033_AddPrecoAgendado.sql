START TRANSACTION;
ALTER TABLE "Agendamentos" ADD "PrecoAgendado" numeric(10,2);

ALTER TABLE "Agendamentos" ADD CONSTRAINT "CK_Agendamentos_PrecoAgendadoPositivo" CHECK ("PrecoAgendado" IS NULL OR "PrecoAgendado" > 0);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261024233033_AddPrecoAgendado', '10.0.5');

COMMIT;
