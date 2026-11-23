-- =====================================================
-- 02_procedimientos.sql
-- Ejecutar despues de 01_esquema.sql.
-- =====================================================
SET DEFINE OFF


-- #####################################################################
-- HU-009/016: bitacora, notificaciones y asignacion de validador
-- Origen: 07_sp_asignacion_manual.sql
-- #####################################################################
CREATE OR REPLACE PROCEDURE sp_asignar_validador_denuncia (
  p_denuncia_id   IN NUMBER,
  p_validador_id  IN NUMBER
)
IS
BEGIN
  UPDATE denuncia
  SET funcionario_asignado_id = p_validador_id,
      estado = 'EN_TIEMPO'
  WHERE id_denuncia = p_denuncia_id;
END sp_asignar_validador_denuncia;
/

CREATE OR REPLACE PROCEDURE sp_insertar_bitacora (
  p_denuncia_id    IN NUMBER,
  p_funcionario_id IN NUMBER,
  p_area_origen_id IN NUMBER,
  p_tipo_registro  IN VARCHAR2,
  p_contenido      IN VARCHAR2
)
IS
BEGIN
  INSERT INTO bitacora (
    denuncia_id,
    funcionario_id,
    area_origen_id,
    tipo_registro,
    contenido
  ) VALUES (
    p_denuncia_id,
    p_funcionario_id,
    p_area_origen_id,
    p_tipo_registro,
    p_contenido
  );
END sp_insertar_bitacora;
/

CREATE OR REPLACE PROCEDURE sp_insertar_notificacion (
  p_denuncia_id                 IN NUMBER,
  p_tipo                        IN VARCHAR2,
  p_destinatario_tipo           IN VARCHAR2,
  p_destinatario_funcionario_id IN NUMBER,
  p_mensaje                     IN VARCHAR2
)
IS
BEGIN
  INSERT INTO notificacion (
    denuncia_id,
    tipo,
    destinatario_tipo,
    destinatario_funcionario_id,
    mensaje
  ) VALUES (
    p_denuncia_id,
    p_tipo,
    p_destinatario_tipo,
    p_destinatario_funcionario_id,
    p_mensaje
  );
END sp_insertar_notificacion;
/


-- #####################################################################
-- HU-002/008: registrar denuncia
-- Origen: 04_sp_registrar_denuncia.sql
-- #####################################################################
-- =====================================================
-- 04_sp_registrar_denuncia.sql
-- Registra una nueva denuncia en el sistema y le asigna el
-- Área Rectora de Salud correspondiente según su ubicación geográfica.
-- Implementación de HU-008.
--
-- NOTA (HU-016): la bitácora se inserta con SQL directo (paso 8, más
-- abajo) y no usa sp_insertar_bitacora (definido en
-- 07_sp_asignacion_manual.sql, que se ejecuta después de este script).
-- =====================================================

CREATE OR REPLACE PROCEDURE sp_registrar_denuncia (
  p_denuncia_json      IN  CLOB,
  p_codigo_seguimiento IN  VARCHAR2,
  p_id_denuncia        OUT NUMBER,
  p_consecutivo        OUT VARCHAR2
)
IS
  v_anio_actual NUMBER(4) := EXTRACT(YEAR FROM SYSDATE);

  -- datos del hecho
  v_denunciado_otra_institucion     denuncia_hecho.denunciado_otra_institucion%TYPE;
  v_detalle_otra_institucion        denuncia_hecho.detalle_otra_institucion%TYPE;
  v_denunciado_previamente_minsalud denuncia_hecho.denunciado_previamente_minsalud%TYPE;
  v_detalle_previo_minsalud         denuncia_hecho.detalle_previo_minsalud%TYPE;
  v_nombre_establecimiento          denuncia_hecho.nombre_establecimiento%TYPE;
  v_descripcion_hecho               denuncia_hecho.descripcion_hecho%TYPE;

  -- ubicacion
  v_canton_id        canton.id_canton%TYPE;
  v_distrito_id      distrito.id_distrito%TYPE;
  v_direccion_exacta denuncia_ubicacion.direccion_exacta%TYPE;

  -- privacidad / denunciante (ya validados por Backend)
  v_es_anonima         denuncia_denunciante.es_anonima%TYPE;
  v_den_nombre         denunciante.nombre%TYPE;
  v_den_numero_id      denunciante.numero_identificacion%TYPE;
  v_den_correo         denunciante.correo%TYPE;
  v_den_telefono       denunciante.telefono%TYPE;
  v_den_direccion      denuncia_denunciante.direccion_domicilio%TYPE;
  v_denunciante_id     denunciante.id_denunciante%TYPE;

  -- area / consecutivo
  v_area_id           area.id_area%TYPE;
  v_codigo_area       area.codigo_area%TYPE;
  v_numero_secuencial denuncia.numero_secuencial%TYPE;

  v_id_detalle_producto detalle_producto.id_detalle_producto%TYPE;

BEGIN
  ----------------------------------------------------------------
  -- 0. guarda tecnica de la interfaz (no es validacion de negocio)
  ----------------------------------------------------------------
  IF p_denuncia_json IS NULL OR NOT p_denuncia_json IS JSON THEN
    RAISE_APPLICATION_ERROR(-20000, 'El payload de la denuncia no es un JSON valido.');
  END IF;

  ----------------------------------------------------------------
  -- 1. leer campos escalares del JSON (ya validados/normalizados por Backend)
  ----------------------------------------------------------------
  SELECT jt.denunciado_otra_institucion,
         jt.detalle_otra_institucion,
         jt.denunciado_previamente_minsalud,
         jt.detalle_previo_minsalud,
         jt.nombre_establecimiento,
         jt.descripcion_hecho,
         jt.canton_id,
         jt.distrito_id,
         jt.direccion_exacta,
         jt.es_anonima,
         jt.denunciante_nombre,
         jt.denunciante_numero_identificacion,
         jt.denunciante_correo,
         jt.denunciante_telefono,
         jt.denunciante_direccion_domicilio
    INTO v_denunciado_otra_institucion,
         v_detalle_otra_institucion,
         v_denunciado_previamente_minsalud,
         v_detalle_previo_minsalud,
         v_nombre_establecimiento,
         v_descripcion_hecho,
         v_canton_id,
         v_distrito_id,
         v_direccion_exacta,
         v_es_anonima,
         v_den_nombre,
         v_den_numero_id,
         v_den_correo,
         v_den_telefono,
         v_den_direccion
    FROM JSON_TABLE(
      p_denuncia_json, '$'
      COLUMNS (
        denunciado_otra_institucion      CHAR(1)       PATH '$.hecho.denunciado_otra_institucion',
        detalle_otra_institucion         VARCHAR2(500) PATH '$.hecho.detalle_otra_institucion',
        denunciado_previamente_minsalud  CHAR(1)       PATH '$.hecho.denunciado_previamente_minsalud',
        detalle_previo_minsalud          VARCHAR2(500) PATH '$.hecho.detalle_previo_minsalud',
        nombre_establecimiento           VARCHAR2(200) PATH '$.hecho.nombre_establecimiento',
        descripcion_hecho                VARCHAR2(4000) PATH '$.hecho.descripcion_hecho',
        canton_id                        NUMBER        PATH '$.ubicacion.canton_id',
        distrito_id                      NUMBER        PATH '$.ubicacion.distrito_id',
        direccion_exacta                 VARCHAR2(500) PATH '$.ubicacion.direccion_exacta',
        es_anonima                       CHAR(1)       PATH '$.privacidad.es_anonima',
        denunciante_nombre               VARCHAR2(150) PATH '$.privacidad.denunciante.nombre',
        denunciante_numero_identificacion VARCHAR2(30) PATH '$.privacidad.denunciante.numero_identificacion',
        denunciante_correo               VARCHAR2(150) PATH '$.privacidad.denunciante.correo',
        denunciante_telefono             VARCHAR2(20)  PATH '$.privacidad.denunciante.telefono',
        denunciante_direccion_domicilio  VARCHAR2(500) PATH '$.privacidad.denunciante.direccion_domicilio'
      )
    ) jt;

  ----------------------------------------------------------------
  -- 2. invariante de privacidad (garantia estructural, no validacion de formato)
  ----------------------------------------------------------------
  IF v_es_anonima = 'S' THEN
    v_den_nombre    := NULL;
    v_den_numero_id := NULL;
    v_den_correo    := NULL;
    v_den_telefono  := NULL;
    v_den_direccion := NULL;
  END IF;

  ----------------------------------------------------------------
  -- 3. resolver area (HU-008: Dirigir denuncia a area).
  --    Busca por coincidencia exacta (Canton + Distrito).
  --    Si no encuentra, hace fallback a Canton general (Distrito IS NULL).
  ----------------------------------------------------------------
  BEGIN
    SELECT area_id INTO v_area_id
    FROM (
      SELECT area_id, 1 as prioridad
      FROM ubicacion_area
      WHERE canton_id = v_canton_id
        AND NVL(distrito_id, -1) = NVL(v_distrito_id, -1)
      UNION ALL
      SELECT area_id, 2 as prioridad
      FROM ubicacion_area
      WHERE canton_id = v_canton_id
        AND distrito_id IS NULL
      ORDER BY prioridad ASC
    )
    WHERE ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20008, UNISTR('No se encontr\00F3 un \00C1rea Rectora de Salud (ARS) para la ubicaci\00F3n especificada.'));
  END;

  SELECT codigo_area INTO v_codigo_area FROM area WHERE id_area = v_area_id;

  ----------------------------------------------------------------
  -- 4. numero secuencial por area y anio (bloqueo por fila -> sin choques;
  --    reinicio anual: el filtro por anio hace que vuelva a 1 cada anio)
  ----------------------------------------------------------------
  UPDATE contador_denuncia_area
     SET ultimo_numero = ultimo_numero + 1
   WHERE area_id = v_area_id
     AND anio = v_anio_actual
  RETURNING ultimo_numero INTO v_numero_secuencial;

  IF SQL%ROWCOUNT = 0 THEN
    INSERT INTO contador_denuncia_area (area_id, anio, ultimo_numero)
    VALUES (v_area_id, v_anio_actual, 1);
    v_numero_secuencial := 1;
  END IF;

  p_consecutivo := v_codigo_area || '-' || TO_CHAR(v_anio_actual) || '-' ||
                   LPAD(v_numero_secuencial, 6, '0');

  ----------------------------------------------------------------
  -- 5. denuncia (anio_creacion ya NO va aqui: es columna virtual, se
  --    calcula sola desde fecha_creacion)
  ----------------------------------------------------------------
  INSERT INTO denuncia (consecutivo, numero_secuencial, area_actual_id)
  VALUES (p_consecutivo, v_numero_secuencial, v_area_id)
  RETURNING id_denuncia INTO p_id_denuncia;

  INSERT INTO denuncia_hecho (
    denuncia_id, nombre_establecimiento, descripcion_hecho,
    denunciado_otra_institucion, detalle_otra_institucion,
    denunciado_previamente_minsalud, detalle_previo_minsalud
  ) VALUES (
    p_id_denuncia, v_nombre_establecimiento, v_descripcion_hecho,
    v_denunciado_otra_institucion, v_detalle_otra_institucion,
    v_denunciado_previamente_minsalud, v_detalle_previo_minsalud
  );

  INSERT INTO denuncia_ubicacion (
    denuncia_id, canton_id, distrito_id, area_formulario_id, direccion_exacta
  ) VALUES (
    p_id_denuncia, v_canton_id, v_distrito_id, v_area_id, v_direccion_exacta
  );

  INSERT INTO denuncia_seguimiento (denuncia_id, codigo_seguimiento)
  VALUES (p_id_denuncia, p_codigo_seguimiento);

  ----------------------------------------------------------------
  -- 6. denunciante (solo si es confidencial)
  ----------------------------------------------------------------
  IF v_es_anonima = 'N' THEN
    INSERT INTO denunciante (nombre, numero_identificacion, correo, telefono)
    VALUES (v_den_nombre, v_den_numero_id, v_den_correo, v_den_telefono)
    RETURNING id_denunciante INTO v_denunciante_id;
  END IF;

  INSERT INTO denuncia_denunciante (denuncia_id, denunciante_id, es_anonima, direccion_domicilio)
  VALUES (p_id_denuncia, v_denunciante_id, v_es_anonima, v_den_direccion);

  ----------------------------------------------------------------
  -- 7. productos (uno o varios) y sus motivos
  ----------------------------------------------------------------
  FOR prod IN (
    SELECT jt.tipo_producto_id, jt.nombre_producto_texto,
           jt.registro_sanitario_ingresado, jt.descripcion_producto,
           jt.marca_ingresada, jt.numero_lote, jt.fabricante_ingresado,
           jt.pais_origen, jt.presentacion, jt.fecha_compra_txt, jt.motivos_json
    FROM JSON_TABLE(
      p_denuncia_json, '$.productos[*]'
      COLUMNS (
        tipo_producto_id              NUMBER        PATH '$.tipo_producto_id',
        nombre_producto_texto         VARCHAR2(200) PATH '$.nombre_producto_texto',
        registro_sanitario_ingresado  VARCHAR2(30)  PATH '$.registro_sanitario_ingresado',
        descripcion_producto          VARCHAR2(500) PATH '$.descripcion_producto',
        marca_ingresada               VARCHAR2(150) PATH '$.marca_ingresada',
        numero_lote                   VARCHAR2(50)  PATH '$.numero_lote',
        fabricante_ingresado          VARCHAR2(200) PATH '$.fabricante_ingresado',
        pais_origen                   VARCHAR2(100) PATH '$.pais_origen',
        presentacion                  VARCHAR2(100) PATH '$.presentacion',
        fecha_compra_txt              VARCHAR2(10)  PATH '$.fecha_compra',
        motivos_json                  VARCHAR2(4000) FORMAT JSON PATH '$.motivos'
      )
    ) jt
  )
  LOOP
    INSERT INTO detalle_producto (
      denuncia_id, tipo_producto_id, nombre_producto_texto,
      registro_sanitario_ingresado, descripcion_producto, marca_ingresada
    ) VALUES (
      p_id_denuncia, prod.tipo_producto_id, prod.nombre_producto_texto,
      prod.registro_sanitario_ingresado, prod.descripcion_producto, prod.marca_ingresada
    ) RETURNING id_detalle_producto INTO v_id_detalle_producto;

    INSERT INTO detalle_producto_compra (
      id_detalle_producto, numero_lote, fabricante_ingresado,
      pais_origen, presentacion, fecha_compra
    ) VALUES (
      v_id_detalle_producto, prod.numero_lote, prod.fabricante_ingresado,
      prod.pais_origen, prod.presentacion,
      CASE WHEN prod.fecha_compra_txt IS NOT NULL
           THEN TO_DATE(prod.fecha_compra_txt, 'YYYY-MM-DD') END
    );

    FOR mot IN (
      SELECT VALUE AS id_motivo
      FROM JSON_TABLE(prod.motivos_json, '$[*]' COLUMNS (VALUE NUMBER PATH '$'))
    )
    LOOP
      INSERT INTO detalle_producto_motivo (id_detalle_producto, id_motivo)
      VALUES (v_id_detalle_producto, mot.id_motivo);
    END LOOP;
  END LOOP;

  ----------------------------------------------------------------
  -- 8. bitacora (evento automatico, sin funcionario detras)
  ----------------------------------------------------------------
  INSERT INTO bitacora (denuncia_id, funcionario_id, area_origen_id, tipo_registro, contenido)
  VALUES (p_id_denuncia, NULL, v_area_id, 'registro_denuncia',
          'Denuncia recibida y registrada automaticamente en el sistema.');

  COMMIT;

EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_registrar_denuncia;
/


-- #####################################################################
-- HU-010: criterios tecnicos
-- Origen: 10_sp_criterios_tecnicos.sql
-- #####################################################################
-- =====================================================
-- 10_sp_criterios_tecnicos.sql
-- Stored procedures para HU-010 "Registrar criterios tecnicos
-- de analisis".
--
-- El puntaje_total se recalcula (suma de lo que quede en
-- respuesta_criterio) despues de cada registro/actualizacion de
-- UN criterio individual (criterio de aceptacion #3 y #6 de la HU,
-- correccion de alcance: por criterio, no por evaluacion completa
-- -- ver sp_actualizar_criterio_tecnico). nivel_criticidad y fecha_limite
-- quedan siempre NULL/intactos aqui: mapear el puntaje a un nivel
-- (ALTO/MEDIO/BAJO) es responsabilidad exclusiva de HU-012.
--
-- La validacion de rol de superusuario (solo el superusuario puede
-- actualizar un criterio o la metadata) se hace en el
-- backend C#, no aqui.
--
-- Todos reciben denuncia_id (no id_evaluacion): el frontend trabaja
-- en el contexto de una denuncia, no conoce el id interno de la
-- evaluacion. Se resuelve la evaluacion mas reciente de esa denuncia.
-- Una denuncia solo deberia tener una evaluacion activa en el alcance
-- de HU-010; si en el futuro HU-011/re-evaluaciones permiten varias,
-- revisar este criterio de "mas reciente".
--
-- Los criterios no se pueden eliminar: una denuncia siempre debe
-- quedar con sus criterios registrados (se retiro el antiguo
-- sp_eliminar_criterio_tecnico). En bases locales creadas antes de
-- este cambio el procedimiento puede seguir existiendo sin uso; se
-- borra con 00_reset_local.sql o con DROP PROCEDURE.
--
-- sp_actualizar_criterio_tecnico exige p_justificacion (no vacia) cuando
-- el criterio ya tenia una respuesta registrada por el funcionario
-- validador (edicion real, ej. cambiar "Si" por "No"). No se exige
-- cuando el criterio no tenia valor previo (boton "Agregar" en el
-- frontend, evaluaciones incompletas de cuando si se permitia eliminar
-- criterios): ahi no hay nada que "justificar", se esta completando la
-- evaluacion. La justificacion queda en bitacora, no en una columna
-- aparte, siguiendo el mismo patron que el resto de este archivo.
--
-- Ejecutar DESPUES de 09_catalogo_criterios_tecnicos.sql.
-- =====================================================

-- Textos legibles para la bitacora: la bitacora la leen personas que no
-- son de TI, asi que se usan los mismos nombres que ven en pantalla (con
-- tildes) en vez de los nombres internos del catalogo.
CREATE OR REPLACE FUNCTION fn_etiqueta_criterio_tecnico(p_criterio_clave IN VARCHAR2)
RETURN VARCHAR2
IS
BEGIN
  RETURN CASE p_criterio_clave
    WHEN 'DANO_POTENCIAL_SALUD' THEN UNISTR('Nivel de da\00F1o potencial a la salud')
    WHEN 'CRITERIO_ESPECIALIZADO_UVC' THEN UNISTR('\00BFRequiere criterio t\00E9cnico especializado de UVC?')
    ELSE p_criterio_clave
  END;
END fn_etiqueta_criterio_tecnico;
/

CREATE OR REPLACE FUNCTION fn_valor_legible(p_valor IN VARCHAR2)
RETURN VARCHAR2
IS
BEGIN
  RETURN CASE
    WHEN p_valor IS NULL THEN 'sin dato'
    WHEN p_valor IN ('S', 'Si') THEN UNISTR('S\00ED')
    WHEN p_valor = 'N' THEN 'No'
    ELSE p_valor
  END;
END fn_valor_legible;
/

CREATE OR REPLACE PROCEDURE sp_registrar_criterios_tecnicos (
  p_denuncia_id                     IN  NUMBER,
  p_funcionario_validador_id        IN  NUMBER,
  p_vinculado_comercio_electronico  IN  CHAR,
  p_dano_potencial_salud            IN  VARCHAR2,
  p_requiere_criterio_especializado IN  VARCHAR2,
  p_observaciones_tecnicas          IN  VARCHAR2,
  p_id_evaluacion                   OUT NUMBER,
  p_puntaje_total                   OUT NUMBER
)
IS
  v_criterio_dano_id  criterio.id_criterio%TYPE;
  v_puntaje_dano      criterio_valor.puntaje%TYPE;
  v_criterio_espec_id criterio.id_criterio%TYPE;
  v_puntaje_espec     criterio_valor.puntaje%TYPE;
