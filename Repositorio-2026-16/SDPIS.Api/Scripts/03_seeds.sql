-- =====================================================
-- 03_seeds.sql
-- Catalogos REALES del sistema (no datos de prueba): provincias,
-- cantones y distritos; tipos de producto y motivos; regiones,
-- areas rectoras y su cobertura; area UVC; criterios tecnicos y
-- niveles de criticidad.
-- Ejecutar despues de 02_procedimientos.sql. Sin limpiezas
-- defensivas: siempre se parte de 00_reset.sql.
-- =====================================================
SET DEFINE OFF


-- #####################################################################
-- Provincias, cantones y distritos
-- Origen: 002 seed_ubicaciones.sql
-- #####################################################################

-- SEED: Ubicación geográfica (provincia, cantón, distrito)
-- Carga única / versionada. No ejecutar en ambientes ya poblados.


-- Provincias
INSERT INTO provincia (nombre) VALUES (UNISTR('San Jos\00E9'));
INSERT INTO provincia (nombre) VALUES ('Alajuela');
INSERT INTO provincia (nombre) VALUES ('Cartago');
INSERT INTO provincia (nombre) VALUES ('Heredia');
INSERT INTO provincia (nombre) VALUES ('Guanacaste');
INSERT INTO provincia (nombre) VALUES ('Puntarenas');
INSERT INTO provincia (nombre) VALUES (UNISTR('Lim\00F3n'));

-- Cantones
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Central', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Escaz\00FA'), 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Desamparados', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Puriscal', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Tarraz\00FA'), 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Aserr\00ED'), 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Mora', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Goicoechea', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Santa Ana', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Alajuelita', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('V\00E1zquez de Coronado'), 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Acosta', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Tib\00E1s'), 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Moravia', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Montes de Oca', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Turrubares', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Dota', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Curridabat', 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('P\00E9rez Zeled\00F3n'), 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Le\00F3n Cort\00E9s Castro'), 'S' FROM provincia WHERE nombre = UNISTR('San Jos\00E9');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Central', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('San Ram\00F3n'), 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Grecia', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'San Mateo', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Atenas', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Naranjo', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Palmares', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Po\00E1s'), 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Orotina', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'San Carlos', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Zarcero', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Sarch\00ED'), 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Upala', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Los Chiles', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Guatuso', 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('R\00EDo Cuarto'), 'S' FROM provincia WHERE nombre = 'Alajuela';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Central', 'S' FROM provincia WHERE nombre = 'Cartago';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Para\00EDso'), 'S' FROM provincia WHERE nombre = 'Cartago';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('La Uni\00F3n'), 'S' FROM provincia WHERE nombre = 'Cartago';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Jim\00E9nez'), 'S' FROM provincia WHERE nombre = 'Cartago';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Turrialba', 'S' FROM provincia WHERE nombre = 'Cartago';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Alvarado', 'S' FROM provincia WHERE nombre = 'Cartago';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Oreamuno', 'S' FROM provincia WHERE nombre = 'Cartago';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'El Guarco', 'S' FROM provincia WHERE nombre = 'Cartago';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Central', 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Barva', 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Santo Domingo', 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Santa B\00E1rbara'), 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'San Rafael', 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'San Isidro', 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Bel\00E9n'), 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Flores', 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'San Pablo', 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Sarapiqu\00ED'), 'S' FROM provincia WHERE nombre = 'Heredia';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Liberia', 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Nicoya', 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Santa Cruz', 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Bagaces', 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Carrillo', 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Ca\00F1as'), 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Abangares', 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Tilar\00E1n'), 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Nandayure', 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'La Cruz', 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Hojancha', 'S' FROM provincia WHERE nombre = 'Guanacaste';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Central', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Esparza', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Buenos Aires', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Montes De Oro', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Osa', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Quepos', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Golfito', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Coto Brus', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Parrita', 'N' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Corredores', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Garabito', 'S' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Monteverde', 'N' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Puerto Jim\00E9nez'), 'N' FROM provincia WHERE nombre = 'Puntarenas';
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Central', 'S' FROM provincia WHERE nombre = UNISTR('Lim\00F3n');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Pococ\00ED'), 'S' FROM provincia WHERE nombre = UNISTR('Lim\00F3n');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Siquirres', 'S' FROM provincia WHERE nombre = UNISTR('Lim\00F3n');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Talamanca', 'S' FROM provincia WHERE nombre = UNISTR('Lim\00F3n');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, 'Matina', 'S' FROM provincia WHERE nombre = UNISTR('Lim\00F3n');
INSERT INTO canton (provincia_id, nombre, tiene_distrito)
  SELECT id_provincia, UNISTR('Gu\00E1cimo'), 'S' FROM provincia WHERE nombre = UNISTR('Lim\00F3n');

