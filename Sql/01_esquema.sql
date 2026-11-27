-- crea la base de datos, tablas, claves y restricciones (mysql 8)
-- ejecutar primero, borra y vuelve a crear todo
DROP DATABASE IF EXISTS bandflow;
CREATE DATABASE bandflow CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE bandflow;

-- categorias de bandas con su tarifa base vigente
CREATE TABLE Categorias (
  id_categoria     INT AUTO_INCREMENT PRIMARY KEY,
  nombre_categoria VARCHAR(50)   NOT NULL,
  precio_base      DECIMAL(10,2) NOT NULL,
  CONSTRAINT uq_categoria_nombre UNIQUE (nombre_categoria),
  CONSTRAINT ck_categoria_precio CHECK (precio_base >= 0)
) ENGINE=InnoDB;

-- catalogo de bandas representadas
CREATE TABLE Bandas (
  id_banda            INT AUTO_INCREMENT PRIMARY KEY,
  nombre_banda        VARCHAR(100) NOT NULL,
  genero_musical      VARCHAR(60)  NOT NULL,
  porcentaje_comision DECIMAL(5,2) NOT NULL DEFAULT 10.00,
  id_categoria        INT          NOT NULL,
  contacto_principal  VARCHAR(100) NOT NULL,
  activa              BOOLEAN      NOT NULL DEFAULT TRUE,
  CONSTRAINT uq_banda_nombre UNIQUE (nombre_banda),
  CONSTRAINT ck_banda_comision CHECK (porcentaje_comision BETWEEN 0 AND 100),
  CONSTRAINT fk_banda_categoria FOREIGN KEY (id_categoria)
    REFERENCES Categorias (id_categoria) ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

-- promotores / clientes que solicitan cotizaciones
CREATE TABLE Promotores (
  id_promotor INT AUTO_INCREMENT PRIMARY KEY,
  nombre      VARCHAR(100) NOT NULL,
  telefono    VARCHAR(20)  NOT NULL,
  correo      VARCHAR(100) NOT NULL,
  CONSTRAINT uq_promotor_correo UNIQUE (correo)
) ENGINE=InnoDB;

-- cotizaciones (se guarda una "instantanea" de los precios al cotizar)
CREATE TABLE Cotizaciones (
  id_cotizacion         INT AUTO_INCREMENT PRIMARY KEY,
  id_banda              INT           NOT NULL,
  id_promotor           INT           NOT NULL,
  fecha_inicio          DATETIME      NOT NULL,
  duracion_minutos      INT           NOT NULL DEFAULT 120,
  fecha_fin             DATETIME GENERATED ALWAYS AS
                          (DATE_ADD(fecha_inicio, INTERVAL duracion_minutos MINUTE)) STORED,
  costo_base            DECIMAL(10,2) NOT NULL,
  incluye_luces         BOOLEAN       NOT NULL DEFAULT FALSE,
  costo_luces           DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  monto_comision_banda  DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  monto_extras_manager  DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  costo_total           DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  estado_cotizacion     VARCHAR(20)   NOT NULL DEFAULT 'Pendiente',
  fecha_creacion        TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_cot_banda    FOREIGN KEY (id_banda)    REFERENCES Bandas (id_banda)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_cot_promotor FOREIGN KEY (id_promotor) REFERENCES Promotores (id_promotor)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT ck_cot_estado   CHECK (estado_cotizacion IN ('Pendiente','Aprobada','Rechazada')),
  CONSTRAINT ck_cot_duracion CHECK (duracion_minutos BETWEEN 15 AND 720),
  CONSTRAINT ck_cot_montos   CHECK (costo_base >= 0 AND costo_luces >= 0
                                    AND monto_comision_banda >= 0
                                    AND monto_extras_manager >= 0 AND costo_total >= 0),
  CONSTRAINT ck_cot_luces    CHECK (incluye_luces = TRUE OR costo_luces = 0),
  INDEX idx_cot_banda_fecha (id_banda, fecha_inicio)
) ENGINE=InnoDB;

-- catalogo de servicios extra que ofrece el manager
CREATE TABLE Catalogo_Extras (
  id_extra        INT AUTO_INCREMENT PRIMARY KEY,
  nombre_extra    VARCHAR(100)  NOT NULL,
  precio_sugerido DECIMAL(10,2) NOT NULL,
  CONSTRAINT uq_extra_nombre UNIQUE (nombre_extra),
  CONSTRAINT ck_extra_precio CHECK (precio_sugerido >= 0)
) ENGINE=InnoDB;

-- tabla intermedia entre cotizaciones y catalogo_extras
CREATE TABLE Cotizacion_Extras (
  id_cotizacion   INT           NOT NULL,
  id_extra        INT           NOT NULL,
  cantidad        INT           NOT NULL DEFAULT 1,
  precio_unitario DECIMAL(10,2) NOT NULL,
  PRIMARY KEY (id_cotizacion, id_extra),
  CONSTRAINT fk_ce_cot   FOREIGN KEY (id_cotizacion) REFERENCES Cotizaciones (id_cotizacion)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ce_extra FOREIGN KEY (id_extra) REFERENCES Catalogo_Extras (id_extra)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT ck_ce_cantidad CHECK (cantidad > 0),
  CONSTRAINT ck_ce_precio   CHECK (precio_unitario >= 0)
) ENGINE=InnoDB;

-- eventos confirmados, la fecha viene de la cotizacion
CREATE TABLE Eventos (
  id_evento       INT AUTO_INCREMENT PRIMARY KEY,
  id_cotizacion   INT          NOT NULL,
  lugar_evento    VARCHAR(200) NOT NULL,
  google_event_id VARCHAR(255) NULL,
  estado_evento   VARCHAR(20)  NOT NULL DEFAULT 'Programado',
  CONSTRAINT uq_evento_cot UNIQUE (id_cotizacion),
  CONSTRAINT fk_evento_cot FOREIGN KEY (id_cotizacion) REFERENCES Cotizaciones (id_cotizacion)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT ck_evento_estado CHECK (estado_evento IN ('Programado','Realizado','Cancelado'))
) ENGINE=InnoDB;

-- bitacora de auditoria de cambios de estado (la llena un trigger)
CREATE TABLE Auditoria_Cotizaciones (
  id_auditoria    INT AUTO_INCREMENT PRIMARY KEY,
  id_cotizacion   INT         NOT NULL,
  estado_anterior VARCHAR(20) NOT NULL,
  estado_nuevo    VARCHAR(20) NOT NULL,
  usuario_bd      VARCHAR(100) NOT NULL,
  fecha_cambio    TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_aud_cot (id_cotizacion)
) ENGINE=InnoDB;