BEGIN
  BEGIN
    SELECT c.id_criterio, cv.puntaje INTO v_criterio_dano_id, v_puntaje_dano
    FROM criterio c
    JOIN criterio_valor cv ON cv.id_criterio = c.id_criterio
    WHERE c.nombre = 'Dano potencial a la salud' AND cv.valor = p_dano_potencial_salud;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20010, 'Valor invalido para el criterio de dano potencial a la salud.');
  END;

  BEGIN
    SELECT c.id_criterio, cv.puntaje INTO v_criterio_espec_id, v_puntaje_espec
    FROM criterio c
    JOIN criterio_valor cv ON cv.id_criterio = c.id_criterio
    WHERE c.nombre = 'Criterio tecnico especializado UVC' AND cv.valor = p_requiere_criterio_especializado;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20011, 'Valor invalido para el criterio tecnico especializado de UVC.');
  END;

  INSERT INTO evaluacion_criticidad (
    denuncia_id, funcionario_validador_id, vinculado_comercio_electronico,
    observaciones_tecnicas
  ) VALUES (
    p_denuncia_id, p_funcionario_validador_id, p_vinculado_comercio_electronico,
    p_observaciones_tecnicas
  ) RETURNING id_evaluacion INTO p_id_evaluacion;

  INSERT INTO respuesta_criterio (id_evaluacion, criterio_id, valor_seleccionado, puntaje_obtenido)
  VALUES (p_id_evaluacion, v_criterio_dano_id, p_dano_potencial_salud, v_puntaje_dano);

  INSERT INTO respuesta_criterio (id_evaluacion, criterio_id, valor_seleccionado, puntaje_obtenido)
  VALUES (p_id_evaluacion, v_criterio_espec_id, p_requiere_criterio_especializado, v_puntaje_espec);

  p_puntaje_total := v_puntaje_dano + v_puntaje_espec;

  UPDATE evaluacion_criticidad
     SET puntaje_total = p_puntaje_total
   WHERE id_evaluacion = p_id_evaluacion;

  sp_insertar_bitacora(
    p_denuncia_id, p_funcionario_validador_id, NULL, 'evaluacion_tecnica',
    UNISTR('Se registraron los criterios t\00E9cnicos de la denuncia:') || CHR(10) ||
    UNISTR('- Nivel de da\00F1o potencial a la salud: ') || fn_valor_legible(p_dano_potencial_salud) || CHR(10) ||
    UNISTR('- \00BFRequiere criterio t\00E9cnico especializado de UVC?: ') || fn_valor_legible(p_requiere_criterio_especializado) || CHR(10) ||
    UNISTR('- \00BFRelaci\00F3n con comercio electr\00F3nico?: ') || fn_valor_legible(p_vinculado_comercio_electronico) || CHR(10) ||
    'Puntaje total: ' || TO_CHAR(p_puntaje_total) || '.'
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_registrar_criterios_tecnicos;
/

-- Resuelve el nombre de catalogo (tabla criterio) a partir de una clave
-- estable, para no exponer el texto exacto de criterio.nombre como
-- contrato publico entre capas (backend/frontend solo conocen la clave).
CREATE OR REPLACE FUNCTION fn_nombre_criterio_tecnico(p_criterio_clave IN VARCHAR2)
RETURN VARCHAR2
IS
BEGIN
  CASE p_criterio_clave
    WHEN 'DANO_POTENCIAL_SALUD' THEN RETURN 'Dano potencial a la salud';
    WHEN 'CRITERIO_ESPECIALIZADO_UVC' THEN RETURN 'Criterio tecnico especializado UVC';
    ELSE RAISE_APPLICATION_ERROR(-20014, 'Clave de criterio tecnico no reconocida: ' || p_criterio_clave);
  END CASE;
END fn_nombre_criterio_tecnico;
/

CREATE OR REPLACE PROCEDURE sp_actualizar_criterio_tecnico (
  p_denuncia_id      IN  NUMBER,
  p_funcionario_id   IN  NUMBER,
  p_criterio_clave   IN  VARCHAR2,
  p_valor_seleccionado IN VARCHAR2,
  p_justificacion    IN  VARCHAR2,
  p_id_evaluacion    OUT NUMBER,
  p_puntaje_total    OUT NUMBER
)
IS
  v_criterio_nombre VARCHAR2(150) := fn_nombre_criterio_tecnico(p_criterio_clave);
  v_criterio_id     criterio.id_criterio%TYPE;
  v_puntaje         criterio_valor.puntaje%TYPE;
  v_valor_anterior  respuesta_criterio.valor_seleccionado%TYPE;
BEGIN
  BEGIN
    SELECT id_evaluacion INTO p_id_evaluacion
    FROM (
      SELECT id_evaluacion
      FROM evaluacion_criticidad
      WHERE denuncia_id = p_denuncia_id
      ORDER BY fecha_evaluacion DESC, id_evaluacion DESC
    )
    WHERE ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20012, 'La denuncia no tiene una evaluacion de criticidad. Registre los criterios tecnicos primero.');
  END;

  BEGIN
    SELECT c.id_criterio, cv.puntaje INTO v_criterio_id, v_puntaje
    FROM criterio c
    JOIN criterio_valor cv ON cv.id_criterio = c.id_criterio
    WHERE c.nombre = v_criterio_nombre AND cv.valor = p_valor_seleccionado;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20010, 'Valor invalido para el criterio tecnico "' || v_criterio_nombre || '".');
  END;

  -- Trazabilidad: se guarda el valor anterior ANTES del MERGE para que
  -- la bitacora registre el cambio completo (de "X" a "Y"), no solo el
  -- valor nuevo. NULL = el criterio no tenia respuesta (se agrega).
  BEGIN
    SELECT valor_seleccionado INTO v_valor_anterior
    FROM respuesta_criterio
    WHERE id_evaluacion = p_id_evaluacion AND criterio_id = v_criterio_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      v_valor_anterior := NULL;
  END;

  -- Panel de justificacion obligatorio: solo cuando se reemplaza un valor
  -- que el funcionario validador ya habia registrado (edicion real). Si el
  -- criterio no tenia respuesta previa (boton "Agregar", evaluaciones de
  -- cuando si se permitia eliminar criterios), no se exige. Se valida aqui
  -- ademas de en el backend C# (CriteriosTecnicosValidator) porque este SP
  -- tambien puede invocarse fuera de la API (ej. scripts de migracion).
  IF v_valor_anterior IS NOT NULL AND LENGTH(TRIM(p_justificacion)) IS NULL THEN
    RAISE_APPLICATION_ERROR(-20015, 'Debe indicar la justificacion del cambio para editar un criterio ya registrado.');
  END IF;

  -- MERGE en vez de UPDATE: si el criterio no tenia respuesta previa
  -- (evaluaciones de cuando se permitia eliminar criterios), el
  -- superusuario la agrega en vez de que la actualizacion no haga nada.
  MERGE INTO respuesta_criterio rc
  USING (SELECT p_id_evaluacion AS id_evaluacion, v_criterio_id AS criterio_id FROM DUAL) src
  ON (rc.id_evaluacion = src.id_evaluacion AND rc.criterio_id = src.criterio_id)
  WHEN MATCHED THEN UPDATE SET valor_seleccionado = p_valor_seleccionado, puntaje_obtenido = v_puntaje
  WHEN NOT MATCHED THEN INSERT (id_evaluacion, criterio_id, valor_seleccionado, puntaje_obtenido)
    VALUES (p_id_evaluacion, v_criterio_id, p_valor_seleccionado, v_puntaje);

  SELECT NVL(SUM(puntaje_obtenido), 0) INTO p_puntaje_total
  FROM respuesta_criterio
  WHERE id_evaluacion = p_id_evaluacion;

  UPDATE evaluacion_criticidad
     SET puntaje_total = p_puntaje_total
   WHERE id_evaluacion = p_id_evaluacion;

  sp_insertar_bitacora(
    p_denuncia_id, p_funcionario_id, NULL, 'evaluacion_tecnica',
    CASE
      WHEN v_valor_anterior IS NULL THEN
        UNISTR('El superusuario agreg\00F3 el criterio "') || fn_etiqueta_criterio_tecnico(p_criterio_clave) ||
        '" con la respuesta "' || fn_valor_legible(p_valor_seleccionado) || '".'
      ELSE
        UNISTR('El superusuario cambi\00F3 el criterio "') || fn_etiqueta_criterio_tecnico(p_criterio_clave) || '":' || CHR(10) ||
        '- Antes: "' || fn_valor_legible(v_valor_anterior) || '"' || CHR(10) ||
        '- Ahora: "' || fn_valor_legible(p_valor_seleccionado) || '"' || CHR(10) ||
        UNISTR('- Justificaci\00F3n: "') || TRIM(p_justificacion) || '"'
    END || CHR(10) ||
    'Nuevo puntaje total: ' || TO_CHAR(p_puntaje_total) || '.'
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_actualizar_criterio_tecnico;
/

CREATE OR REPLACE PROCEDURE sp_actualizar_metadata_criterios_tecnicos (
  p_denuncia_id                     IN  NUMBER,
  p_funcionario_id                  IN  NUMBER,
  p_vinculado_comercio_electronico  IN  CHAR,
  p_observaciones_tecnicas          IN  VARCHAR2,
  p_justificacion                   IN  VARCHAR2,   
  p_id_evaluacion                   OUT NUMBER
)
IS
  v_comercio_anterior      evaluacion_criticidad.vinculado_comercio_electronico%TYPE;
  v_observaciones_anterior evaluacion_criticidad.observaciones_tecnicas%TYPE;
  v_cambios                VARCHAR2(8000);

  -- Texto entre comillas para la bitacora (o "(vacío)" si no habia dato);
  -- se recorta a 300 caracteres para que el valor anterior + el nuevo
  -- quepan en bitacora.contenido (2000).
  FUNCTION fn_texto(p_valor IN VARCHAR2) RETURN VARCHAR2 IS
  BEGIN
    IF p_valor IS NULL THEN
      RETURN UNISTR('(vac\00EDo)');
    ELSIF LENGTH(p_valor) > 300 THEN
      RETURN '"' || SUBSTR(p_valor, 1, 300) || '..."';
    END IF;
    RETURN '"' || p_valor || '"';
  END fn_texto;

  -- Una linea por campo modificado: nombre del campo como se ve en
  -- pantalla, y debajo lo que decia antes y lo que dice ahora.
  FUNCTION fn_linea_cambio(p_campo IN VARCHAR2, p_antes IN VARCHAR2, p_ahora IN VARCHAR2) RETURN VARCHAR2 IS
  BEGIN
    RETURN CHR(10) || '- ' || p_campo || CHR(10) ||
           '  Antes: ' || p_antes || CHR(10) ||
           '  Ahora: ' || p_ahora;
  END fn_linea_cambio;
  BEGIN
    BEGIN
    SELECT id_evaluacion, vinculado_comercio_electronico, observaciones_tecnicas
      INTO p_id_evaluacion, v_comercio_anterior, v_observaciones_anterior
    FROM (
      SELECT id_evaluacion, vinculado_comercio_electronico, observaciones_tecnicas
      FROM evaluacion_criticidad
      WHERE denuncia_id = p_denuncia_id
      ORDER BY fecha_evaluacion DESC, id_evaluacion DESC
    )
    WHERE ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20012, 'La denuncia no tiene una evaluacion de criticidad. Registre los criterios tecnicos primero.');
  END;

  -- Panel de justificacion obligatorio: solo cuando "Relacion con comercio
  -- electronico" ya tenia un valor registrado y realmente cambia (edicion
  -- real). Mismo criterio que sp_actualizar_criterio_tecnico.
  IF v_comercio_anterior IS NOT NULL
     AND NVL(v_comercio_anterior, '-') <> NVL(p_vinculado_comercio_electronico, '-')
     AND LENGTH(TRIM(p_justificacion)) IS NULL THEN
    RAISE_APPLICATION_ERROR(-20015, 'Debe indicar la justificacion del cambio para editar un criterio ya registrado.');
  END IF;

  UPDATE evaluacion_criticidad
     SET vinculado_comercio_electronico = p_vinculado_comercio_electronico,
         observaciones_tecnicas = p_observaciones_tecnicas
   WHERE id_evaluacion = p_id_evaluacion;

  -- Trazabilidad: se registra en bitacora cada campo que realmente cambio,
  -- con su valor anterior y el nuevo. Estos datos no suman al puntaje, por
  -- lo que no recalculan la criticidad ni reinician el plazo.
  IF NVL(v_comercio_anterior, '-') <> NVL(p_vinculado_comercio_electronico, '-') THEN
    v_cambios := v_cambios || fn_linea_cambio(
      UNISTR('\00BFRelaci\00F3n con comercio electr\00F3nico?'),
      fn_valor_legible(v_comercio_anterior), fn_valor_legible(p_vinculado_comercio_electronico));
    IF v_comercio_anterior IS NOT NULL THEN
      v_cambios := v_cambios || CHR(10) || '  ' || UNISTR('Justificaci\00F3n: ') || TRIM(p_justificacion);
    END IF;
  END IF;
  IF NVL(v_observaciones_anterior, CHR(0)) <> NVL(p_observaciones_tecnicas, CHR(0)) THEN
    v_cambios := v_cambios || fn_linea_cambio(
      UNISTR('Observaciones t\00E9cnicas'), fn_texto(v_observaciones_anterior), fn_texto(p_observaciones_tecnicas));
  END IF;

  IF v_cambios IS NOT NULL THEN
    sp_insertar_bitacora(
      p_denuncia_id, p_funcionario_id, NULL, 'evaluacion_tecnica',
      SUBSTRB(UNISTR('El superusuario modific\00F3 los datos generales de la evaluaci\00F3n:') || v_cambios, 1, 2000)
    );
  END IF;

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_actualizar_metadata_criterios_tecnicos;
/