-- Distritos
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Carmen' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Merced' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Hospital' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Catedral' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Zapote' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Francisco De Dos Rios' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Uruca' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Mata Redonda' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pavas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Hatillo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Sebasti\00E1n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Escaz\00FA') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Escaz\00FA') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Antonio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Escaz\00FA') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Escaz\00FA') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Desamparados' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Miguel' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan De Dios' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael Arriba' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael Abajo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Antonio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Frailes' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Patarra' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Cristobal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rosario' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Damas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Gravilias' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Los Guido' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Desamparados' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santiago' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Puriscal' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Mercedes Sur' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Puriscal' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Barbacoas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Puriscal' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Grifo Alto' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Puriscal' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Puriscal' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Candelarita' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Puriscal' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Desamparaditos' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Puriscal' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Antonio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Puriscal' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Chires' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Puriscal' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Marcos' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tarraz\00FA') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Lorenzo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tarraz\00FA') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Carlos' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tarraz\00FA') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Aserr\00ED') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Aserr\00ED') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tarbaca' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Aserr\00ED') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Vuelta De Jorco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Aserr\00ED') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Gabriel' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Aserr\00ED') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Legua' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Aserr\00ED') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Monterrey' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Aserr\00ED') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Salitrillos' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Aserr\00ED') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Col\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Mora' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Guayabo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Mora' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tabarcia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Mora' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Piedras Negras' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Mora' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Picagres' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Mora' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Jaris' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Mora' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Guadalupe' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Goicoechea' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Francisco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Goicoechea' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Calle Blancos' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Goicoechea' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Mata De Platano' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Goicoechea' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Ip\00EDs') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Goicoechea' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rancho Redondo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Goicoechea' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Purral' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Goicoechea' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Ana' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Ana' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Salitral' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Ana' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pozos' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Ana' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Uruca' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Ana' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Piedades' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Ana' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Brasil' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Ana' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Alajuelita' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Alajuelita' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Josecito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Alajuelita' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Antonio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Alajuelita' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Concepci\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Alajuelita' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Felipe' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Alajuelita' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('V\00E1zquez de Coronado') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('V\00E1zquez de Coronado') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Dulce Nombre De Jesus' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('V\00E1zquez de Coronado') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Patalillo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('V\00E1zquez de Coronado') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cascajal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('V\00E1zquez de Coronado') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Ignacio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Acosta' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Guaitil' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Acosta' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Palmichal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Acosta' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cangrejal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Acosta' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sabanillas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Acosta' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tib\00E1s') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cinco Esquinas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tib\00E1s') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Anselmo Llorente' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tib\00E1s') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Leon XII' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tib\00E1s') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Colima' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tib\00E1s') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Vicente' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Moravia' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Jeronimo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Moravia' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Trinidad' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Moravia' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pedro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Montes de Oca' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sabanilla' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Montes de Oca' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Mercedes' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Montes de Oca' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Montes de Oca' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pablo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrubares' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pedro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrubares' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan De Mata' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrubares' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Luis' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrubares' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Carara' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrubares' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Santa Mar\00EDa') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Dota' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Jardin' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Dota' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Copey' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Dota' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Curridabat' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Curridabat' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Granadilla' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Curridabat' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sanchez' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Curridabat' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tirrases' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Curridabat' AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro De El General' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'El General' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Daniel Flores' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rivas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pedro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Platanares' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pejibaye' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cajon' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Baru' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rio Nuevo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('P\00E1ramo') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('P\00E9rez Zeled\00F3n') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pablo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Le\00F3n Cort\00E9s Castro') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Andr\00E9s') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Le\00F3n Cort\00E9s Castro') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Llano Bonito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Le\00F3n Cort\00E9s Castro') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Le\00F3n Cort\00E9s Castro') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Cruz' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Le\00F3n Cort\00E9s Castro') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Antonio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Le\00F3n Cort\00E9s Castro') AND p.nombre = UNISTR('San Jos\00E9');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Alajuela' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Jos\00E9') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Carrizal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Antonio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Gu\00E1cima') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sabanilla' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rio Segundo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Desamparados' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Turrucares' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tambor' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Garita' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Sarapiqu\00ED') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Ram\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santiago' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Piedades Norte' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Piedades Sur' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Angeles' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Alfaro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Volio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Concepci\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Zapotal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Pe\00F1as Blancas') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('San Ram\00F3n') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Grecia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Grecia' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Grecia' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Jos\00E9') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Grecia' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Roque' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Grecia' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tacares' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Grecia' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Puente De Piedra' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Grecia' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Bol\00EDvar') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Grecia' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Mateo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Mateo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Desmonte' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Mateo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Jes\00FAs Mar\00EDa') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Mateo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Labrador' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Mateo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Atenas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Atenas' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Jes\00FAs') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Atenas' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Mercedes' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Atenas' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Atenas' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Concepci\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Atenas' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Jos\00E9') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Atenas' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Eulalia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Atenas' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Escobal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Atenas' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Naranjo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Naranjo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Miguel' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Naranjo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Jos\00E9') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Naranjo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Cirr\00ED Sur') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Naranjo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Jer\00F3nimo') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Naranjo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Naranjo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'El Rosario' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Naranjo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Palmitos' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Naranjo' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Palmares' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Palmares' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Zaragoza' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Palmares' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Buenos Aires' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Palmares' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santiago' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Palmares' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Candelaria' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Palmares' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Esquipulas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Palmares' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Granja' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Palmares' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pedro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Po\00E1s') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Po\00E1s') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Po\00E1s') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Carrillos' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Po\00E1s') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sabana Redonda' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Po\00E1s') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Orotina' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Orotina' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'El Mastate' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Orotina' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Hacienda Vieja' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Orotina' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Coyolar' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Orotina' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Ceiba' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Orotina' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Quesada' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Florencia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Buenavista' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Aguas Zarcas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Venecia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pital' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Fortuna' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Tigra' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Palmera' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Venado' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cutris' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Monterrey' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pocosol' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Carlos' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Zarcero' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Zarcero' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Laguna' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Zarcero' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tapesco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Zarcero' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Guadalupe' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Zarcero' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Palmira' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Zarcero' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Zapote' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Zarcero' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Brisas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Zarcero' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Sarch\00ED Norte') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarch\00ED') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Sarch\00ED Sur') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarch\00ED') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Toro Amarillo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarch\00ED') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pedro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarch\00ED') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rodriguez' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarch\00ED') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Upala' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Upala' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Aguas Claras' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Upala' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Jos\00E9 o Pizote') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Upala' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Bijagua' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Upala' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Delicias' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Upala' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Dos Rios' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Upala' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Yolillal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Upala' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Canalete' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Upala' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Los Chiles' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Los Chiles' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Ca\00F1o Negro') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Los Chiles' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'El Amparo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Los Chiles' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Jorge' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Los Chiles' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Guatuso' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Buenavista' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Guatuso' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cote' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Guatuso' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Katira' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Guatuso' AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('R\00EDo Cuarto') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('R\00EDo Cuarto') AND p.nombre = 'Alajuela';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Oriental' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Occidental' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Carmen' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Nicol\00E1s') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Aguacaliente o San Francisco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Guadalupe o Arenilla' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Corralillo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tierra Blanca' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Dulce Nombre' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Llano Grande' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Quebradilla' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Paraiso' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Para\00EDso') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santiago' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Para\00EDso') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Orosi' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Para\00EDso') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Cach\00ED') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Para\00EDso') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Llanos de Santa Luc\00EDa') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Para\00EDso') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tres Rios' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('La Uni\00F3n') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Diego' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('La Uni\00F3n') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('La Uni\00F3n') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('La Uni\00F3n') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Concepci\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('La Uni\00F3n') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Dulce Nombre' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('La Uni\00F3n') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Ram\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('La Uni\00F3n') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rio Azul' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('La Uni\00F3n') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Juan Vi\00F1as') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Jim\00E9nez') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tucurrique' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Jim\00E9nez') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pejibaye' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Jim\00E9nez') AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Turrialba' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Suiza' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Peralta' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Cruz' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Teresita' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pavones' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tuis' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tayutic' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Rosa' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tres Equis' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Isabel' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Chirrip\00F3') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Turrialba' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pacayas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Alvarado' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cervantes' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Alvarado' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Capellades' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Alvarado' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Oreamuno' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cot' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Oreamuno' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Potrero Cerrado' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Oreamuno' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cipreses' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Oreamuno' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Rosa' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Oreamuno' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'El Tejar' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'El Guarco' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'El Guarco' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tobosi' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'El Guarco' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Patio De Agua' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'El Guarco' AND p.nombre = 'Cartago';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Heredia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Mercedes' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Francisco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Ulloa' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Varablanca' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Barva' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Barva' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pedro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Barva' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pablo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Barva' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Roque' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Barva' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Santa Luc\00EDa') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Barva' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Jos\00E9 de la Monta\00F1a') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Barva' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santo Domingo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santo Domingo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Vicente' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santo Domingo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Miguel' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santo Domingo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Paracito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santo Domingo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Santo Tom\00E1s') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santo Domingo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Rosa' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santo Domingo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tures' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santo Domingo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Para' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santo Domingo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Santa B\00E1rbara') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Santa B\00E1rbara') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pedro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Santa B\00E1rbara') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Santa B\00E1rbara') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Jes\00FAs') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Santa B\00E1rbara') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santo Domingo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Santa B\00E1rbara') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Puraba' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Santa B\00E1rbara') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Rafael' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Josecito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Rafael' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santiago' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Rafael' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Los \00C1ngeles') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Rafael' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Concepci\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Rafael' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Isidro' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Jos\00E9') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Isidro' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Concepci\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Isidro' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Francisco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Isidro' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Antonio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Bel\00E9n') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Ribera' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Bel\00E9n') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Asuncion' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Bel\00E9n') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Joaqu\00EDn') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Flores' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Barrantes' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Flores' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Llorente' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Flores' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pablo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Pablo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rincon De Sabanilla' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'San Pablo' AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Puerto Viejo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarapiqu\00ED') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Virgen' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarapiqu\00ED') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Las Horquetas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarapiqu\00ED') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Llanuras Del Gaspar' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarapiqu\00ED') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Cure\00F1a') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Sarapiqu\00ED') AND p.nombre = 'Heredia';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Liberia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Liberia' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Ca\00F1as Dulces') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Liberia' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Mayorga' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Liberia' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Nacascolo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Liberia' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Curubande' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Liberia' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Nicoya' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nicoya' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Mansi\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nicoya' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Antonio' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nicoya' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Quebrada Honda' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nicoya' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('S\00E1mara') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nicoya' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Nosara' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nicoya' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Bel\00E9n De Nosarita') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nicoya' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Cruz' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Bolson' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Veintisiete de Abril' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tempate' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cartagena' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cuajiniquil' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Diria' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cabo Velas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tamarindo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Santa Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Bagaces' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Bagaces' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Fortuna' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Bagaces' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Mogote' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Bagaces' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rio Naranjo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Bagaces' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Filadelfia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Carrillo' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Palmira' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Carrillo' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sardinal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Carrillo' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Belen' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Carrillo' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Ca\00F1as') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Ca\00F1as') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Palmira' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Ca\00F1as') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Miguel' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Ca\00F1as') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Bebedero' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Ca\00F1as') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Porozal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Ca\00F1as') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Las Juntas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Abangares' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sierra' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Abangares' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Abangares' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Colorado' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Abangares' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Tilar\00E1n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tilar\00E1n') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Quebrada Grande' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tilar\00E1n') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tronadora' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tilar\00E1n') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Rosa' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tilar\00E1n') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('L\00EDbano') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tilar\00E1n') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Tierras Morenas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tilar\00E1n') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Arenal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Tilar\00E1n') AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Carmona' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nandayure' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Rita' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nandayure' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Zapotal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nandayure' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Pablo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nandayure' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Porvenir' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nandayure' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Bejuco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Nandayure' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Cruz' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'La Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Cecilia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'La Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Garita' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'La Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Santa Elena' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'La Cruz' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Hojancha' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Hojancha' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Monte Romo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Hojancha' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Puerto Carrillo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Hojancha' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Huacas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Hojancha' AND p.nombre = 'Guanacaste';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Puntarenas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pitahaya' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Chomes' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Lepanto' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Paquera' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Manzanillo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Guacimal' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Barranca' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Monte Verde' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Isla Del Coco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('C\00F3bano') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Chacarita' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Chira' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Acapulco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'El Roble' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Arancibia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Esp\00EDritu Santo') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Esparza' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Juan Grande' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Esparza' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Macacona' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Esparza' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Rafael' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Esparza' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('San Jer\00F3nimo') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Esparza' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Buenos Aires' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Buenos Aires' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Volc\00E1n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Buenos Aires' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Potrero Grande' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Buenos Aires' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Boruca' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Buenos Aires' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pilas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Buenos Aires' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Colinas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Buenos Aires' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Changuena' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Buenos Aires' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Biolley' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Buenos Aires' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Brunka' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Buenos Aires' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Miramar' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Montes De Oro' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('La Uni\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Montes De Oro' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Isidro' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Montes De Oro' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Puerto Cort\00E9s') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Osa' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Palmar' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Osa' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sierpe' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Osa' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Bah\00EDa Ballena') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Osa' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Piedras Blancas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Osa' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Bah\00EDa Drake') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Osa' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Quepos' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Quepos' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Savegre' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Quepos' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Naranjito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Quepos' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Golfito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Golfito' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Puerto Jim\00E9nez') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Golfito' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Guaycara' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Golfito' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Pav\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Golfito' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'San Vito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Coto Brus' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sabalito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Coto Brus' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Aguabuena' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Coto Brus' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Limoncito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Coto Brus' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pittier' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Coto Brus' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Corredor' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Corredores' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Cuesta' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Corredores' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Canoas' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Corredores' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Laurel' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Corredores' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Jac\00F3') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Garabito' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('T\00E1rcoles') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Garabito' AND p.nombre = 'Puntarenas';
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Lim\00F3n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Valle La Estrella' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rio Blanco' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Matama' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Central' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Guapiles' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Pococ\00ED') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Jim\00E9nez') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Pococ\00ED') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Rita' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Pococ\00ED') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Roxana' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Pococ\00ED') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cariari' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Pococ\00ED') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Colorado' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Pococ\00ED') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'La Colonia' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Pococ\00ED') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Siquirres' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Siquirres' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pacuarito' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Siquirres' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Florida' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Siquirres' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Germania' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Siquirres' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'El Cairo' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Siquirres' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Alegr\00EDa') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Siquirres' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Bratsi' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Talamanca' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Sixaola' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Talamanca' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Cahuita' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Talamanca' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Telire' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Talamanca' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Matina' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Matina' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Bat\00E1n') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Matina' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Carrandi' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = 'Matina' AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Gu\00E1cimo') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Gu\00E1cimo') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Mercedes' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Gu\00E1cimo') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Pocora' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Gu\00E1cimo') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, UNISTR('Rio Jim\00E9nez') FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Gu\00E1cimo') AND p.nombre = UNISTR('Lim\00F3n');
INSERT INTO distrito (canton_id, nombre)
  SELECT c.id_canton, 'Duacari' FROM canton c JOIN provincia p ON c.provincia_id = p.id_provincia
  WHERE c.nombre = UNISTR('Gu\00E1cimo') AND p.nombre = UNISTR('Lim\00F3n');

