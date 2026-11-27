-- vistas y consultas, ejecutar despues de 02
USE bandflow;

-- vistas
-- v1: agenda de eventos confirmados (une 5 tablas)
CREATE OR REPLACE VIEW v_agenda_eventos AS
SELECT e.id_evento, b.nombre_banda, p.nombre AS promotor,
       c.fecha_inicio, c.fecha_fin, e.lugar_evento,
       e.estado_evento, e.google_event_id, c.costo_total
FROM Eventos e
JOIN Cotizaciones c ON c.id_cotizacion = e.id_cotizacion
JOIN Bandas b       ON b.id_banda      = c.id_banda
JOIN Promotores p   ON p.id_promotor   = c.id_promotor;

-- v2: ingresos de la agencia por banda (solo cotizaciones aprobadas)
CREATE OR REPLACE VIEW v_ingresos_por_banda AS
SELECT b.id_banda, b.nombre_banda,
       COUNT(c.id_cotizacion)                          AS shows_aprobados,
       COALESCE(SUM(c.monto_comision_banda), 0)        AS ingreso_comisiones,
       COALESCE(SUM(c.monto_extras_manager + c.costo_luces), 0) AS ingreso_servicios,
       COALESCE(SUM(c.monto_comision_banda + c.monto_extras_manager + c.costo_luces), 0) AS ingreso_agencia
FROM Bandas b
LEFT JOIN Cotizaciones c ON c.id_banda = b.id_banda AND c.estado_cotizacion = 'Aprobada'
GROUP BY b.id_banda, b.nombre_banda;

SELECT * FROM v_agenda_eventos ORDER BY fecha_inicio;
SELECT * FROM v_ingresos_por_banda ORDER BY ingreso_agencia DESC;

-- consultas
-- q1 (join) cotizaciones con banda, categoria y promotor
SELECT c.id_cotizacion, b.nombre_banda, cat.nombre_categoria, p.nombre AS promotor,
       c.fecha_inicio, c.costo_total, c.estado_cotizacion
FROM Cotizaciones c
JOIN Bandas b       ON b.id_banda = c.id_banda
JOIN Categorias cat ON cat.id_categoria = b.id_categoria
JOIN Promotores p   ON p.id_promotor = c.id_promotor
ORDER BY c.fecha_inicio;

-- q2 (join) desglose de extras de cada cotizacion
SELECT c.id_cotizacion, b.nombre_banda, x.nombre_extra, ce.cantidad,
       ce.precio_unitario, ce.cantidad * ce.precio_unitario AS subtotal
FROM Cotizacion_Extras ce
JOIN Cotizaciones c    ON c.id_cotizacion = ce.id_cotizacion
JOIN Bandas b          ON b.id_banda = c.id_banda
JOIN Catalogo_Extras x ON x.id_extra = ce.id_extra
ORDER BY c.id_cotizacion, x.nombre_extra;

-- q3 (left join) todas las bandas y cuantas cotizaciones tienen (incluye 0)
SELECT b.nombre_banda, COUNT(c.id_cotizacion) AS total_cotizaciones
FROM Bandas b
LEFT JOIN Cotizaciones c ON c.id_banda = b.id_banda
GROUP BY b.id_banda, b.nombre_banda
ORDER BY total_cotizaciones DESC, b.nombre_banda;

-- q4 (subconsulta escalar) cotizaciones con costo total mayor al promedio
SELECT id_cotizacion, id_banda, costo_total
FROM Cotizaciones
WHERE costo_total > (SELECT AVG(costo_total) FROM Cotizaciones)
ORDER BY costo_total DESC;

-- q5 (not exists) bandas que nunca han tenido una cotizacion aprobada
SELECT b.nombre_banda
FROM Bandas b
WHERE NOT EXISTS (SELECT 1 FROM Cotizaciones c
                  WHERE c.id_banda = b.id_banda AND c.estado_cotizacion = 'Aprobada');

-- q6 (exists) promotores que tienen al menos una cotizacion aprobada
SELECT p.nombre
FROM Promotores p
WHERE EXISTS (SELECT 1 FROM Cotizaciones c
              WHERE c.id_promotor = p.id_promotor AND c.estado_cotizacion = 'Aprobada');

-- q7 (group by) ingresos de la agencia por mes (solo aprobadas)
SELECT DATE_FORMAT(fecha_inicio, '%Y-%m') AS mes, COUNT(*) AS shows,
       SUM(monto_comision_banda + costo_luces + monto_extras_manager) AS ingreso_agencia
FROM Cotizaciones
WHERE estado_cotizacion = 'Aprobada'
GROUP BY DATE_FORMAT(fecha_inicio, '%Y-%m')
ORDER BY mes;

-- q8 (group by + having) generos con mas de una cotizacion y su ticket promedio
SELECT b.genero_musical, COUNT(*) AS cotizaciones, ROUND(AVG(c.costo_total), 2) AS ticket_promedio
FROM Cotizaciones c
JOIN Bandas b ON b.id_banda = c.id_banda
GROUP BY b.genero_musical
HAVING COUNT(*) > 1
ORDER BY ticket_promedio DESC;

-- q9 (group by + having) promotores que han gastado mas de $10,000 en cotizaciones aprobadas
SELECT p.nombre, SUM(c.costo_total) AS gasto_total
FROM Promotores p
JOIN Cotizaciones c ON c.id_promotor = p.id_promotor
WHERE c.estado_cotizacion = 'Aprobada'
GROUP BY p.id_promotor, p.nombre
HAVING SUM(c.costo_total) > 10000
ORDER BY gasto_total DESC;

-- q10 (libre: autojoin) posibles traslapes de horario entre cotizaciones de la misma banda
SELECT a.id_cotizacion AS cot_a, b.id_cotizacion AS cot_b, a.id_banda, a.fecha_inicio, b.fecha_inicio AS inicio_b
FROM Cotizaciones a
JOIN Cotizaciones b ON a.id_banda = b.id_banda AND a.id_cotizacion < b.id_cotizacion
WHERE a.fecha_inicio < b.fecha_fin AND a.fecha_fin > b.fecha_inicio;

-- q11 (libre: subconsulta derivada) banda con mas ingreso para la agencia
SELECT t.nombre_banda, t.ingreso_agencia
FROM v_ingresos_por_banda t
WHERE t.ingreso_agencia = (SELECT MAX(ingreso_agencia) FROM v_ingresos_por_banda);

-- q12 (libre: busqueda con filtros, como en el modulo de reportes) bandas por genero y rango de tarifa
SELECT b.nombre_banda, b.genero_musical, cat.nombre_categoria, cat.precio_base
FROM Bandas b
JOIN Categorias cat ON cat.id_categoria = b.id_categoria
WHERE b.genero_musical LIKE '%hardcore%' AND cat.precio_base BETWEEN 3000 AND 10000
ORDER BY cat.precio_base, b.nombre_banda;