-- #####################################################################
-- HU-011: evaluar criticidad
-- Origen: 12_sp_evaluar_criticidad.sql
-- #####################################################################
-- =====================================================
-- 12_sp_evaluar_criticidad.sql
-- Stored procedures para HU-011 "Evaluar criticidad de denuncias"
-- (#62): persistir el nivel de criticidad ya calculado en C#
-- (CriticidadCalculator, a partir de los rangos de
-- nivel_criticidad) y dejar constancia en bitacora de los dos
-- disparadores automaticos (AC #4 y AC #5).
--
-- El nivel se recibe ya calculado (no se recalcula aqui) para no
-- duplicar la logica de umbrales que ya vive en C# y esta cubierta
-- por pruebas unitarias (CriticidadCalculatorTests).
--
-- Las tres acciones son automaticas del sistema, no de un
-- funcionario puntual, por eso bitacora.funcionario_id va NULL
-- (ver comentario "NULL = accion automatica del sistema" en
-- SDPIS.sql).
--
-- Ejecutar DESPUES de 11_seed_nivel_criticidad.sql.
-- =====================================================

CREATE OR REPLACE PROCEDURE sp_registrar_evaluacion_criticidad (
  p_denuncia_id        IN NUMBER,
  p_nivel_criticidad   IN VARCHAR2,
  p_plazo_dias         IN NUMBER
)
IS
  v_id_evaluacion        evaluacion_criticidad.id_evaluacion%TYPE;
  v_nivel_anterior       evaluacion_criticidad.nivel_criticidad%TYPE;
  v_fecha_limite_anterior evaluacion_criticidad.fecha_limite%TYPE;
  v_fecha_limite_nueva   evaluacion_criticidad.fecha_limite%TYPE;
BEGIN
  BEGIN
    SELECT id_evaluacion, nivel_criticidad, fecha_limite
      INTO v_id_evaluacion, v_nivel_anterior, v_fecha_limite_anterior
    FROM (
      SELECT id_evaluacion, nivel_criticidad, fecha_limite
      FROM evaluacion_criticidad
      WHERE denuncia_id = p_denuncia_id
      ORDER BY fecha_evaluacion DESC, id_evaluacion DESC
    )
    WHERE ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20013, 'La denuncia no tiene una evaluacion de criticidad registrada.');
  END;

  -- p_nivel_criticidad invalido lo rechaza la FK_EVAL_NIVEL hacia
  -- nivel_criticidad(nivel), igual que hace HU-010 con los valores
  -- de criterio_valor: no se duplica esa validacion aqui.
  -- p_plazo_dias viene del mismo rango de nivel_criticidad que ya
  -- determino p_nivel_criticidad (plazos oficiales sembrados en
  -- 11_seed_nivel_criticidad.sql: ALTO 60, MEDIO 90, BAJO 120 dias).
  --
  -- El plazo SIEMPRE se cuenta desde el dia en que se (re)calcula la
  -- criticidad, no desde el registro original: acordado con el Product
  -- Owner (2026-09-24) que si el superusuario modifica los criterios
  -- tecnicos, lo justo para el funcionario es reiniciar el plazo al dia
  -- de la modificacion, segun el nuevo nivel.
  v_fecha_limite_nueva := TRUNC(SYSDATE) + p_plazo_dias;

  UPDATE evaluacion_criticidad
     SET nivel_criticidad = p_nivel_criticidad,
         fecha_limite = v_fecha_limite_nueva
   WHERE id_evaluacion = v_id_evaluacion;

  -- Trazabilidad: si ya habia un plazo corriendo, se deja constancia del
  -- reinicio (nivel y fecha limite anteriores vs. nuevos).
  sp_insertar_bitacora(
    p_denuncia_id, NULL, NULL, 'criticidad',
    CASE
      WHEN v_fecha_limite_anterior IS NULL THEN
        UNISTR('El sistema calcul\00F3 el nivel de criticidad de la denuncia: ') || INITCAP(p_nivel_criticidad) || '.' || CHR(10) ||
        'La denuncia tiene ' || TO_CHAR(p_plazo_dias) || UNISTR(' d\00EDas para ser atendida, ') ||
        UNISTR('con fecha l\00EDmite el ') || TO_CHAR(v_fecha_limite_nueva, 'DD/MM/YYYY') || '.'
      ELSE
        UNISTR('Como se modificaron los criterios t\00E9cnicos, el sistema volvi\00F3 a calcular el nivel de criticidad ') ||
        UNISTR('y reinici\00F3 el plazo de la denuncia a partir de hoy (') || TO_CHAR(TRUNC(SYSDATE), 'DD/MM/YYYY') || '):' || CHR(10) ||
        '- Nivel de criticidad: antes ' || NVL(INITCAP(v_nivel_anterior), 'sin nivel') ||
        ', ahora ' || INITCAP(p_nivel_criticidad) || CHR(10) ||
        UNISTR('- Fecha l\00EDmite: antes ') || TO_CHAR(v_fecha_limite_anterior, 'DD/MM/YYYY') ||
        ', ahora ' || TO_CHAR(v_fecha_limite_nueva, 'DD/MM/YYYY') ||
        ' (' || TO_CHAR(p_plazo_dias) || UNISTR(' d\00EDas)')
    END
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_registrar_evaluacion_criticidad;
/

-- AC #4: nivel Alto -> notificar a UVC sin cambio de area (el caso
-- sigue en el ARS correspondiente). "Notificar" hoy es dejar
-- constancia en bitacora, no un envio de correo real: el proyecto
-- no tiene infraestructura SMTP todavia.
CREATE OR REPLACE PROCEDURE sp_notificar_uvc_criticidad_alta (
  p_denuncia_id IN NUMBER
)
IS
BEGIN
  sp_insertar_bitacora(
    p_denuncia_id, NULL, NULL, 'notificacion',
    UNISTR('Como la denuncia tiene nivel de criticidad Alto, el sistema notific\00F3 a la UVC ') ||
    UNISTR('(uvc.correspondencia@minsalud.go.cr). La denuncia sigue en la misma \00E1rea.')
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_notificar_uvc_criticidad_alta;
/

-- AC #5: vinculado a comercio electronico -> disparo de
-- reasignacion a UVC. TODO: HU-013 "Gestionar reglas de
-- criticidad" (#64) debe reemplazar esto por la reasignacion real
-- de area_actual_id. Por ahora solo se deja constancia en bitacora.
CREATE OR REPLACE PROCEDURE sp_marcar_pendiente_reasignacion_uvc (
  p_denuncia_id IN NUMBER
)
IS
BEGIN
  sp_insertar_bitacora(
    p_denuncia_id, NULL, NULL, 'traslado',
    -- TODO: HU-013 debe implementar la reasignacion real a UVC.
    UNISTR('La denuncia est\00E1 relacionada con comercio electr\00F3nico, por lo que queda pendiente ') ||
    'de ser trasladada a la UVC.'
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_marcar_pendiente_reasignacion_uvc;
/

-- HU-010 permite editar criterios individuales
-- (sp_actualizar_criterio_tecnico en 10_sp_criterios_tecnicos.sql):
-- si tras esa operacion la evaluacion sigue incompleta (falta algun
-- criterio puntuable, solo posible en evaluaciones de cuando se
-- permitia eliminar criterios), el backend C#
-- llama aqui en vez de a sp_registrar_evaluacion_criticidad, para no
-- dejar un nivel_criticidad calculado sobre un puntaje parcial.
-- No hace nada (ni bitacora) si ya estaba en NULL, para no generar
-- ruido cada vez que se edita un criterio de una evaluacion que
-- todavia no tenia nivel calculado.
CREATE OR REPLACE PROCEDURE sp_limpiar_evaluacion_criticidad (
  p_denuncia_id IN NUMBER
)
IS
  v_id_evaluacion evaluacion_criticidad.id_evaluacion%TYPE;
  v_nivel_actual  evaluacion_criticidad.nivel_criticidad%TYPE;
  v_fecha_limite_actual evaluacion_criticidad.fecha_limite%TYPE;
BEGIN
  BEGIN
    SELECT id_evaluacion, nivel_criticidad, fecha_limite
      INTO v_id_evaluacion, v_nivel_actual, v_fecha_limite_actual
    FROM (
      SELECT id_evaluacion, nivel_criticidad, fecha_limite
      FROM evaluacion_criticidad
      WHERE denuncia_id = p_denuncia_id
      ORDER BY fecha_evaluacion DESC, id_evaluacion DESC
    )
    WHERE ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20013, 'La denuncia no tiene una evaluacion de criticidad registrada.');
  END;

  IF v_nivel_actual IS NULL THEN
    RETURN;
  END IF;

  UPDATE evaluacion_criticidad
     SET nivel_criticidad = NULL,
         fecha_limite = NULL
   WHERE id_evaluacion = v_id_evaluacion;

  sp_insertar_bitacora(
    p_denuncia_id, NULL, NULL, 'criticidad',
    UNISTR('Como falta completar los criterios t\00E9cnicos, la denuncia qued\00F3 sin nivel de criticidad ') ||
    '(antes era ' || INITCAP(v_nivel_actual) ||
    NVL2(v_fecha_limite_actual, UNISTR(', con fecha l\00EDmite el ') || TO_CHAR(v_fecha_limite_actual, 'DD/MM/YYYY'), '') ||
    ').' || CHR(10) ||
    UNISTR('El plazo volver\00E1 a empezar cuando se completen los criterios.')
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_limpiar_evaluacion_criticidad;
/


-- #####################################################################
-- HU-012: asignar encargado, reasignar area, confirmar y devolver (UVC)
-- Origen: 14_especialidad_permiso_alertas_uvc.sql (secciones 2, 3, 5 y 6; reemplazan a las de 13)
-- #####################################################################
-- =====================================================
-- 2) SP ASIGNAR ENCARGADO (CREATE OR REPLACE)
--    Añade la validación de permiso de asignación:
--    solo el jefe de área de la denuncia (activo) o un
--    funcionario activo con permite_asignar='S' puede
--    ejecutar la asignación.
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_asignar_funcionario_encargado (
  p_denuncia_id   IN NUMBER,
  p_encargado_id  IN NUMBER,
  p_realizado_por IN NUMBER,
  p_motivo        IN VARCHAR2
)
IS
  v_area_actual_id      denuncia.area_actual_id%TYPE;
  v_funcionario_previo  denuncia.funcionario_asignado_id%TYPE;
  v_nombre_previo       funcionario.nombre%TYPE;
  v_nombre_encargado    funcionario.nombre%TYPE;
  v_rol_encargado       funcionario.rol%TYPE;
  v_activo_encargado    funcionario.activo%TYPE;
  v_area_encargado      funcionario.area_id%TYPE;
  v_rol_emisor          funcionario.rol%TYPE;
  v_activo_emisor       funcionario.activo%TYPE;
  v_area_emisor         funcionario.area_id%TYPE;
  v_permite_emisor      funcionario.permite_asignar%TYPE;
  v_es_adicional        NUMBER;
  v_estado              denuncia.estado%TYPE;
BEGIN
  -- La denuncia debe existir. FOR UPDATE: se serializa con
  -- sp_solicitar_cierre_denuncia, que bloquea la misma fila.
  BEGIN
    SELECT area_actual_id, funcionario_asignado_id, estado
      INTO v_area_actual_id, v_funcionario_previo, v_estado
      FROM denuncia
     WHERE id_denuncia = p_denuncia_id
       FOR UPDATE;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20014, 'La denuncia indicada no existe.');
  END;

  -- HU-017: con el cierre en revision la denuncia queda congelada hasta que
  -- la jefatura lo resuelva (si no, la solicitud quedaria PENDIENTE para siempre).
  IF v_estado = 'EN_REVISION' THEN
    RAISE_APPLICATION_ERROR(-20029,
      UNISTR('La denuncia tiene una solicitud de cierre en revisi\00F3n por la jefatura: no se puede modificar hasta que se apruebe o rechace.'));
  END IF;

  -- Quien ejecuta debe existir y estar activo.
  BEGIN
    SELECT rol, activo, area_id, permite_asignar
      INTO v_rol_emisor, v_activo_emisor, v_area_emisor, v_permite_emisor
      FROM funcionario
     WHERE id_funcionario = p_realizado_por;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20021, 'El usuario que realiza la asignacion no existe.');
  END;

  -- Permiso de asignación: jefe de área de la denuncia o delegado con permiso.
  IF v_activo_emisor = 'N'
     OR NOT (v_rol_emisor = 'jefe_area' AND v_area_emisor = v_area_actual_id)
     AND v_permite_emisor != 'S' THEN
    RAISE_APPLICATION_ERROR(-20020, 'El usuario no cuenta con el permiso de asignacion de denuncias.');
  END IF;

  -- El encargado debe existir, estar activo y NO ser jefe de área.
  BEGIN
    SELECT nombre, rol, activo, area_id
      INTO v_nombre_encargado, v_rol_encargado, v_activo_encargado, v_area_encargado
      FROM funcionario
     WHERE id_funcionario = p_encargado_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20015, 'El funcionario encargado indicado no existe.');
  END;

  IF v_rol_encargado = 'jefe_area' THEN
    RAISE_APPLICATION_ERROR(-20016, 'El jefe de area no puede ser asignado como funcionario encargado.');
  END IF;

  IF v_activo_encargado = 'N' THEN
    RAISE_APPLICATION_ERROR(-20017, 'No se puede asignar un funcionario inactivo como encargado.');
  END IF;

  -- NUEVO (H16): el encargado debe pertenecer al área actual de la denuncia.
  -- Pertenece si es su area base o una area adicional (funcionario_area_adicional).
  IF v_area_encargado != v_area_actual_id THEN
    SELECT COUNT(*) INTO v_es_adicional
      FROM funcionario_area_adicional
     WHERE funcionario_id = p_encargado_id
       AND area_id = v_area_actual_id;
    IF v_es_adicional = 0 THEN
      RAISE_APPLICATION_ERROR(-20028, 'El funcionario encargado no pertenece al area actual de la denuncia.');
    END IF;
  END IF;

  -- Asignación del encargado; el estado pasa a EN_TIEMPO.
  UPDATE denuncia
     SET funcionario_asignado_id = p_encargado_id,
         estado = 'EN_TIEMPO'
   WHERE id_denuncia = p_denuncia_id;

  -- Registro del cambio en el historial de asignaciones.
  INSERT INTO historial_asignacion (
    denuncia_id, area_anterior_id, area_nueva_id,
    funcionario_anterior_id, funcionario_nuevo_id,
    realizado_por_id, motivo
  ) VALUES (
    p_denuncia_id, v_area_actual_id, v_area_actual_id,
    v_funcionario_previo, p_encargado_id,
    p_realizado_por, p_motivo
  );

  -- Bitácora (acción obligatoria, HU-012 AC #7). Si la denuncia ya tenia
  -- otro funcionario, la jefatura la esta reasignando y se deja quien la tenia.
  IF v_funcionario_previo IS NOT NULL AND v_funcionario_previo != p_encargado_id THEN
    SELECT nombre INTO v_nombre_previo
      FROM funcionario
     WHERE id_funcionario = v_funcionario_previo;
  END IF;

  sp_insertar_bitacora(
    p_denuncia_id, p_realizado_por, v_area_actual_id,
    'asignacion',
    CASE
      WHEN v_nombre_previo IS NOT NULL THEN
        'Denuncia reasignada del funcionario ' || v_nombre_previo ||
        ' al funcionario ' || v_nombre_encargado || '.'
      ELSE
        'Denuncia asignada al funcionario encargado ' || v_nombre_encargado || '.'
    END
  );

  -- Notificación al funcionario encargado.
  sp_insertar_notificacion(
    p_denuncia_id, 'asignacion', 'funcionario', p_encargado_id,
    'Se le ha asignado una denuncia como funcionario encargado.'
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_asignar_funcionario_encargado;
/

-- =====================================================
-- 3) SP REASIGNAR ÁREA (CREATE OR REPLACE)
--    Añade la misma validación de permiso de asignación
--    al reasignar a otra área (o UVC).
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_reasignar_denuncia_area (
  p_denuncia_id   IN NUMBER,
  p_area_nueva_id IN NUMBER,      -- NULL => área UVC (comercio electrónico)
  p_realizado_por IN NUMBER,
  p_motivo        IN VARCHAR2,
  p_numero_oficio IN VARCHAR2 DEFAULT NULL  -- solo al reasignar a UVC
)
IS
  v_area_anterior_id      denuncia.area_actual_id%TYPE;
  v_es_uvc                BOOLEAN := FALSE;
  v_tipo_destino          area.tipo%TYPE;
  v_funcionario_anterior  denuncia.funcionario_asignado_id%TYPE;
  v_area_nueva_id         area.id_area%TYPE;
  v_nombre_area_nueva     area.nombre%TYPE;
  v_jefe_area_nueva       funcionario.id_funcionario%TYPE;
  v_rol_emisor            funcionario.rol%TYPE;
  v_activo_emisor         funcionario.activo%TYPE;
  v_area_emisor           funcionario.area_id%TYPE;
  v_permite_emisor        funcionario.permite_asignar%TYPE;
  v_estado                denuncia.estado%TYPE;
BEGIN
  BEGIN
    SELECT area_actual_id, funcionario_asignado_id, estado
      INTO v_area_anterior_id, v_funcionario_anterior, v_estado
      FROM denuncia
     WHERE id_denuncia = p_denuncia_id
       FOR UPDATE;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20014, 'La denuncia indicada no existe.');
  END;

  -- HU-017: no se traslada una denuncia con el cierre en revision.
  IF v_estado = 'EN_REVISION' THEN
    RAISE_APPLICATION_ERROR(-20029,
      UNISTR('La denuncia tiene una solicitud de cierre en revisi\00F3n por la jefatura: no se puede modificar hasta que se apruebe o rechace.'));
  END IF;

  -- Quien ejecuta debe existir, estar activo y tener permiso de asignación.
  BEGIN
    SELECT rol, activo, area_id, permite_asignar
      INTO v_rol_emisor, v_activo_emisor, v_area_emisor, v_permite_emisor
      FROM funcionario
     WHERE id_funcionario = p_realizado_por;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20021, 'El usuario que realiza la reasignacion no existe.');
  END;

  IF v_activo_emisor = 'N'
     OR NOT (v_rol_emisor = 'jefe_area' AND v_area_emisor = v_area_anterior_id)
     AND v_permite_emisor != 'S' THEN
    RAISE_APPLICATION_ERROR(-20020, 'El usuario no cuenta con el permiso de asignacion de denuncias.');
  END IF;

  -- Resolución del área destino.
  IF p_area_nueva_id IS NOT NULL THEN
    BEGIN
      SELECT id_area, nombre
        INTO v_area_nueva_id, v_nombre_area_nueva
        FROM area
       WHERE id_area = p_area_nueva_id;
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20018, 'El area destino indicada no existe.');
    END;
  ELSE
    BEGIN
      SELECT id_area, nombre
        INTO v_area_nueva_id, v_nombre_area_nueva
        FROM area
       WHERE tipo = 'UVC'
         AND ROWNUM = 1;
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20019, 'No existe un area UVC configurada para la reasignacion.');
    END;
  END IF;

  -- Reasignar a UVC exige justificacion (queda en historial y bitacora).
  SELECT tipo INTO v_tipo_destino FROM area WHERE id_area = v_area_nueva_id;
  v_es_uvc := (v_tipo_destino = 'UVC');
  IF v_es_uvc AND TRIM(p_motivo) IS NULL THEN
    RAISE_APPLICATION_ERROR(-20030, 'Debe indicar la justificacion de la reasignacion a UVC.');
  END IF;

  -- NUEVO (H15): no permitir "reasignar" a la misma área en la que ya está.
  IF v_area_nueva_id = v_area_anterior_id THEN
    RAISE_APPLICATION_ERROR(-20022, 'La denuncia ya se encuentra en el area indicada.');
  END IF;

  -- La denuncia va al área destino, sin funcionario encargado,
  -- pendiente de nueva asignación.
  UPDATE denuncia
     SET area_actual_id = v_area_nueva_id,
         funcionario_asignado_id = NULL,
         estado = 'PENDIENTE_ASIGNACION'
   WHERE id_denuncia = p_denuncia_id;

  -- Registro del cambio en el historial de asignaciones.
  INSERT INTO historial_asignacion (
    denuncia_id, area_anterior_id, area_nueva_id,
    funcionario_anterior_id, funcionario_nuevo_id,
    realizado_por_id, motivo, numero_oficio
  ) VALUES (
    p_denuncia_id, v_area_anterior_id, v_area_nueva_id,
    v_funcionario_anterior, NULL,
    p_realizado_por, p_motivo,
    CASE WHEN v_es_uvc THEN TRIM(p_numero_oficio) END
  );

  -- Bitácora (acción obligatoria).
  sp_insertar_bitacora(
    p_denuncia_id, p_realizado_por, v_area_anterior_id,
    'traslado',
    'Denuncia reasignada al area ' || v_nombre_area_nueva || '.' ||
    CASE WHEN v_es_uvc THEN
      CHR(10) || UNISTR('Justificaci\00F3n: ') || TRIM(p_motivo) ||
      CASE WHEN TRIM(p_numero_oficio) IS NOT NULL
           THEN CHR(10) || UNISTR('N\00FAmero de oficio: ') || TRIM(p_numero_oficio) END
    END
  );

  -- Notificación al jefe del área destino (si existe).
  BEGIN
    SELECT id_funcionario
      INTO v_jefe_area_nueva
      FROM funcionario
     WHERE area_id = v_area_nueva_id
       AND rol = 'jefe_area'
       AND activo = 'S'
       AND ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      v_jefe_area_nueva := NULL;
  END;

  sp_insertar_notificacion(
    p_denuncia_id, 'reasignacion', 'jefe_area', v_jefe_area_nueva,
    'Se le ha reasignado una denuncia a su area.'
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_reasignar_denuncia_area;
/

-- =====================================================
-- 5) SP CONFIRMAR ATENCIÓN UVC
--    Superusuario de UVC confirma que la denuncia (ya con
--    funcionario encargado asignado en UVC) fue atendida.
--    Estado -> FINALIZADA.
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_confirmar_atencion_uvc (
  p_denuncia_id      IN NUMBER,
  p_superusuario_id  IN NUMBER,
  p_motivo           IN VARCHAR2
)
IS
  v_area_id       denuncia.area_actual_id%TYPE;
  v_area_tipo     area.tipo%TYPE;
  v_encargado_id  funcionario.id_funcionario%TYPE;
  v_consecutivo   denuncia.consecutivo%TYPE;
  v_rol           funcionario.rol%TYPE;
  v_activo        funcionario.activo%TYPE;
  v_estado        denuncia.estado%TYPE;