COMMIT;


-- #####################################################################
-- Tipos de producto y motivos de denuncia
-- Origen: 03_catalogo_minimo.sql (sin la limpieza defensiva)
-- #####################################################################
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (1, 'Alimentos');
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (2, UNISTR('Cannabis/C\00E1\00F1amo'));
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (3, UNISTR('Cosm\00E9ticos'));
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (4, UNISTR('Equipos y materiales biom\00E9dicos (EMB)'));
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (5, 'Medicamentos');
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (6, 'Plaguicidas');
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (7, UNISTR('Productos Higi\00E9nicos'));
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (8, 'Productos Naturales');
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (9, UNISTR('Productos Qu\00EDmicos'));
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (10, 'Suplementos');
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (11, 'Tintas para Tatuaje');
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (12, UNISTR('L\00EDquidos para vapeo'));
INSERT INTO tipo_producto (id_tipo_producto, nombre) VALUES (13, 'Materias Primas');


INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (1, UNISTR('Adulteraci\00F3n'));
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (2, 'Calidad');
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (3, 'Etiquetado');
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (4, UNISTR('Falsificaci\00F3n'));
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (5, 'Indicaciones no Aprobadas');
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (6, UNISTR('Intoxicaci\00F3n'));
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (7, 'Irregularidades en el registro');
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (8, 'Producto vencido');
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (9, 'Publicidad');
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (10, 'Seguridad');
INSERT INTO motivo_denuncia (id_motivo, nombre) VALUES (11, 'Sin Registro Sanitario');

