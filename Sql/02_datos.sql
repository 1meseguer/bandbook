-- ejecutar despues de 01_esquema.sql
USE bandflow;
SET SQL_SAFE_UPDATES = 0;

INSERT INTO Categorias (nombre_categoria, precio_base) VALUES
 ('Local', 3000.00), ('Emergente', 6000.00), ('Estelar', 15000.00);

INSERT INTO Bandas (nombre_banda, genero_musical, porcentaje_comision, id_categoria, contacto_principal) VALUES
 ('Ruido Blanco',      'Post-hardcore', 10.00, 1, 'ruidoblanco@mail.com'),
 ('Cicatriz Azul',     'Post-hardcore', 12.00, 2, '646-555-0101'),
 ('Marea Negra',       'Metal',         15.00, 3, '646-555-0102'),
 ('Hijos del Eco',     'Rock',          10.00, 1, 'hijosdeleco@mail.com'),
 ('Paredes de Humo',   'Rock',          12.00, 2, '646-555-0103'),
 ('Caída Libre',       'Punk',          10.00, 1, '646-555-0104'),
 ('Vértigo Sur',       'Metal',         15.00, 3, 'vertigosur@mail.com'),
 ('Anónimo 99',        'Post-hardcore', 12.00, 2, '646-555-0105');

INSERT INTO Promotores (nombre, telefono, correo) VALUES
 ('Foro Underground Ensenada', '646-555-1001', 'foro@underground.mx'),
 ('Producciones Costa Brava',  '646-555-1002', 'costabrava@mail.com'),
 ('Café Cultural La Grieta',   '646-555-1003', 'lagrieta@mail.com'),
 ('Marcos Villalobos',         '664-555-1004', 'marcos.v@mail.com'),
 ('Festival Mar y Ruido',      '646-555-1005', 'marruido@mail.com');

INSERT INTO Catalogo_Extras (nombre_extra, precio_sugerido) VALUES
 ('Renta de amplificador', 800.00),
 ('Operador de sonido extra', 1200.00),
 ('Backline completo', 2500.00),
 ('Staff técnico (por persona)', 600.00),
 ('Transporte de equipo', 900.00);

-- cotizaciones: el costo_base se copia de la tarifa de la categoria (instantanea)
INSERT INTO Cotizaciones (id_banda, id_promotor, fecha_inicio, duracion_minutos, costo_base, incluye_luces, costo_luces, estado_cotizacion)
SELECT b.id_banda, p.id_promotor, x.fi, x.dur, c.precio_base, x.luces, x.costo_luces, x.estado
FROM (
  SELECT 'Ruido Blanco' AS banda, 'foro@underground.mx' AS prom, '2026-12-05 20:00:00' AS fi, 90 AS dur, TRUE AS luces, 1500.00 AS costo_luces, 'Aprobada'  AS estado UNION ALL
  SELECT 'Cicatriz Azul',  'costabrava@mail.com', '2026-12-05 21:00:00', 120, FALSE, 0.00,    'Aprobada'  UNION ALL
  SELECT 'Marea Negra',    'marruido@mail.com',   '2026-12-12 22:00:00', 120, TRUE,  3000.00, 'Aprobada'  UNION ALL
  SELECT 'Hijos del Eco',  'lagrieta@mail.com',   '2026-12-06 19:00:00', 60,  FALSE, 0.00,    'Pendiente' UNION ALL
  SELECT 'Paredes de Humo','marcos.v@mail.com',   '2026-12-13 20:00:00', 90,  TRUE,  1800.00, 'Pendiente' UNION ALL
  SELECT 'Caída Libre',    'foro@underground.mx', '2026-12-19 20:00:00', 60,  FALSE, 0.00,    'Rechazada' UNION ALL
  SELECT 'Vértigo Sur',    'marruido@mail.com',   '2026-12-20 22:00:00', 120, TRUE,  3500.00, 'Aprobada'  UNION ALL
  SELECT 'Anónimo 99',     'costabrava@mail.com', '2027-01-09 21:00:00', 90,  TRUE,  1500.00, 'Pendiente' UNION ALL
  SELECT 'Ruido Blanco',   'lagrieta@mail.com',   '2027-01-16 20:00:00', 60,  FALSE, 0.00,    'Pendiente' UNION ALL
  SELECT 'Cicatriz Azul',  'foro@underground.mx', '2027-01-23 21:00:00', 120, TRUE,  2000.00, 'Aprobada'
) x
JOIN Bandas b ON b.nombre_banda = x.banda
JOIN Categorias c ON c.id_categoria = b.id_categoria
JOIN Promotores p ON p.correo = x.prom;

-- extras contratados (precio_unitario es instantanea del precio al cotizar)
INSERT INTO Cotizacion_Extras (id_cotizacion, id_extra, cantidad, precio_unitario) VALUES
 (1, 1, 1, 800.00), (1, 4, 2, 600.00),
 (2, 2, 1, 1200.00),
 (3, 3, 1, 2500.00), (3, 5, 1, 900.00),
 (5, 1, 2, 800.00),
 (7, 3, 1, 2500.00), (7, 4, 3, 600.00),
 (8, 5, 1, 900.00),
 (10, 2, 1, 1200.00);

-- calculo inicial de montos derivados (la funcion fn_calcular_total se crea en 04)
UPDATE Cotizaciones c
JOIN Bandas b ON b.id_banda = c.id_banda
SET c.monto_comision_banda = ROUND(c.costo_base * b.porcentaje_comision / 100, 2),
    c.monto_extras_manager = COALESCE((SELECT SUM(ce.cantidad * ce.precio_unitario)
                                       FROM Cotizacion_Extras ce
                                       WHERE ce.id_cotizacion = c.id_cotizacion), 0)
WHERE c.id_cotizacion > 0;
UPDATE Cotizaciones
SET costo_total = costo_base + costo_luces + monto_extras_manager
WHERE id_cotizacion > 0;

-- eventos en firme para las cotizaciones aprobadas (1, 2, 3, 7, 10)
INSERT INTO Eventos (id_cotizacion, lugar_evento, google_event_id, estado_evento) VALUES
 (1,  'Foro Underground, Zona Centro, Ensenada',      'gcal_evt_0001', 'Programado'),
 (2,  'Salón Costa Brava, Blvd. Costero, Ensenada',   'gcal_evt_0002', 'Programado'),
 (3,  'Explanada Riviera, Ensenada',                  'gcal_evt_0003', 'Programado'),
 (7,  'Explanada Riviera, Ensenada',                  NULL,            'Programado'),
 (10, 'Foro Underground, Zona Centro, Ensenada',      NULL,            'Programado');