BEGIN
  BEGIN
    SELECT d.area_actual_id, a.tipo, d.funcionario_asignado_id, d.consecutivo, d.estado
      INTO v_area_id, v_area_tipo, v_encargado_id, v_consecutivo, v_estado
      FROM denuncia d
      JOIN area a ON a.id_area = d.area_actual_id
     WHERE d.id_denuncia = p_denuncia_id
       FOR UPDATE OF d.estado;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20014, 'La denuncia indicada no existe.');
  END;

  -- HU-017: con el cierre en revision solo la jefatura la finaliza.
  IF v_estado = 'EN_REVISION' THEN
    RAISE_APPLICATION_ERROR(-20029,
      UNISTR('La denuncia tiene una solicitud de cierre en revisi\00F3n por la jefatura: no se puede modificar hasta que se apruebe o rechace.'));
  END IF;

  IF v_area_tipo != 'UVC' THEN
    RAISE_APPLICATION_ERROR(-20023, 'Solo se puede confirmar la atencion de denuncias que estan en la UVC.');
  END IF;

  IF v_encargado_id IS NULL THEN
    RAISE_APPLICATION_ERROR(-20024, 'La denuncia debe tener un funcionario encargado asignado antes de confirmar.');
  END IF;

  BEGIN
    SELECT rol, activo
      INTO v_rol, v_activo
      FROM funcionario
     WHERE id_funcionario = p_superusuario_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20025, 'El superusuario indicado no existe.');
  END;

  IF v_rol NOT IN ('superusuario', 'jefe_area') OR v_activo = 'N' THEN
    RAISE_APPLICATION_ERROR(-20026, 'Solo un superusuario o jefe de area activo puede confirmar la atencion.');
  END IF;

  UPDATE denuncia SET estado = 'FINALIZADA' WHERE id_denuncia = p_denuncia_id;

  INSERT INTO historial_asignacion (
    denuncia_id, area_anterior_id, area_nueva_id,
    funcionario_anterior_id, funcionario_nuevo_id, realizado_por_id, motivo
  ) VALUES (
    p_denuncia_id, v_area_id, v_area_id, v_encargado_id, v_encargado_id,
    p_superusuario_id,
    COALESCE('Confirmacion de atencion UVC: ' || p_motivo, 'Confirmacion de atencion UVC')
  );

  sp_insertar_bitacora(
    p_denuncia_id, p_superusuario_id, v_area_id,
    'traslado',
    'Denuncia ' || v_consecutivo || ' confirmada como atendida en la UVC.'
  );

  sp_insertar_notificacion(
    p_denuncia_id, 'confirmacion_uvc', 'funcionario', v_encargado_id,
    'La denuncia fue confirmada como atendida por la UVC.'
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_confirmar_atencion_uvc;
/

-- =====================================================
-- 6) SP DEVOLVER A ÁREA RECTORA
--    Superusuario de UVC devuelve la denuncia al Área
--    Rectora de origen/indicada. Se libera el encargado y
--    vuelve a PENDIENTE_ASIGNACION para nueva asignación.
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_devolver_area_rectora (
  p_denuncia_id      IN NUMBER,
  p_area_rectora_id  IN NUMBER,
  p_superusuario_id  IN NUMBER,
  p_motivo           IN VARCHAR2
)
IS
  v_area_anterior_id   denuncia.area_actual_id%TYPE;
  v_area_tipo          area.tipo%TYPE;
  v_funcionario_anterior funcionario.id_funcionario%TYPE;
  v_consecutivo        denuncia.consecutivo%TYPE;
  v_rol                funcionario.rol%TYPE;
  v_activo             funcionario.activo%TYPE;
  v_nombre_area        area.nombre%TYPE;
  v_jefe_area_nueva    funcionario.id_funcionario%TYPE;
  v_estado             denuncia.estado%TYPE;
