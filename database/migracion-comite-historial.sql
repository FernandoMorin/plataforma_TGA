START TRANSACTION;
CREATE TABLE `comite_historial` (
    `id_historial` int NOT NULL AUTO_INCREMENT,
    `id_comite` int NOT NULL,
    `fecha` date NOT NULL,
    `accion` varchar(20) NOT NULL,
    `rpe_sale` varchar(10) NULL,
    `rpe_entra` varchar(10) NULL,
    `rol_anterior` int NULL,
    `rol_nuevo` int NULL,
    `id_usuario` int NULL,
    `fecha_registro` datetime NOT NULL,
    PRIMARY KEY (`id_historial`),
    CONSTRAINT `ck_historial_accion` CHECK (accion IN ('Alta', 'Baja', 'Reemplazo', 'Cambio de rol')),
    CONSTRAINT `fk_historial_comite` FOREIGN KEY (`id_comite`) REFERENCES `comite` (`id_comite`) ON DELETE CASCADE,
    CONSTRAINT `fk_historial_entra` FOREIGN KEY (`rpe_entra`) REFERENCES `profesor` (`rpe`),
    CONSTRAINT `fk_historial_rol_anterior` FOREIGN KEY (`rol_anterior`) REFERENCES `CAT_rol_comite` (`id_rol_comite`),
    CONSTRAINT `fk_historial_rol_nuevo` FOREIGN KEY (`rol_nuevo`) REFERENCES `CAT_rol_comite` (`id_rol_comite`),
    CONSTRAINT `fk_historial_sale` FOREIGN KEY (`rpe_sale`) REFERENCES `profesor` (`rpe`),
    CONSTRAINT `fk_historial_usuario` FOREIGN KEY (`id_usuario`) REFERENCES `usuario` (`id_usuario`)
);

CREATE INDEX `IX_comite_historial_id_comite` ON `comite_historial` (`id_comite`);

CREATE INDEX `IX_comite_historial_id_usuario` ON `comite_historial` (`id_usuario`);

CREATE INDEX `IX_comite_historial_rol_anterior` ON `comite_historial` (`rol_anterior`);

CREATE INDEX `IX_comite_historial_rol_nuevo` ON `comite_historial` (`rol_nuevo`);

CREATE INDEX `IX_comite_historial_rpe_entra` ON `comite_historial` (`rpe_entra`);

CREATE INDEX `IX_comite_historial_rpe_sale` ON `comite_historial` (`rpe_sale`);

INSERT INTO comite_historial (id_comite, fecha, accion, rpe_entra, rol_nuevo, fecha_registro)
                SELECT id_comite, COALESCE(fecha_asignacion, CURDATE()), 'Alta', rpe, id_rol_comite, NOW() FROM comite_profesor ORDER BY id_comite_profesor;

INSERT INTO comite_historial (id_comite, fecha, accion, rpe_sale, rol_anterior, fecha_registro)
                SELECT id_comite, fecha_baja, 'Baja', rpe, id_rol_comite, NOW() FROM comite_profesor WHERE fecha_baja IS NOT NULL ORDER BY id_comite_profesor;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261110030942_ComiteHistorial', '10.0.14');

COMMIT;

