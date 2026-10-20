START TRANSACTION;
ALTER TABLE "ConexoesMercadoPago" ADD "RequerReautorizacao" boolean NOT NULL DEFAULT FALSE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261020213112_AddEstadoRecuperacaoMercadoPago', '10.0.5');

COMMIT;