BEGIN
  BEGIN
    SELECT d.area_actual_id, a.tipo, d.funcionario_asignado_id, d.consecutivo, d.estado
      INTO v_area_anterior_id, v_area_tipo, v_funcionario_anterior, v_consecutivo, v_estado
      FROM denuncia d
      JOIN area a ON a.id_area = d.area_actual_id
     WHERE d.id_denuncia = p_denuncia_id
       FOR UPDATE OF d.estado;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20014, 'La denuncia indicada no existe.');
  END;

  -- HU-017: no se devuelve una denuncia con el cierre en revision.
  IF v_estado = 'EN_REVISION' THEN
    RAISE_APPLICATION_ERROR(-20029,
      UNISTR('La denuncia tiene una solicitud de cierre en revisi\00F3n por la jefatura: no se puede modificar hasta que se apruebe o rechace.'));
  END IF;

  IF v_area_tipo != 'UVC' THEN
    RAISE_APPLICATION_ERROR(-20023, 'Solo se puede devolver denuncias que estan en la UVC.');
  END IF;

  -- La justificacion es obligatoria (queda en historial y bitacora).
  IF TRIM(p_motivo) IS NULL THEN
    RAISE_APPLICATION_ERROR(-20031, 'Debe indicar la justificacion de la devolucion al area rectora.');
  END IF;

  BEGIN
    SELECT rol, activo
      INTO v_rol, v_activo
      FROM funcionario
     WHERE id_funcionario = p_superusuario_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20025, 'El superusuario indicado no existe.');
  END;

  IF v_rol NOT IN ('superusuario', 'jefe_area') OR v_activo = 'N' THEN
    RAISE_APPLICATION_ERROR(-20026, 'Solo un superusuario o jefe de area activo puede devolver la denuncia.');
  END IF;

  BEGIN
    SELECT nombre
      INTO v_nombre_area
      FROM area
     WHERE id_area = p_area_rectora_id
       AND tipo IN ('AR','ARS');
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20027, 'El area rectora destino no existe o no es valida.');
  END;

  UPDATE denuncia
     SET area_actual_id = p_area_rectora_id,
         funcionario_asignado_id = NULL,
         estado = 'PENDIENTE_ASIGNACION'
   WHERE id_denuncia = p_denuncia_id;

  INSERT INTO historial_asignacion (
    denuncia_id, area_anterior_id, area_nueva_id,
    funcionario_anterior_id, funcionario_nuevo_id, realizado_por_id, motivo
  ) VALUES (
    p_denuncia_id, v_area_anterior_id, p_area_rectora_id,
    v_funcionario_anterior, NULL,
    p_superusuario_id,
    COALESCE('Devolucion a area rectora: ' || p_motivo, 'Devolucion a area rectora')
  );

  sp_insertar_bitacora(
    p_denuncia_id, p_superusuario_id, v_area_anterior_id,
    'traslado',
    'Denuncia ' || v_consecutivo || ' devuelta al area rectora ' || v_nombre_area || '.' ||
    CHR(10) || UNISTR('Justificaci\00F3n: ') || TRIM(p_motivo)
  );

  BEGIN
    SELECT id_funcionario
      INTO v_jefe_area_nueva
      FROM funcionario
     WHERE area_id = p_area_rectora_id
       AND rol = 'jefe_area'
       AND activo = 'S'
       AND ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      v_jefe_area_nueva := NULL;
  END;

  sp_insertar_notificacion(
    p_denuncia_id, 'devolucion_area_rectora', 'jefe_area', v_jefe_area_nueva,
    'Su area debe reasignar la denuncia devuelta por la UVC.'
  );

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_devolver_area_rectora;
/


