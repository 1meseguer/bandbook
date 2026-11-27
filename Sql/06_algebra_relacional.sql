-- sql de cada expresion de algebra relacional del reporte
-- ejecutar despues de 01 a 04 (si ya hay mas datos, saldran mas renglones)
-- la proyeccion elimina duplicados, por eso se usa distinct
USE bandflow;

-- 1. seleccion (σ)
-- σ estado_cotizacion = 'aprobada' (cotizaciones)
SELECT *
FROM Cotizaciones
WHERE estado_cotizacion = 'Aprobada';

-- 2. seleccion y proyeccion (π)
-- π nombre_banda (σ genero_musical = 'metal' (bandas))
SELECT DISTINCT nombre_banda
FROM Bandas
WHERE genero_musical = 'Metal';

-- 3. reunion (⋈)
-- π nombre_banda, nombre, fecha_inicio, costo_total
-- (σ estado_cotizacion = 'aprobada' (bandas ⋈ cotizaciones ⋈ promotores))
SELECT DISTINCT b.nombre_banda, p.nombre, c.fecha_inicio, c.costo_total
FROM Bandas b
JOIN Cotizaciones c ON c.id_banda    = b.id_banda
JOIN Promotores   p ON p.id_promotor = c.id_promotor
WHERE c.estado_cotizacion = 'Aprobada'
ORDER BY c.fecha_inicio;

-- 4. diferencia (−)
-- π id_banda (bandas) − π id_banda (σ estado_cotizacion = 'aprobada' (cotizaciones))
SELECT b.id_banda
FROM Bandas b
WHERE NOT EXISTS (SELECT 1
                  FROM Cotizaciones c
                  WHERE c.id_banda = b.id_banda
                    AND c.estado_cotizacion = 'Aprobada')
ORDER BY b.id_banda;

-- igual pero con el nombre de la banda
SELECT b.id_banda, b.nombre_banda
FROM Bandas b
WHERE NOT EXISTS (SELECT 1
                  FROM Cotizaciones c
                  WHERE c.id_banda = b.id_banda
                    AND c.estado_cotizacion = 'Aprobada')
ORDER BY b.nombre_banda;

-- 5. union (∪)
-- π id_promotor (σ estado_cotizacion = 'pendiente' (cotizaciones))
-- ∪ π id_promotor (σ estado_cotizacion = 'rechazada' (cotizaciones))
SELECT id_promotor FROM Cotizaciones WHERE estado_cotizacion = 'Pendiente'
UNION
SELECT id_promotor FROM Cotizaciones WHERE estado_cotizacion = 'Rechazada'
ORDER BY id_promotor;

-- igual pero con el nombre del promotor
SELECT p.id_promotor, p.nombre
FROM Promotores p
WHERE p.id_promotor IN (
        SELECT id_promotor FROM Cotizaciones WHERE estado_cotizacion = 'Pendiente'
        UNION
        SELECT id_promotor FROM Cotizaciones WHERE estado_cotizacion = 'Rechazada')
ORDER BY p.id_promotor;

-- 6. agrupacion (γ)
-- γ genero_musical; count(id_cotizacion) → cotizaciones,
-- avg(costo_total) → ticket_promedio  (bandas ⋈ cotizaciones)
SELECT b.genero_musical,
       COUNT(c.id_cotizacion)       AS cotizaciones,
       ROUND(AVG(c.costo_total), 2) AS ticket_promedio
FROM Bandas b
JOIN Cotizaciones c ON c.id_banda = b.id_banda
GROUP BY b.genero_musical
ORDER BY cotizaciones DESC, b.genero_musical;
