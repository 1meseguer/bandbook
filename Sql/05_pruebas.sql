-- pruebas validas e invalidas, correr con: mysql -u root -p --force < 05_pruebas.sql
USE bandflow;

SELECT 'P1 VALIDO: registrar cotizacion (Hijos del Eco, fecha futura)' AS prueba;
CALL sp_registrar_cotizacion(4, 3, '2027-03-06 20:00:00', 90, TRUE, 1200.00, @id_nueva);
SELECT @id_nueva AS id_nueva;
SELECT id_cotizacion, costo_base, costo_luces, monto_comision_banda, costo_total, estado_cotizacion
FROM Cotizaciones WHERE id_cotizacion = @id_nueva;

SELECT 'P2 INVALIDO: fecha en el pasado (espera error)' AS prueba;
CALL sp_registrar_cotizacion(4, 3, '2020-01-01 20:00:00', 90, FALSE, 0, @x);

SELECT 'P3 INVALIDO: banda inexistente (espera error)' AS prueba;
CALL sp_registrar_cotizacion(999, 3, '2027-03-07 20:00:00', 90, FALSE, 0, @x);

SELECT 'P4 VALIDO: agregar extra y recalcular total (funcion)' AS prueba;
CALL sp_agregar_extra(@id_nueva, 1, 2);
SELECT id_cotizacion, monto_extras_manager, costo_total, fn_calcular_total(id_cotizacion) AS total_funcion
FROM Cotizaciones WHERE id_cotizacion = @id_nueva;

SELECT 'P5 INVALIDO: agregar extra a cotizacion ya aprobada (espera error)' AS prueba;
CALL sp_agregar_extra(1, 1, 1);

SELECT 'P6 VALIDO: aprobar cotizacion y crear evento (transaccion + trigger auditoria)' AS prueba;
CALL sp_aprobar_cotizacion(@id_nueva, 'Café Cultural La Grieta, Ensenada');
SELECT id_cotizacion, estado_cotizacion FROM Cotizaciones WHERE id_cotizacion = @id_nueva;
SELECT id_evento, id_cotizacion, lugar_evento, estado_evento FROM Eventos WHERE id_cotizacion = @id_nueva;
SELECT * FROM Auditoria_Cotizaciones WHERE id_cotizacion = @id_nueva;

SELECT 'P7 INVALIDO: aprobar una cotizacion de Ruido Blanco traslapada con otra ya aprobada (espera error y ROLLBACK)' AS prueba;
CALL sp_registrar_cotizacion(1, 2, '2027-03-20 20:00:00', 90, FALSE, 0, @id_a);
CALL sp_registrar_cotizacion(1, 5, '2027-03-20 21:00:00', 90, FALSE, 0, @id_b);
CALL sp_aprobar_cotizacion(@id_a, 'Foro Underground');
CALL sp_aprobar_cotizacion(@id_b, 'Salón Costa Brava');  -- debe fallar por traslape
SELECT id_cotizacion, estado_cotizacion FROM Cotizaciones WHERE id_cotizacion IN (@id_a, @id_b);
SELECT COUNT(*) AS eventos_de_b FROM Eventos WHERE id_cotizacion = @id_b;  -- debe ser 0 (rollback)

SELECT 'P8 INVALIDO: restricciones CHECK / UNIQUE / FK (cada una espera error)' AS prueba;
INSERT INTO Bandas (nombre_banda, genero_musical, porcentaje_comision, id_categoria, contacto_principal)
VALUES ('Banda X', 'Rock', 150, 1, 'x@mail.com');  -- check comision
INSERT INTO Bandas (nombre_banda, genero_musical, porcentaje_comision, id_categoria, contacto_principal)
VALUES ('Ruido Blanco', 'Rock', 10, 1, 'x@mail.com');  -- unique nombre
INSERT INTO Bandas (nombre_banda, genero_musical, porcentaje_comision, id_categoria, contacto_principal)
VALUES ('Banda Y', 'Rock', 10, 99, 'y@mail.com');  -- fk categoria
UPDATE Cotizaciones SET estado_cotizacion = 'Cancelada' WHERE id_cotizacion = 4;  -- check estado
DELETE FROM Categorias WHERE id_categoria = 1;  -- fk restrict

SELECT 'P9 TRANSACCION con COMMIT' AS prueba;
START TRANSACTION;
  UPDATE Categorias SET precio_base = precio_base + 500 WHERE nombre_categoria = 'Local';
COMMIT;
SELECT nombre_categoria, precio_base FROM Categorias WHERE nombre_categoria = 'Local';

SELECT 'P10 TRANSACCION con ROLLBACK' AS prueba;
START TRANSACTION;
  UPDATE Categorias SET precio_base = 0 WHERE nombre_categoria = 'Local';
  SELECT nombre_categoria, precio_base AS precio_dentro_de_transaccion FROM Categorias WHERE nombre_categoria = 'Local';
ROLLBACK;
SELECT nombre_categoria, precio_base AS precio_despues_de_rollback FROM Categorias WHERE nombre_categoria = 'Local';

SELECT 'P11 Historial: cambio de tarifa NO altera cotizaciones viejas' AS prueba;
SELECT id_cotizacion, costo_base FROM Cotizaciones WHERE id_cotizacion = 1;

SELECT 'P12 Auditoria completa' AS prueba;
SELECT id_cotizacion, estado_anterior, estado_nuevo, usuario_bd FROM Auditoria_Cotizaciones ORDER BY id_auditoria;