-- #####################################################################
-- HU-012: alerta de vencimiento de asignacion (version final)
-- Origen: 15_alerta_vencimiento_3dias.sql
-- #####################################################################
-- =====================================================
-- 15_alerta_vencimiento_3dias.sql
-- Ajuste del SP de alerta de vencimiento de asignación
-- (HU-012) para que:
--   1) Incluya el dia 3 exacto (>= 3, antes > 3).
--   2) En areas UVC (sin jefe de area) notifique al
--      superusuario activo de esa area.
--
-- CREATE OR REPLACE un SP ya existente (creado en 14);
-- puede ejecutarse de nuevo sin problemas.
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_generar_alerta_vencimiento_asignacion (
  p_area_id   IN NUMBER,
  p_generadas OUT NUMBER
)
IS
BEGIN
  INSERT INTO notificacion (denuncia_id, tipo, destinatario_tipo, destinatario_funcionario_id, mensaje)
  SELECT d.id_denuncia,
         'alerta_vencimiento_asignacion',
         CASE WHEN a.tipo = 'UVC' THEN 'superusuario' ELSE 'jefe_area' END,
         CASE WHEN a.tipo = 'UVC' THEN su.id_funcionario ELSE f.id_funcionario END,
         'La denuncia ' || d.consecutivo || ' lleva 3 dias o mas sin funcionario encargado asignado.'
    FROM denuncia d
    JOIN area a ON a.id_area = d.area_actual_id
    LEFT JOIN funcionario f  ON f.area_id = d.area_actual_id
                            AND f.rol = 'jefe_area'
                            AND f.activo = 'S'
    LEFT JOIN funcionario su ON su.area_id = d.area_actual_id
                            AND su.rol = 'superusuario'
                            AND su.activo = 'S'
   WHERE d.estado = 'PENDIENTE_ASIGNACION'
     AND d.funcionario_asignado_id IS NULL
     AND d.area_actual_id = p_area_id
     AND TRUNC(SYSDATE) - TRUNC(d.fecha_creacion) >= 3
     AND ( (a.tipo = 'UVC' AND su.id_funcionario IS NOT NULL)
           OR (a.tipo != 'UVC' AND f.id_funcionario IS NOT NULL) )
     AND NOT EXISTS (
       SELECT 1 FROM notificacion n
        WHERE n.denuncia_id = d.id_denuncia
          AND n.tipo = 'alerta_vencimiento_asignacion' );

  p_generadas := SQL%ROWCOUNT;

  -- Bitácora de la acción automática del sistema (funcionario NULL).
  FOR r IN (
    SELECT d.id_denuncia
      FROM denuncia d
     WHERE d.estado = 'PENDIENTE_ASIGNACION'
       AND d.funcionario_asignado_id IS NULL
       AND d.area_actual_id = p_area_id
       AND TRUNC(SYSDATE) - TRUNC(d.fecha_creacion) >= 3
  ) LOOP
    sp_insertar_bitacora(
      r.id_denuncia, NULL, NULL,
      'notificacion',
      'Alerta de vencimiento del plazo de asignacion (3 dias).'
    );
  END LOOP;

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_generar_alerta_vencimiento_asignacion;
/

COMMIT;


-- #####################################################################
-- HU-018: solicitar cierre de denuncia
-- #####################################################################
-- =====================================================
-- SP SOLICITAR CIERRE: en UNA transaccion registra la solicitud, pasa
-- la denuncia a EN_REVISION (automatico, no es una edicion manual del
-- estado), deja la accion en bitacora y notifica a la jefatura del area.
--   * Solo el funcionario responsable asignado (funcionario_asignado_id),
--     activo, puede solicitar.
--   * Solo denuncias en curso: EN_TIEMPO, PRONTA_VENCER o ATRASADA.
--   * Notifica a los jefes de area activos del area actual; si el area
--     no tiene jefe activo, a los superusuarios activos, para que la
--     solicitud nunca quede sin destinatario.
-- La aprobacion/rechazo es HU-017.
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_solicitar_cierre_denuncia (
  p_denuncia_id           IN  NUMBER,
  p_funcionario_id        IN  NUMBER,
  p_resolucion_final      IN  VARCHAR2,
  p_observaciones_finales IN  VARCHAR2,
  p_acciones_realizadas   IN  VARCHAR2,
  p_id_solicitud          OUT NUMBER
)
IS
  v_estado          denuncia.estado%TYPE;
  v_area_id         denuncia.area_actual_id%TYPE;
  v_asignado_id     denuncia.funcionario_asignado_id%TYPE;
  v_consecutivo     denuncia.consecutivo%TYPE;
  v_nombre_area     area.nombre%TYPE;
  v_activo          funcionario.activo%TYPE;
  v_mensaje         notificacion.mensaje%TYPE;
BEGIN
  -- La denuncia debe existir. FOR UPDATE: dos solicitudes simultaneas
  -- sobre la misma denuncia se serializan aqui.
  BEGIN
    SELECT d.estado, d.area_actual_id, d.funcionario_asignado_id, d.consecutivo, a.nombre
      INTO v_estado, v_area_id, v_asignado_id, v_consecutivo, v_nombre_area
      FROM denuncia d
      JOIN area a ON a.id_area = d.area_actual_id
     WHERE d.id_denuncia = p_denuncia_id
       FOR UPDATE OF d.estado;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20180, 'La denuncia indicada no existe.');
  END;

  -- Solo el funcionario responsable asignado, y activo.
  BEGIN
    SELECT activo INTO v_activo FROM funcionario WHERE id_funcionario = p_funcionario_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      v_activo := 'N';
  END;

  IF v_activo != 'S' OR v_asignado_id IS NULL OR v_asignado_id != p_funcionario_id THEN
    RAISE_APPLICATION_ERROR(-20181,
      'Solo el funcionario responsable asignado a la denuncia puede solicitar su cierre.');
  END IF;

  IF v_estado = 'EN_REVISION' THEN
    RAISE_APPLICATION_ERROR(-20182,
      UNISTR('La denuncia ya tiene una solicitud de cierre pendiente de revisi\00F3n por la jefatura.'));
  END IF;

  IF v_estado NOT IN ('EN_TIEMPO', 'PRONTA_VENCER', 'ATRASADA') THEN
    RAISE_APPLICATION_ERROR(-20183,
      UNISTR('Solo se puede solicitar el cierre de una denuncia en curso (en tiempo, pr\00F3xima a vencer o atrasada).'));
  END IF;

  IF TRIM(p_resolucion_final) IS NULL
     OR TRIM(p_observaciones_finales) IS NULL
     OR TRIM(p_acciones_realizadas) IS NULL THEN
    RAISE_APPLICATION_ERROR(-20184,
      UNISTR('La resoluci\00F3n final, las observaciones finales y las acciones realizadas son obligatorias.'));
  END IF;

  INSERT INTO solicitud_cierre (
    denuncia_id, solicitado_por_id, area_id,
    resolucion_final, observaciones_finales, acciones_realizadas,
    estado_previo  -- HU-017: estado al que vuelve la denuncia si se rechaza
  ) VALUES (
    p_denuncia_id, p_funcionario_id, v_area_id,
    TRIM(p_resolucion_final), TRIM(p_observaciones_finales), TRIM(p_acciones_realizadas),
    v_estado
  )
  RETURNING id_solicitud_cierre INTO p_id_solicitud;

  -- Cambio de estado automatico como consecuencia de la solicitud.
  UPDATE denuncia
     SET estado = 'EN_REVISION'
   WHERE id_denuncia = p_denuncia_id;

  -- Bitacora (accion obligatoria): usuario, fecha y hora los pone
  -- sp_insertar_bitacora (funcionario_id + SYSTIMESTAMP).
  sp_insertar_bitacora(
    p_denuncia_id, p_funcionario_id, v_area_id,
    'solicitud_cierre',
    UNISTR('Solicitud de cierre enviada a la jefatura de ') || v_nombre_area
      || UNISTR(' para su evaluaci\00F3n. La denuncia pasa a estado En revisi\00F3n.')
  );

  -- Notificacion a la jefatura del area.
  v_mensaje := UNISTR('Se solicit\00F3 el cierre de la denuncia ') || v_consecutivo
               || UNISTR('. Queda pendiente de su evaluaci\00F3n.');

  INSERT INTO notificacion (denuncia_id, tipo, destinatario_tipo, destinatario_funcionario_id, mensaje)
  SELECT p_denuncia_id, 'solicitud_cierre', 'jefe_area', f.id_funcionario, v_mensaje
    FROM funcionario f
   WHERE f.area_id = v_area_id
     AND f.rol = 'jefe_area'
     AND f.activo = 'S';

  IF SQL%ROWCOUNT = 0 THEN
    INSERT INTO notificacion (denuncia_id, tipo, destinatario_tipo, destinatario_funcionario_id, mensaje)
    SELECT p_denuncia_id, 'solicitud_cierre', 'superusuario', f.id_funcionario, v_mensaje
      FROM funcionario f
     WHERE f.rol = 'superusuario'
       AND f.activo = 'S';
  END IF;

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_solicitar_cierre_denuncia;
/

