START TRANSACTION;
CREATE TABLE "ConexoesMercadoPago" (
    "Id" integer NOT NULL,
    "ContaId" bigint NOT NULL,
    "AdministradorId" character varying(450) NOT NULL,
    "TokensProtegidos" text NOT NULL,
    "ExpiraEm" timestamp with time zone NOT NULL,
    "Sandbox" boolean NOT NULL,
    CONSTRAINT "PK_ConexoesMercadoPago" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_ConexoesMercadoPago_UnicaInstalacao" CHECK ("Id" = 1)
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261005172851_AddConexaoMercadoPago', '10.0.5');

COMMIT;
