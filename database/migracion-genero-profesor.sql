USE `plataforma_TGA`;

START TRANSACTION;

ALTER TABLE `profesor` ADD `genero` varchar(10) NOT NULL DEFAULT 'Masculino';
ALTER TABLE `profesor` ADD CONSTRAINT `ck_profesor_genero` CHECK (`genero` IN ('Masculino', 'Femenino'));

-- Actualizar a profesoras existentes según su nombre
SET SQL_SAFE_UPDATES = 0;
UPDATE `profesor` SET `genero` = 'Femenino' WHERE `nombre` IN ('Ana', 'Laura');
SET SQL_SAFE_UPDATES = 1;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261124223000_GeneroProfesor', '10.0.14');

COMMIT;