-- Empuja el contador del IDENTITY para que, si en el futuro alguien
-- inserta un tipo_producto/motivo nuevo SIN especificar id, no choque
ALTER TABLE tipo_producto MODIFY id_tipo_producto
  GENERATED BY DEFAULT AS IDENTITY (START WITH 100);
ALTER TABLE motivo_denuncia MODIFY id_motivo
  GENERATED BY DEFAULT AS IDENTITY (START WITH 100);

COMMIT;


-- #####################################################################
-- Regiones, areas rectoras y cobertura (desde Data/area.csv y Data/ubicacion_area.csv)
-- Origen: 05_seed_ars_y_ubicaciones.sql (sin la limpieza defensiva)
-- #####################################################################
-- REGIONES
INSERT INTO region (id_region, nombre) VALUES (100, 'Central Sur');
INSERT INTO region (id_region, nombre) VALUES (101, 'Central Norte');
INSERT INTO region (id_region, nombre) VALUES (102, 'Central Este');
INSERT INTO region (id_region, nombre) VALUES (103, 'Central Occidente');
INSERT INTO region (id_region, nombre) VALUES (104, 'Huetar Caribe');
INSERT INTO region (id_region, nombre) VALUES (105, 'Brunca');
INSERT INTO region (id_region, nombre) VALUES (106, 'Chorotega');
INSERT INTO region (id_region, nombre) VALUES (107, 'Huetar Norte');
INSERT INTO region (id_region, nombre) VALUES (108, UNISTR('Pac\00EDfico Central'));
COMMIT;

