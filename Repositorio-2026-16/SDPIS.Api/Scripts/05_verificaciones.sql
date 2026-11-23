-- =====================================================
-- 05_verificaciones.sql
-- SOLO LECTURA: comprueba que la carga quedo bien y muestra los
-- usuarios de prueba. No modifica nada; se puede ejecutar cuando se
-- quiera.
-- Fusiona las secciones 4 y 5 de 16, la seccion 5 de 21 y el
-- resumen de 18.
-- =====================================================
SET SERVEROUTPUT ON SIZE UNLIMITED


-- #####################################################################
-- HU-008: cobertura de distritos
-- Origen: 16_prueba_hu008_asignacion_areas.sql (seccion 4)
-- #####################################################################
-- -----------------------------------------------------
-- 4) COBERTURA: toda combinacion canton+distrito de ubicacion_area
--    debe resolverse a un area con el mismo criterio de sp_registrar_denuncia
--    (criterio de aceptacion 3 de la HU). Solo lectura.
-- -----------------------------------------------------
DECLARE
  v_total NUMBER; v_sin_area NUMBER; v_dist_sin_area NUMBER;
BEGIN
  DBMS_OUTPUT.PUT_LINE('=== 4) Cobertura de distritos ===');
  SELECT COUNT(*) INTO v_total FROM distrito;
  SELECT COUNT(*) INTO v_dist_sin_area FROM distrito d
   WHERE NOT EXISTS (SELECT 1 FROM ubicacion_area ua WHERE ua.canton_id = d.canton_id AND ua.distrito_id = d.id_distrito)
     AND NOT EXISTS (SELECT 1 FROM ubicacion_area ua WHERE ua.canton_id = d.canton_id AND ua.distrito_id IS NULL);
  DBMS_OUTPUT.PUT_LINE('Distritos en catalogo: ' || v_total || ' | sin area asignable: ' || v_dist_sin_area
                       || CASE WHEN v_dist_sin_area = 0 THEN '  [OK]' ELSE '  [FALLO: criterio 3 incumplido]' END);

  FOR r IN (SELECT p.nombre AS provincia, c.nombre AS canton, d.nombre AS distrito
              FROM distrito d JOIN canton c ON c.id_canton = d.canton_id
              JOIN provincia p ON p.id_provincia = c.provincia_id
             WHERE NOT EXISTS (SELECT 1 FROM ubicacion_area ua WHERE ua.canton_id = d.canton_id AND ua.distrito_id = d.id_distrito)
               AND NOT EXISTS (SELECT 1 FROM ubicacion_area ua WHERE ua.canton_id = d.canton_id AND ua.distrito_id IS NULL)
             ORDER BY 1, 2, 3 FETCH FIRST 15 ROWS ONLY) LOOP
    DBMS_OUTPUT.PUT_LINE('   sin area: ' || r.provincia || ' / ' || r.canton || ' / ' || r.distrito);
  END LOOP;

  -- Combinaciones repetidas en distintas areas (el sistema puede elegir cualquiera)
  SELECT COUNT(*) INTO v_sin_area FROM (
    SELECT canton_id, distrito_id FROM ubicacion_area
     GROUP BY canton_id, distrito_id HAVING COUNT(DISTINCT area_id) > 1);
  DBMS_OUTPUT.PUT_LINE('Combinaciones repetidas en varias areas: ' || v_sin_area);
END;
/


-- #####################################################################
-- HU-008/012: denuncias de prueba por area
-- Origen: 16_prueba_hu008_asignacion_areas.sql (seccion 5)
-- #####################################################################
-- -----------------------------------------------------
-- 5) RESUMEN PARA LA BANDEJA (HU-012)
-- -----------------------------------------------------
PROMPT === 5) Denuncias de prueba por area ===
SELECT a.codigo_area, a.id_area, d.consecutivo, d.estado, h.nombre_establecimiento
  FROM denuncia d
  JOIN area a ON a.id_area = d.area_actual_id
  JOIN denuncia_hecho h ON h.denuncia_id = d.id_denuncia
 WHERE h.nombre_establecimiento LIKE 'HU008-%'
 ORDER BY a.codigo_area, d.id_denuncia;

PROMPT === Valores para las const de AsignacionEncargadoPage.jsx ===
SELECT 'MI_AREA_ID = ' || id_area AS valor FROM area WHERE codigo_area = 'ABAN'
UNION ALL SELECT 'JEFES ABAN = 1 y 302 (con permiso de asignar: 9100, 9101; jefas ACOS: 9110 y 9112 en area ' ||
       (SELECT id_area FROM area WHERE codigo_area = 'ACOS') || ')' FROM dual;


