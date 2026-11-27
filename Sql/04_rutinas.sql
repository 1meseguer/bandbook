-- funcion, procedimientos y triggers (ejecutar despues de 03)
-- los triggers solo validan y auditan, los montos los calculan la funcion y los procedimientos
USE bandflow;
DROP TRIGGER IF EXISTS trg_cot_bu_validar_aprobacion;
DROP TRIGGER IF EXISTS trg_cot_au_auditoria;
DROP PROCEDURE IF EXISTS sp_registrar_cotizacion;
DROP PROCEDURE IF EXISTS sp_agregar_extra;
DROP PROCEDURE IF EXISTS sp_aprobar_cotizacion;
DROP FUNCTION  IF EXISTS fn_calcular_total;

DELIMITER $$

-- funcion: total = costo_base + costo_luces + suma de extras
CREATE FUNCTION fn_calcular_total(p_id_cotizacion INT) RETURNS DECIMAL(10,2)
READS SQL DATA
BEGIN
  DECLARE v_total DECIMAL(10,2);
  SELECT c.costo_base + c.costo_luces + COALESCE(
           (SELECT SUM(ce.cantidad * ce.precio_unitario)
            FROM Cotizacion_Extras ce WHERE ce.id_cotizacion = c.id_cotizacion), 0)
    INTO v_total
  FROM Cotizaciones c WHERE c.id_cotizacion = p_id_cotizacion;
  RETURN v_total;  -- null si la cotizacion no existe
END$$

-- trigger 1 (regla de negocio)
-- antes de aprobar, revisa que la banda no tenga otra aprobada en ese horario
CREATE TRIGGER trg_cot_bu_validar_aprobacion
BEFORE UPDATE ON Cotizaciones
FOR EACH ROW
BEGIN
  DECLARE v_traslapes INT DEFAULT 0;
  IF NEW.estado_cotizacion = 'Aprobada' AND OLD.estado_cotizacion <> 'Aprobada' THEN
    SELECT COUNT(*) INTO v_traslapes
    FROM Cotizaciones c
    WHERE c.id_banda = NEW.id_banda
      AND c.id_cotizacion <> NEW.id_cotizacion
      AND c.estado_cotizacion = 'Aprobada'
      AND c.fecha_inicio < DATE_ADD(NEW.fecha_inicio, INTERVAL NEW.duracion_minutos MINUTE)
      AND c.fecha_fin    > NEW.fecha_inicio;
    IF v_traslapes > 0 THEN
      SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'La banda ya tiene una cotizacion aprobada en ese horario';
    END IF;
  END IF;
END$$

-- trigger 2 (auditoria)
-- guarda cada cambio de estado en la bitacora
CREATE TRIGGER trg_cot_au_auditoria
AFTER UPDATE ON Cotizaciones
FOR EACH ROW
BEGIN
  IF NEW.estado_cotizacion <> OLD.estado_cotizacion THEN
    INSERT INTO Auditoria_Cotizaciones (id_cotizacion, estado_anterior, estado_nuevo, usuario_bd)
    VALUES (NEW.id_cotizacion, OLD.estado_cotizacion, NEW.estado_cotizacion, CURRENT_USER());
  END IF;
END$$

-- procedimiento 1: registra una cotizacion nueva (estado pendiente).
-- copia la tarifa de la categoria y calcula comision y total
CREATE PROCEDURE sp_registrar_cotizacion(
  IN  p_id_banda INT, IN p_id_promotor INT, IN p_fecha_inicio DATETIME,
  IN  p_duracion_min INT, IN p_incluye_luces BOOLEAN, IN p_costo_luces DECIMAL(10,2),
  OUT p_id_cotizacion INT)
BEGIN
  DECLARE v_base DECIMAL(10,2);
  DECLARE v_pct  DECIMAL(5,2);
  DECLARE v_luces DECIMAL(10,2);

  SELECT cat.precio_base, b.porcentaje_comision INTO v_base, v_pct
  FROM Bandas b JOIN Categorias cat ON cat.id_categoria = b.id_categoria
  WHERE b.id_banda = p_id_banda AND b.activa = TRUE;

  IF v_base IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La banda no existe o no esta activa';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM Promotores WHERE id_promotor = p_id_promotor) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El promotor no existe';
  END IF;
  IF p_fecha_inicio <= NOW() THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La fecha solicitada debe ser futura';
  END IF;

  SET v_luces = IF(p_incluye_luces, COALESCE(p_costo_luces, 0), 0);

  INSERT INTO Cotizaciones (id_banda, id_promotor, fecha_inicio, duracion_minutos, costo_base,
                            incluye_luces, costo_luces, monto_comision_banda, costo_total)
  VALUES (p_id_banda, p_id_promotor, p_fecha_inicio, p_duracion_min, v_base,
          p_incluye_luces, v_luces, ROUND(v_base * v_pct / 100, 2), v_base + v_luces);
  SET p_id_cotizacion = LAST_INSERT_ID();
END$$

-- procedimiento 2: agrega un extra a una cotizacion pendiente y recalcula montos
CREATE PROCEDURE sp_agregar_extra(IN p_id_cotizacion INT, IN p_id_extra INT, IN p_cantidad INT)
BEGIN
  DECLARE v_estado VARCHAR(20);
  DECLARE v_precio DECIMAL(10,2);
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT estado_cotizacion INTO v_estado FROM Cotizaciones WHERE id_cotizacion = p_id_cotizacion;
  SELECT precio_sugerido INTO v_precio FROM Catalogo_Extras WHERE id_extra = p_id_extra;

  IF v_estado IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cotizacion no existe';
  ELSEIF v_estado <> 'Pendiente' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Solo se pueden modificar cotizaciones pendientes';
  ELSEIF v_precio IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El extra no existe en el catalogo';
  ELSEIF p_cantidad IS NULL OR p_cantidad <= 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cantidad debe ser mayor a cero';
  END IF;

  START TRANSACTION;
    INSERT INTO Cotizacion_Extras (id_cotizacion, id_extra, cantidad, precio_unitario)
    VALUES (p_id_cotizacion, p_id_extra, p_cantidad, v_precio)
    ON DUPLICATE KEY UPDATE cantidad = cantidad + VALUES(cantidad);

    UPDATE Cotizaciones
    SET monto_extras_manager = (SELECT SUM(cantidad * precio_unitario)
                                FROM Cotizacion_Extras WHERE id_cotizacion = p_id_cotizacion),
        costo_total = fn_calcular_total(p_id_cotizacion)
    WHERE id_cotizacion = p_id_cotizacion;
  COMMIT;
END$$

-- procedimiento 3: aprueba la cotizacion y crea el evento, si hay traslape se revierte todo
CREATE PROCEDURE sp_aprobar_cotizacion(IN p_id_cotizacion INT, IN p_lugar VARCHAR(200))
BEGIN
  DECLARE v_estado VARCHAR(20);
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT estado_cotizacion INTO v_estado FROM Cotizaciones WHERE id_cotizacion = p_id_cotizacion;
  IF v_estado IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cotizacion no existe';
  ELSEIF v_estado <> 'Pendiente' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Solo se pueden aprobar cotizaciones pendientes';
  ELSEIF p_lugar IS NULL OR TRIM(p_lugar) = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El lugar del evento es obligatorio';
  END IF;

  START TRANSACTION;
    UPDATE Cotizaciones SET estado_cotizacion = 'Aprobada' WHERE id_cotizacion = p_id_cotizacion;
    INSERT INTO Eventos (id_cotizacion, lugar_evento) VALUES (p_id_cotizacion, p_lugar);
  COMMIT;
END$$

DELIMITER ;
