START TRANSACTION;
CREATE UNIQUE INDEX `uq_revision_periodo` ON `revision` (`id_tesis`, `id_periodo`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261110003414_InformePorPeriodo', '10.0.14');

COMMIT;

