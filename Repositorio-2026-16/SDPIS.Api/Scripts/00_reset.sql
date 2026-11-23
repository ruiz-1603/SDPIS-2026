-- =====================================================
-- 00_reset.sql
-- Borra TODOS los objetos de tu esquema/usuario local de Oracle
-- para poder recargar todo desde cero, en el orden correcto.
-- Solo usar en tu base LOCAL de desarrollo. No ejecutar contra
-- ninguna base compartida ni de otro compañero.
--
-- HU-015: ademas de tablas y procedimientos, borra funciones,
-- vistas, paquetes, tipos y secuencias sueltas (p. ej. las de
-- versiones anteriores de 21_roles_permisos.sql), para que la
-- recarga quede realmente limpia.
--
-- Orden de carga completo (cada uno con F5 en SQL Developer):
--   00_reset.sql -> 01_esquema.sql -> 02_procedimientos.sql
--   -> 03_seeds.sql -> 04_datos_prueba.sql -> 05_verificaciones.sql
-- =====================================================

BEGIN
  FOR t IN (SELECT table_name FROM user_tables) LOOP
    EXECUTE IMMEDIATE 'DROP TABLE "' || t.table_name || '" CASCADE CONSTRAINTS PURGE';
  END LOOP;
END;
/

-- Procedimientos, funciones, vistas, paquetes, tipos y secuencias
-- que no se borran con las tablas. Las secuencias de columnas
-- IDENTITY (ISEQ$$_...) ya se fueron con su tabla.
BEGIN
  FOR o IN (
    SELECT object_name, object_type
      FROM user_objects
     WHERE object_type IN ('PROCEDURE','FUNCTION','VIEW','PACKAGE','TYPE','SEQUENCE','TRIGGER')
       AND object_name NOT LIKE 'ISEQ$$%'
       AND object_name NOT LIKE 'BIN$%'
     ORDER BY CASE object_type WHEN 'VIEW' THEN 1 WHEN 'PACKAGE' THEN 2 ELSE 3 END
  ) LOOP
    BEGIN
      EXECUTE IMMEDIATE 'DROP ' || o.object_type || ' "' || o.object_name || '"'
        || CASE o.object_type WHEN 'TYPE' THEN ' FORCE' ELSE '' END;
    EXCEPTION
      WHEN OTHERS THEN
        -- Objeto ya eliminado en cascada por otro de la lista.
        IF SQLCODE NOT IN (-4043, -942, -2289) THEN RAISE; END IF;
    END;
  END LOOP;
END;
/

COMMIT;