-- AREAS RECTORAS
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (100, 'A.R.S Acosta', 'ARS', 'ACOS', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (101, 'A.R.S Alajuelita', 'ARS', 'ALTA', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (102, UNISTR('A.R.S Aserr\00ED'), 'ARS', 'ASER', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (103, 'A.R.S Carmen-Merced-Uruca', 'ARS', 'CMU', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (104, 'A.R.S Coronado', 'ARS', 'CORO', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (105, 'A.R.S Curridabat', 'ARS', 'CURR', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (106, 'A.R.S Desamparados', 'ARS', 'DESA', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (107, UNISTR('A.R.S Escaz\00FA'), 'ARS', 'ESCA', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (108, 'A.R.S Goicoechea', 'ARS', 'GOIC', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (109, 'A.R.S Hatillo', 'ARS', 'HATI', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (110, 'A.R.S Hospital Mata Redonda', 'ARS', 'HMR', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (111, 'A.R.S Montes de Oca', 'ARS', 'MOCA', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (112, 'A.R.S Mora', 'ARS', 'MORA', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (113, 'A.R.S Moravia', 'ARS', 'MORV', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (114, 'A.R.S Pavas', 'ARS', 'PAVA', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (115, 'A.R.S Puriscal Turrubares', 'ARS', 'PURI', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (116, 'A.R.S Santa Ana', 'ARS', 'SANA', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (117, 'A.R.S Sureste Metropolitana', 'ARS', 'SURE', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (118, UNISTR('A.R.S Tib\00E1s'), 'ARS', 'TIBA', 100);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (119, 'A.R.S Alajuela 1', 'ARS', 'ALA1', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (120, 'A.R.S Alajuela 2', 'ARS', 'ALA2', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (121, 'A.R.S Atenas', 'ARS', 'ATEN', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (122, 'A.R.S Barva - San Rafael', 'ARS', 'BARV', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (123, UNISTR('A.R.S Bel\00E9n - Flores'), 'ARS', 'BELF', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (124, 'A.R.S Grecia', 'ARS', 'GREC', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (125, 'A.R.S Heredia', 'ARS', 'HERE', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (126, UNISTR('A.R.S Po\00E1s'), 'ARS', 'POAS', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (127, 'A.R.S San Pablo - San Isidro', 'ARS', 'SPSI', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (128, UNISTR('A.R.S Santa B\00E1rbara'), 'ARS', 'SBAR', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (129, 'A.R.S Santo Domingo', 'ARS', 'SDOM', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (130, UNISTR('A.R.S Sarapiqu\00ED'), 'ARS', 'SARA', 101);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (131, 'A.R.S Cartago', 'ARS', 'CART', 102);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (132, UNISTR('A.R.S La Uni\00F3n'), 'ARS', 'LUNION', 102);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (133, 'A.R.S Los Santos', 'ARS', 'LSAN', 102);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (134, 'A.R.S Oreamuno', 'ARS', 'OREA', 102);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (135, UNISTR('A.R.S Para\00EDso'), 'ARS', 'PARA', 102);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (136, 'A.R.S Tejar del Guarco', 'ARS', 'TEJAR', 102);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (137, 'A.R.S Turrialba', 'ARS', 'TURR', 102);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (138, 'A.R.S Naranjo', 'ARS', 'NARA', 103);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (139, 'A.R.S Palmares', 'ARS', 'PALM', 103);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (140, UNISTR('A.R.S San Ram\00F3n'), 'ARS', 'SRAM', 103);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (141, 'A.R.S Valverde Vega', 'ARS', 'VVEG', 103);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (142, 'A.R.S Zarcero', 'ARS', 'ZARC', 103);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (143, UNISTR('A.R.S Gu\00E1cimo'), 'ARS', 'GUAC', 104);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (144, UNISTR('A.R.S Lim\00F3n'), 'ARS', 'LIMO', 104);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (145, 'A.R.S Matina', 'ARS', 'MATI', 104);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (146, UNISTR('A.R.S Pococ\00ED'), 'ARS', 'POCO', 104);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (147, 'A.R.S Siquirres', 'ARS', 'SIQU', 104);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (148, 'A.R.S Talamanca', 'ARS', 'TALA', 104);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (149, 'A.R.S Buenos Aires', 'ARS', 'BAIR', 105);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (150, 'A.R.S Corredores', 'ARS', 'CORR', 105);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (151, 'A.R.S Coto Brus', 'ARS', 'CBRU', 105);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (152, 'A.R.S Golfito', 'ARS', 'GOLF', 105);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (153, 'A.R.S Osa', 'ARS', 'OSA', 105);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (154, UNISTR('A.R.S P\00E9rez Zeled\00F3n'), 'ARS', 'PZEL', 105);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (155, 'A.R.S Abangares', 'ARS', 'ABAN', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (156, 'A.R.S Bagaces', 'ARS', 'BAGA', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (157, UNISTR('A.R.S Ca\00F1as'), 'ARS', 'CANA', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (158, 'A.R.S Carrillo', 'ARS', 'CARR', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (159, 'A.R.S Hojancha', 'ARS', 'HOJA', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (160, 'A.R.S La Cruz', 'ARS', 'LCRU', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (161, 'A.R.S Liberia', 'ARS', 'LIBE', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (162, 'A.R.S Nandayure', 'ARS', 'NAND', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (163, 'A.R.S Nicoya', 'ARS', 'NICO', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (164, 'A.R.S Santa Cruz', 'ARS', 'SCRU', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (165, UNISTR('A.R.S Tilar\00E1n'), 'ARS', 'TILA', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (166, 'A.R.S Upala', 'ARS', 'UPAL', 106);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (167, 'A.R.S Aguas Zarcas', 'ARS', 'AZAR', 107);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (168, 'A.R.S Ciudad Quesada', 'ARS', 'CQUE', 107);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (169, 'A.R.S Florencia', 'ARS', 'FLOR', 107);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (170, 'A.R.S Guatuso', 'ARS', 'GUAT', 107);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (171, 'A.R.S Los Chiles', 'ARS', 'LCHI', 107);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (172, 'A.R.S Santa Rosa Pocosol', 'ARS', 'SRPO', 107);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (173, 'A.R.S Barranca', 'ARS', 'BARR', 108);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (174, 'A.R.S Esparza', 'ARS', 'ESPA', 108);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (175, 'A.R.S Garabito', 'ARS', 'GARA', 108);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (176, 'A.R.S Montes de Oro', 'ARS', 'MORO', 108);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (177, 'A.R.S Orotina San Mateo', 'ARS', 'OROT', 108);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (178, 'A.R.S Parrita', 'ARS', 'PARR', 108);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (179, 'A.R.S Peninsular', 'ARS', 'PENI', 108);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (180, 'A.R.S Puntarenas - Chacarita', 'ARS', 'PUNT', 108);
INSERT INTO area (id_area, nombre, tipo, codigo_area, region_id) VALUES (181, 'A.R.S Quepos', 'ARS', 'QUEP', 108);
COMMIT;

-- RELACION UBICACION -> AREA
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (61, 367, 155);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (61, 368, 155);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (61, 369, 155);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (61, 370, 155);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (6, 42, 100);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (12, 76, 100);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (12, 77, 100);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (12, 79, 100);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (12, 80, 100);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 196, 167);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 197, 167);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 198, 167);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 122, 119);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 124, 119);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 127, 119);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 128, 119);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 130, 119);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 131, 119);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 123, 120);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 125, 120);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 126, 120);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 129, 120);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 132, 120);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 133, 120);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 134, 120);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (21, 135, 120);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (10, 66, 101);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (10, 67, 101);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (10, 68, 101);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (10, 69, 101);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (10, 70, 101);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 17, 102);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (6, 40, 102);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (6, 41, 102);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (6, 43, 102);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (6, 44, 102);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (6, 45, 102);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (6, 46, 102);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (25, 160, 121);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (25, 161, 121);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (25, 162, 121);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (25, 163, 121);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (25, 164, 121);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (25, 165, 121);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (25, 166, 121);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (25, 167, 121);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (58, 354, 156);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (58, 355, 156);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (58, 356, 156);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (58, 357, 156);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 399, 173);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 406, 173);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (46, 291, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (46, 292, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (46, 293, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (46, 294, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (46, 295, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (46, 296, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (49, 311, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (49, 312, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (49, 313, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (49, 314, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (49, 315, 122);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (51, 320, 123);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (51, 321, 123);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (51, 322, 123);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (52, 323, 123);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (52, 324, 123);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (52, 325, 123);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (68, 413, 149);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (68, 414, 149);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (68, 415, 149);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (68, 416, 149);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (68, 417, 149);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (68, 418, 149);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (68, 419, 149);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (68, 420, 149);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (68, 421, 149);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (60, 362, 157);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (60, 363, 157);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (60, 364, 157);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (60, 365, 157);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (60, 366, 157);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 1, 103);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 2, 103);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 7, 103);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (59, 358, 158);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (59, 359, 158);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (59, 360, 158);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (59, 361, 158);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 235, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 236, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 237, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 238, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 239, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 240, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 241, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 242, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 243, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 244, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (37, 245, 131);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 193, 168);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 195, 168);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 201, 168);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (8, 57, 104);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (11, 71, 104);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (11, 72, 104);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (11, 73, 104);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (11, 74, 104);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (11, 75, 104);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (75, 443, 150);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (75, 444, 150);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (75, 445, 150);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (75, 446, 150);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (73, 438, 151);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (73, 439, 151);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (73, 440, 151);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (73, 441, 151);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (73, 442, 151);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (18, 101, 105);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (18, 102, 105);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (18, 103, 105);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (18, 104, 105);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 15, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 16, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 18, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 19, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 20, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 21, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 22, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 23, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 24, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 25, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 26, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (3, 27, 106);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (2, 12, 107);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (2, 13, 107);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (2, 14, 107);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (67, 408, 174);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (67, 409, 174);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (67, 410, 174);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (67, 411, 174);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (67, 412, 174);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 194, 169);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 199, 169);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 200, 169);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 202, 169);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 204, 169);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (76, 447, 175);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (76, 448, 175);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (8, 53, 108);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (8, 54, 108);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (8, 55, 108);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (8, 56, 108);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (8, 58, 108);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (8, 59, 108);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (72, 434, 152);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (72, 435, 152);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (72, 436, 152);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (72, 437, 152);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (78, NULL, 152);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (23, 149, 124);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (23, 150, 124);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (23, 151, 124);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (23, 152, 124);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (23, 153, 124);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (23, 154, 124);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (23, 155, 124);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (36, 234, 124);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (84, 473, 143);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (84, 474, 143);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (84, 475, 143);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (84, 476, 143);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (84, 477, 143);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (35, 230, 170);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (35, 231, 170);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (35, 232, 170);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (35, 233, 170);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 10, 109);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (45, 286, 125);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (45, 287, 125);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (45, 288, 125);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (45, 289, 125);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (45, 290, 125);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (65, 388, 159);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (65, 389, 159);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (65, 390, 159);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (65, 391, 159);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 3, 110);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 8, 110);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (64, 384, 160);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (64, 385, 160);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (64, 386, 160);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (64, 387, 160);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (39, 251, 132);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (39, 252, 132);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (39, 253, 132);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (39, 254, 132);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (39, 255, 132);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (39, 256, 132);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (39, 257, 132);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (39, 258, 132);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (55, 333, 161);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (55, 334, 161);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (55, 335, 161);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (55, 336, 161);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (55, 337, 161);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (79, 449, 144);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (79, 450, 144);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (79, 451, 144);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (79, 452, 144);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (34, 226, 171);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (34, 227, 171);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (34, 228, 171);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (34, 229, 171);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (5, 37, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (5, 38, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (5, 39, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (17, 98, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (17, 99, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (17, 100, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (20, 116, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (20, 117, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (20, 118, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (20, 119, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (20, 120, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (20, 121, 133);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (83, 470, 145);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (83, 471, 145);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (83, 472, 145);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (15, 89, 111);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (15, 90, 111);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (15, 91, 111);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (15, 92, 111);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 400, 176);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (69, 422, 176);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (69, 423, 176);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (69, 424, 176);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (77, NULL, 176);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (7, 47, 112);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (7, 48, 112);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (7, 49, 112);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (7, 50, 112);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (7, 51, 112);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (7, 52, 112);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (12, 78, 112);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (14, 86, 113);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (14, 87, 113);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (14, 88, 113);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (63, 378, 162);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (63, 379, 162);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (63, 380, 162);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (63, 381, 162);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (63, 382, 162);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (63, 383, 162);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (26, 168, 138);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (26, 169, 138);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (26, 170, 138);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (26, 171, 138);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (26, 172, 138);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (26, 173, 138);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (26, 174, 138);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (26, 175, 138);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (56, 338, 163);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (56, 339, 163);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (56, 340, 163);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (56, 341, 163);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (56, 342, 163);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (56, 343, 163);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (56, 344, 163);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (42, 274, 134);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (42, 276, 134);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (43, 277, 134);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (43, 278, 134);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (43, 279, 134);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (43, 280, 134);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (43, 281, 134);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (24, 156, 177);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (24, 157, 177);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (24, 158, 177);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (24, 159, 177);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (29, 188, 177);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (29, 189, 177);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (29, 190, 177);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (29, 191, 177);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (29, 192, 177);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (70, 425, 153);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (70, 426, 153);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (70, 427, 153);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (70, 428, 153);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (70, 429, 153);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (70, 430, 153);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (27, 176, 139);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (27, 177, 139);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (27, 178, 139);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (27, 179, 139);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (27, 180, 139);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (27, 181, 139);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (27, 182, 139);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (38, 246, 135);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (38, 247, 135);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (38, 248, 135);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (38, 249, 135);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (38, 250, 135);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (42, 275, 135);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (74, NULL, 178);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 9, 114);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 395, 179);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 396, 179);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 397, 179);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 402, 179);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 404, 179);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 105, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 106, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 107, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 108, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 109, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 110, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 111, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 112, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 113, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 114, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (19, 115, 154);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (28, 183, 126);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (28, 184, 126);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (28, 185, 126);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (28, 186, 126);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (28, 187, 126);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (80, 453, 146);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (80, 454, 146);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (80, 455, 146);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (80, 456, 146);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (80, 457, 146);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (80, 458, 146);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (80, 459, 146);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 392, 180);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 393, 180);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 394, 180);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 398, 180);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 401, 180);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 403, 180);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 405, 180);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (66, 407, 180);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (4, 28, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (4, 29, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (4, 30, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (4, 31, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (4, 32, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (4, 33, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (4, 34, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (4, 35, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (4, 36, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (16, 93, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (16, 94, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (16, 95, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (16, 96, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (16, 97, 115);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (71, 431, 181);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (71, 432, 181);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (71, 433, 181);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (50, 316, 127);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (50, 317, 127);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (50, 318, 127);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (50, 319, 127);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (53, 326, 127);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (53, 327, 127);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 136, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 137, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 138, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 139, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 140, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 141, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 142, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 143, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 144, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 145, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 146, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 147, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (22, 148, 140);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (9, 60, 116);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (9, 61, 116);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (9, 62, 116);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (9, 63, 116);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (9, 64, 116);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (9, 65, 116);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (48, 305, 128);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (48, 306, 128);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (48, 307, 128);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (48, 308, 128);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (48, 309, 128);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (48, 310, 128);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (57, 345, 164);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (57, 346, 164);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (57, 347, 164);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (57, 348, 164);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (57, 349, 164);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (57, 350, 164);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (57, 351, 164);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (57, 352, 164);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (57, 353, 164);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 203, 172);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (30, 205, 172);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (47, 297, 129);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (47, 298, 129);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (47, 299, 129);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (47, 300, 129);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (47, 301, 129);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (47, 302, 129);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (47, 303, 129);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (47, 304, 129);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (54, 328, 130);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (54, 329, 130);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (54, 330, 130);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (54, 331, 130);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (54, 332, 130);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (81, 460, 147);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (81, 461, 147);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (81, 462, 147);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (81, 463, 147);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (81, 464, 147);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (81, 465, 147);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 4, 117);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 5, 117);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 6, 117);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (1, 11, 117);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (82, 466, 148);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (82, 467, 148);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (82, 468, 148);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (82, 469, 148);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (44, 282, 136);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (44, 283, 136);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (44, 284, 136);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (44, 285, 136);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (13, 81, 118);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (13, 82, 118);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (13, 83, 118);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (13, 84, 118);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (13, 85, 118);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (62, 371, 165);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (62, 372, 165);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (62, 373, 165);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (62, 374, 165);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (62, 375, 165);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (62, 376, 165);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (62, 377, 165);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (40, 259, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (40, 260, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (40, 261, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 262, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 263, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 264, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 265, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 266, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 267, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 268, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 269, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 270, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 271, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 272, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (41, 273, 137);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (33, 218, 166);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (33, 219, 166);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (33, 220, 166);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (33, 221, 166);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (33, 222, 166);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (33, 223, 166);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (33, 224, 166);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (33, 225, 166);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (32, 213, 141);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (32, 214, 141);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (32, 215, 141);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (32, 216, 141);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (32, 217, 141);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (31, 206, 142);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (31, 207, 142);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (31, 208, 142);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (31, 209, 142);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (31, 210, 142);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (31, 211, 142);
INSERT INTO ubicacion_area (canton_id, distrito_id, area_id) VALUES (31, 212, 142);
COMMIT;


-- Actualizar secuencias IDENTITY para evitar colisiones si se inserta manual despues
ALTER TABLE region MODIFY id_region GENERATED BY DEFAULT AS IDENTITY (START WITH 109);
ALTER TABLE area MODIFY id_area GENERATED BY DEFAULT AS IDENTITY (START WITH 182);


-- #####################################################################
-- Criterios tecnicos y sus valores
-- Origen: 09_catalogo_criterios_tecnicos.sql (sin la limpieza defensiva)
-- #####################################################################
INSERT INTO criterio (id_criterio, nombre, tipo_respuesta, afecta_suma) VALUES (1, 'Dano potencial a la salud', 'seleccion_unica', 'S');
INSERT INTO criterio (id_criterio, nombre, tipo_respuesta, afecta_suma) VALUES (2, 'Criterio tecnico especializado UVC', 'seleccion_unica', 'S');

INSERT INTO criterio_valor (id_criterio, valor, puntaje) VALUES (1, 'Bajo', 1);
INSERT INTO criterio_valor (id_criterio, valor, puntaje) VALUES (1, 'Medio', 3);
INSERT INTO criterio_valor (id_criterio, valor, puntaje) VALUES (1, 'Alto', 5);

INSERT INTO criterio_valor (id_criterio, valor, puntaje) VALUES (2, 'No', 0);
INSERT INTO criterio_valor (id_criterio, valor, puntaje) VALUES (2, 'Si', 1);

-- Empuja el contador del IDENTITY para que, si en el futuro alguien
-- inserta un criterio nuevo sin especificar id, no choque.
ALTER TABLE criterio MODIFY id_criterio
  GENERATED BY DEFAULT AS IDENTITY (START WITH 100);

COMMIT;


-- #####################################################################
-- Niveles de criticidad y plazos
-- Origen: 11_seed_nivel_criticidad.sql
-- #####################################################################
-- =====================================================
-- 11_seed_nivel_criticidad.sql
-- Catalogo de rangos de criticidad (HU-011 "Evaluar criticidad de
-- denuncias" #62). Umbrales confirmados en el issue #62:
--   puntaje = suma de "Dano potencial a la salud" + "Criterio
--   tecnico especializado UVC" (maximo 6) -- >=5 ALTO, >=3 MEDIO,
--   resto BAJO.
--
-- Se siembra esta tabla en vez de hardcodear los umbrales en C#
-- porque el esquema ya la definio para este proposito (columnas
-- puntaje_min/puntaje_max). No es una pantalla ni un endpoint
-- editable por el superusuario -- eso lo implementa HU-013
-- "Gestionar reglas de criticidad" (#64), fuera de alcance este
-- sprint (ver AC #9 de HU-011).
--
-- plazo_dias alimenta evaluacion_criticidad.fecha_limite (AC #10:
-- "insumo para la definicion automatica de plazos de atencion").
-- Plazos oficiales de atencion por nivel de criticidad:
--   ALTO=60, MEDIO=90, BAJO=120 dias.
-- (Hasta 2026-09-23 se usaron valores provisionales ALTO=5,
-- MEDIO=15, BAJO=30 acordados el 2026-09-16.)
--
-- Ejecutar DESPUES de 09_catalogo_criterios_tecnicos.sql (la tabla
-- nivel_criticidad ya existe desde SDPIS.sql, este script solo la
-- siembra).
--
-- Reejecutable: usa MERGE en vez de DELETE + INSERT porque
-- evaluacion_criticidad referencia nivel_criticidad(nivel)
-- (FK_EVAL_NIVEL); con denuncias ya evaluadas el DELETE falla con
-- ORA-02292. MERGE actualiza los niveles existentes y crea los que
-- falten.
-- =====================================================

MERGE INTO nivel_criticidad n
USING (
  SELECT 'BAJO'  AS nivel, 0 AS puntaje_min, 2 AS puntaje_max, 120 AS plazo_dias FROM dual UNION ALL
  SELECT 'MEDIO' AS nivel, 3 AS puntaje_min, 4 AS puntaje_max, 90  AS plazo_dias FROM dual UNION ALL
  SELECT 'ALTO'  AS nivel, 5 AS puntaje_min, 6 AS puntaje_max, 60  AS plazo_dias FROM dual
) s
ON (n.nivel = s.nivel)
WHEN MATCHED THEN
  UPDATE SET n.puntaje_min = s.puntaje_min,
             n.puntaje_max = s.puntaje_max,
             n.plazo_dias  = s.plazo_dias
WHEN NOT MATCHED THEN
  INSERT (nivel, puntaje_min, puntaje_max, plazo_dias)
  VALUES (s.nivel, s.puntaje_min, s.puntaje_max, s.plazo_dias);

COMMIT;


-- #####################################################################
-- Area UVC (Unidad de Vigilancia y Control)
-- Origen: 13_encargado_reasignacion_area.sql (seed del area)
-- #####################################################################
-- La UVC es una unidad nacional: no pertenece a ninguna region (region_id NULL).
INSERT INTO area (nombre, tipo, codigo_area, region_id)
VALUES ('Unidad de Vigilancia y Control (UVC)', 'UVC', 'UVC', NULL);
COMMIT;
