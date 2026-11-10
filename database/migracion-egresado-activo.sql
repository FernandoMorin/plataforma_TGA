START TRANSACTION;
ALTER TABLE `egresado` ADD `activo` tinyint(1) NOT NULL DEFAULT TRUE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261110031844_EgresadoActivo', '10.0.14');

COMMIT;