-- #####################################################################
-- HU-015: verificaciones de roles y permisos
-- Origen: 21_roles_permisos.sql (seccion 5)
-- #####################################################################
-- =====================================================
-- 5) VERIFICACION (solo lectura)
-- =====================================================

-- Correos repetidos: el MSI enlazara al funcionario por correo; el
-- backend lo exige unico en altas nuevas, pero los datos previos no.
SELECT LOWER(correo) AS correo, COUNT(*) AS veces
  FROM funcionario
 GROUP BY LOWER(correo)
HAVING COUNT(*) > 1;

-- Jefes con denuncias asignadas (HU-015: un jefe solo tiene "por asignar").
SELECT f.id_funcionario, f.nombre, COUNT(*) AS asignadas
  FROM denuncia d JOIN funcionario f ON f.id_funcionario = d.funcionario_asignado_id
 WHERE f.rol = 'jefe_area' AND d.estado <> 'FINALIZADA'
 GROUP BY f.id_funcionario, f.nombre;

-- Areas por encima del cupo: despues de la seccion 4 debe venir vacia.
SELECT a.codigo_area, a.max_jefes, COUNT(*) AS jefes_activos
  FROM funcionario f JOIN area a ON a.id_area = f.area_id
 WHERE f.rol = 'jefe_area' AND f.activo = 'S'
   AND NOT EXISTS (SELECT 1 FROM funcionario_permiso fp
                    WHERE fp.funcionario_id = f.id_funcionario
                      AND fp.codigo_permiso = 'RENUNCIA_SUPERUSUARIO')
 GROUP BY a.codigo_area, a.max_jefes
HAVING COUNT(*) > a.max_jefes;

-- Usuarios de prueba (HU-001) y su rol: deben verse 301, 302 y 303.
SELECT f.id_funcionario, f.nombre, f.rol, a.codigo_area
  FROM funcionario f JOIN area a ON a.id_area = f.area_id
 WHERE f.id_funcionario IN (301, 302, 303)
 ORDER BY f.id_funcionario;


-- #####################################################################
-- HU-018: solicitudes de cierre
-- #####################################################################
-- Solicitudes pendientes por area (bandeja de la jefatura).
SELECT a.codigo_area AS area, d.consecutivo, f.nombre AS solicitado_por,
       s.fecha_solicitud, d.estado AS estado_denuncia
  FROM solicitud_cierre s
  JOIN denuncia d    ON d.id_denuncia = s.denuncia_id
  JOIN area a        ON a.id_area = s.area_id
  JOIN funcionario f ON f.id_funcionario = s.solicitado_por_id
 WHERE s.estado = 'PENDIENTE'
 ORDER BY a.codigo_area, s.fecha_solicitud;

-- Debe venir vacia: denuncias EN_REVISION sin solicitud pendiente, o
-- solicitudes pendientes cuya denuncia ya no esta EN_REVISION.
SELECT d.consecutivo, d.estado,
       (SELECT COUNT(*) FROM solicitud_cierre s
         WHERE s.denuncia_id = d.id_denuncia AND s.estado = 'PENDIENTE') AS pendientes
  FROM denuncia d
 WHERE (d.estado = 'EN_REVISION'
        AND NOT EXISTS (SELECT 1 FROM solicitud_cierre s
                         WHERE s.denuncia_id = d.id_denuncia AND s.estado = 'PENDIENTE'))
    OR (d.estado <> 'EN_REVISION'
        AND EXISTS (SELECT 1 FROM solicitud_cierre s
                     WHERE s.denuncia_id = d.id_denuncia AND s.estado = 'PENDIENTE'));


-- #####################################################################
-- HU-017: decisiones de la jefatura sobre las solicitudes de cierre
-- #####################################################################
-- Historial de decisiones por denuncia (solicitud + decision).
SELECT d.consecutivo, s.id_solicitud_cierre, s.fecha_solicitud, s.estado_previo,
       s.estado AS estado_solicitud, dc.decision, f.nombre AS decidido_por,
       dc.fecha_decision, dc.observaciones_jefatura, d.estado AS estado_denuncia
  FROM solicitud_cierre s
  JOIN denuncia d              ON d.id_denuncia = s.denuncia_id
  LEFT JOIN decision_cierre dc ON dc.solicitud_cierre_id = s.id_solicitud_cierre
  LEFT JOIN funcionario f      ON f.id_funcionario = dc.decidido_por_id
 ORDER BY d.consecutivo, s.fecha_solicitud;

