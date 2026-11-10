START TRANSACTION;
ALTER TABLE `tesis` DROP COLUMN `linea_investigacion`;

ALTER TABLE `tesis` ADD `id_linea_investigacion` int NULL;

CREATE TABLE `CAT_linea_investigacion` (
    `id_linea_investigacion` int NOT NULL AUTO_INCREMENT,
    `nombre` varchar(150) NOT NULL,
    `activo` tinyint(1) NOT NULL DEFAULT TRUE,
    PRIMARY KEY (`id_linea_investigacion`)
);

INSERT INTO `CAT_linea_investigacion` (`id_linea_investigacion`, `activo`, `nombre`)
VALUES (1, TRUE, 'Tecnología y gestión del agua');
SELECT ROW_COUNT();


CREATE INDEX `IX_tesis_id_linea_investigacion` ON `tesis` (`id_linea_investigacion`);

ALTER TABLE `tesis` ADD CONSTRAINT `fk_tesis_linea` FOREIGN KEY (`id_linea_investigacion`) REFERENCES `CAT_linea_investigacion` (`id_linea_investigacion`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261110002808_LineaInvestigacion', '10.0.14');

COMMIT;