COMMIT;


-- #####################################################################
-- HU-017: aprobar o rechazar la resolucion de una denuncia
-- #####################################################################
-- =====================================================
-- SP RESOLVER SOLICITUD DE CIERRE: la jefatura aprueba o rechaza la
-- solicitud de cierre pendiente (HU-018). En UNA transaccion registra la
-- decision (decision_cierre), cierra la solicitud, cambia el estado de la
-- denuncia de forma automatica (nadie lo edita a mano), deja la transicion
-- en bitacora y notifica al funcionario que solicito el cierre.
--   * p_decision: APROBADA | RECHAZADA.
--   * APROBADA  -> denuncia FINALIZADA + fila en denuncia_cierre.
--   * RECHAZADA -> la denuncia vuelve a solicitud_cierre.estado_previo
--     (EN_TIEMPO, PRONTA_VENCER o ATRASADA). Si estado_previo es NULL
--     (solicitud registrada antes de HU-017) vuelve a EN_TIEMPO.
--   * Observaciones: obligatorias al rechazar, opcionales al aprobar.
--   * Resuelve un jefe de area ACTIVO del area de la solicitud
--     (solicitud_cierre.area_id) o un superusuario ACTIVO de cualquier area
--     (p. ej. si el area no tiene jefe activo; sp_solicitar_cierre_denuncia
--     ya les avisa en ese caso).
--   * Solo solicitudes PENDIENTE de denuncias EN_REVISION.
-- Bloquea primero la denuncia y despues la solicitud (mismo orden que
-- sp_solicitar_cierre_denuncia) para que dos decisiones simultaneas se
-- serialicen sin interbloqueos. Errores de negocio: -20170 a -20175.
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_resolver_solicitud_cierre (
  p_solicitud_id   IN  NUMBER,
  p_jefe_id        IN  NUMBER,
  p_decision       IN  VARCHAR2,
  p_observaciones  IN  VARCHAR2,
  p_estado_final   OUT VARCHAR2
)
IS
  v_decision        VARCHAR2(4000);  -- sin %TYPE: un valor invalido largo da -20170, no ORA-06502
  v_observaciones   decision_cierre.observaciones_jefatura%TYPE;
  v_denuncia_id     solicitud_cierre.denuncia_id%TYPE;
  v_area_solicitud  solicitud_cierre.area_id%TYPE;
  v_estado_sol      solicitud_cierre.estado%TYPE;
  v_estado_previo   solicitud_cierre.estado_previo%TYPE;
  v_solicitante_id  solicitud_cierre.solicitado_por_id%TYPE;
  v_estado_den      denuncia.estado%TYPE;
  v_consecutivo     denuncia.consecutivo%TYPE;
  v_nombre_area     area.nombre%TYPE;
  v_rol             funcionario.rol%TYPE;
  v_activo          funcionario.activo%TYPE;
  v_area_jefe       funcionario.area_id%TYPE;
  v_estado_nuevo    denuncia.estado%TYPE;
  v_etiqueta_estado VARCHAR2(40);
  v_quien           VARCHAR2(400);  -- "la jefatura de <area>" o "un superusuario"
  v_contenido       VARCHAR2(4000);
  v_mensaje         notificacion.mensaje%TYPE;
BEGIN
  v_decision      := UPPER(TRIM(p_decision));
  v_observaciones := TRIM(p_observaciones);

  IF v_decision IS NULL OR v_decision NOT IN ('APROBADA', 'RECHAZADA') THEN
    RAISE_APPLICATION_ERROR(-20170,
      UNISTR('La decisi\00F3n debe ser APROBADA o RECHAZADA.'));
  END IF;

  IF v_decision = 'RECHAZADA' AND v_observaciones IS NULL THEN
    RAISE_APPLICATION_ERROR(-20171,
      UNISTR('Debe indicar las observaciones de la jefatura para rechazar la resoluci\00F3n.'));
  END IF;

  -- La solicitud debe existir (lectura sin bloqueo solo para ubicar la denuncia).
  BEGIN
    SELECT denuncia_id INTO v_denuncia_id
      FROM solicitud_cierre
     WHERE id_solicitud_cierre = p_solicitud_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20172, 'La solicitud de cierre indicada no existe.');
  END;

  -- Bloqueo: denuncia primero, luego la solicitud.
  SELECT d.estado, d.consecutivo
    INTO v_estado_den, v_consecutivo
    FROM denuncia d
   WHERE d.id_denuncia = v_denuncia_id
     FOR UPDATE OF d.estado;

  SELECT s.area_id, s.estado, s.estado_previo, s.solicitado_por_id, a.nombre
    INTO v_area_solicitud, v_estado_sol, v_estado_previo, v_solicitante_id, v_nombre_area
    FROM solicitud_cierre s
    JOIN area a ON a.id_area = s.area_id
   WHERE s.id_solicitud_cierre = p_solicitud_id
     FOR UPDATE OF s.estado;

  -- La jefatura activa del area de la solicitud o un superusuario activo.
  BEGIN
    SELECT rol, activo, area_id
      INTO v_rol, v_activo, v_area_jefe
      FROM funcionario
     WHERE id_funcionario = p_jefe_id;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      v_activo := 'N';
  END;

  IF v_activo != 'S'
     OR NOT (v_rol = 'superusuario'
             OR (v_rol = 'jefe_area' AND v_area_jefe = v_area_solicitud)) THEN
    RAISE_APPLICATION_ERROR(-20173,
      UNISTR('Solo la jefatura del \00E1rea a la que se envi\00F3 la solicitud o un superusuario puede aprobar o rechazar la resoluci\00F3n.'));
  END IF;

  v_quien := CASE v_rol
               WHEN 'superusuario' THEN 'un superusuario'
               ELSE 'la jefatura de ' || v_nombre_area
             END;

  IF v_estado_sol != 'PENDIENTE' THEN
    RAISE_APPLICATION_ERROR(-20174,
      'La solicitud de cierre ya fue resuelta por la jefatura.');
  END IF;

  IF v_estado_den != 'EN_REVISION' THEN
    RAISE_APPLICATION_ERROR(-20175,
      UNISTR('La denuncia no est\00E1 en revisi\00F3n: no se puede resolver la solicitud de cierre.'));
  END IF;

  -- Decision (usuario, fecha y hora) y cierre de la solicitud.
  INSERT INTO decision_cierre (solicitud_cierre_id, decidido_por_id, decision, observaciones_jefatura)
  VALUES (p_solicitud_id, p_jefe_id, v_decision, v_observaciones);

  UPDATE solicitud_cierre
     SET estado = v_decision
   WHERE id_solicitud_cierre = p_solicitud_id;

  -- Cambio de estado automatico como consecuencia de la decision.
  IF v_decision = 'APROBADA' THEN
    v_estado_nuevo := 'FINALIZADA';

    INSERT INTO denuncia_cierre (denuncia_id, aprobado_por_jefe_id, fecha_finalizacion)
    VALUES (v_denuncia_id, p_jefe_id, SYSDATE);
  ELSE
    -- Fallback documentado: solicitudes anteriores a HU-017 sin estado_previo.
    v_estado_nuevo := NVL(v_estado_previo, 'EN_TIEMPO');
  END IF;

  UPDATE denuncia
     SET estado = v_estado_nuevo
   WHERE id_denuncia = v_denuncia_id;

  v_etiqueta_estado := CASE v_estado_nuevo
                         WHEN 'FINALIZADA'    THEN 'Finalizada'
                         WHEN 'EN_TIEMPO'     THEN 'En tiempo'
                         WHEN 'PRONTA_VENCER' THEN UNISTR('Pr\00F3xima a vencer')
                         WHEN 'ATRASADA'      THEN 'Atrasada'
                       END;

  -- Bitacora (accion obligatoria): usuario, fecha y hora los pone
  -- sp_insertar_bitacora. El texto completo de las observaciones queda en
  -- decision_cierre; en la bitacora se recorta al tamano de la columna.
  v_contenido := CASE v_decision
                   WHEN 'APROBADA' THEN
                     UNISTR('Resoluci\00F3n final aprobada por ') || v_quien
                       || '. La denuncia pasa a estado ' || v_etiqueta_estado || '.'
                   ELSE
                     UNISTR('Resoluci\00F3n final rechazada y devuelta para ajustes por ')
                       || v_quien || '. La denuncia vuelve a estado ' || v_etiqueta_estado || '.'
                 END
                 || CASE WHEN v_observaciones IS NOT NULL
                         THEN ' Observaciones: ' || v_observaciones END;

  sp_insertar_bitacora(
    v_denuncia_id, p_jefe_id, v_area_solicitud,
    'solicitud_cierre',
    SUBSTR(v_contenido, 1, 2000)
  );

  -- Confirmacion al funcionario que solicito el cierre.
  v_mensaje := CASE v_decision
                 WHEN 'APROBADA' THEN
                   UPPER(SUBSTR(v_quien, 1, 1)) || SUBSTR(v_quien, 2)
                     || UNISTR(' aprob\00F3 el cierre de la denuncia ') || v_consecutivo
                     || '. La denuncia fue finalizada.'
                 ELSE
                   UPPER(SUBSTR(v_quien, 1, 1)) || SUBSTR(v_quien, 2)
                     || UNISTR(' rechaz\00F3 la solicitud de cierre de la denuncia ') || v_consecutivo
                     || UNISTR(' y la devolvi\00F3 para ajustes. Revise las observaciones en la bit\00E1cora.')
               END;

  sp_insertar_notificacion(
    v_denuncia_id, 'resolucion_cierre', 'funcionario', v_solicitante_id, v_mensaje
  );

  p_estado_final := v_estado_nuevo;

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
    RAISE;
END sp_resolver_solicitud_cierre;
/

COMMIT;