-- Debe venir vacia: solicitudes resueltas sin decision, pendientes con
-- decision, o decision distinta del estado de la solicitud.
SELECT s.id_solicitud_cierre, s.estado, dc.decision
  FROM solicitud_cierre s
  LEFT JOIN decision_cierre dc ON dc.solicitud_cierre_id = s.id_solicitud_cierre
 WHERE (s.estado = 'PENDIENTE' AND dc.id_decision_cierre IS NOT NULL)
    OR (s.estado <> 'PENDIENTE' AND (dc.id_decision_cierre IS NULL OR dc.decision <> s.estado));

-- Debe venir vacia: aprobaciones cuya denuncia no quedo FINALIZADA o sin
-- registro en denuncia_cierre.
SELECT d.consecutivo, d.estado
  FROM decision_cierre dc
  JOIN solicitud_cierre s ON s.id_solicitud_cierre = dc.solicitud_cierre_id
  JOIN denuncia d         ON d.id_denuncia = s.denuncia_id
 WHERE dc.decision = 'APROBADA'
   AND (d.estado <> 'FINALIZADA'
        OR NOT EXISTS (SELECT 1 FROM denuncia_cierre c WHERE c.denuncia_id = d.id_denuncia));

-- Informativa: solicitudes pendientes sin estado_previo (anteriores a
-- HU-017). Si se rechazan, la denuncia vuelve a EN_TIEMPO.
SELECT d.consecutivo, s.id_solicitud_cierre, s.fecha_solicitud
  FROM solicitud_cierre s
  JOIN denuncia d ON d.id_denuncia = s.denuncia_id
 WHERE s.estado = 'PENDIENTE' AND s.estado_previo IS NULL;


-- #####################################################################
-- Resumen general de los datos de prueba
-- Origen: nuevo (consultas de control)
-- #####################################################################
-- Denuncias por area y estado.
SELECT a.codigo_area AS area, d.estado, COUNT(*) AS denuncias
  FROM denuncia d JOIN area a ON a.id_area = d.area_actual_id
 WHERE a.codigo_area IN ('ABAN', 'ACOS', 'LIMO', 'SRAM', 'UVC')
 GROUP BY a.codigo_area, d.estado
 ORDER BY 1, 2;

-- Jefes activos por area (maximo 2).
SELECT a.codigo_area AS area, COUNT(*) AS jefes_activos, a.max_jefes
  FROM funcionario f JOIN area a ON a.id_area = f.area_id
 WHERE f.rol = 'jefe_area' AND f.activo = 'S'
 GROUP BY a.codigo_area, a.max_jefes
 ORDER BY 1;

-- Debe dar 0: ya no hay consecutivos SDPIS-...
SELECT COUNT(*) AS consecutivos_sdpis FROM denuncia WHERE consecutivo LIKE 'SDPIS-%';

-- Debe venir vacia: funcionarios con usuario cuyo nombre no tiene nombre y dos apellidos.
SELECT u.identificacion, f.nombre
  FROM usuario_temporal u JOIN funcionario f ON f.id_funcionario = u.funcionario_id
 WHERE REGEXP_COUNT(TRIM(f.nombre), ' ') < 2;

-- Objetos invalidos (debe venir vacia).
SELECT object_type, object_name FROM user_objects WHERE status <> 'VALID' ORDER BY 1, 2;


-- #####################################################################
-- Usuarios de prueba (clave de todos: ClavePrueba2026!)
-- Origen: 18_seed_usuarios_temporales.sql (resumen)
-- #####################################################################
-- Resumen: usuarios de prueba. Clave de todos: ClavePrueba2026!
SELECT u.identificacion AS cedula, f.nombre, f.rol, a.codigo_area AS area,
       f.activo, f.permite_asignar, u.correo_institucional AS correo
  FROM usuario_temporal u
  JOIN funcionario f ON f.id_funcionario = u.funcionario_id
  JOIN area a ON a.id_area = f.area_id
 ORDER BY CASE f.rol WHEN 'superusuario' THEN 1 WHEN 'jefe_area' THEN 2 ELSE 3 END,
          a.codigo_area, u.identificacion;

PROMPT === Denuncias de Valeria Mora (303) para el plan de usabilidad ===
SELECT d.consecutivo, d.estado, h.nombre_establecimiento, p.nombre_producto_texto
  FROM denuncia d
  JOIN denuncia_hecho h   ON h.denuncia_id = d.id_denuncia
  JOIN detalle_producto p ON p.denuncia_id = d.id_denuncia
 WHERE d.funcionario_asignado_id = 303
 ORDER BY d.id_denuncia;