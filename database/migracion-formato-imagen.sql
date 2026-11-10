START TRANSACTION;
INSERT INTO `CAT_formato_reporte` (`id_formato_reporte`, `nombre`)
VALUES (3, 'Imagen');
SELECT ROW_COUNT();


INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261110035055_FormatoImagen', '10.0.14');

COMMIT;

