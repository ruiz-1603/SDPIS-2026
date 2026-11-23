# SDPIS.Api

# SDPIS - Sistema de Denuncias de Productos de Interés Sanitario

Sistema desarrollado para el **Ministerio de Salud de Costa Rica**, cuyo propósito es centralizar y digitalizar la gestión de denuncias sanitarias relacionadas con productos de interés sanitario (medicamentos, cosméticos, alimentos, entre otros), apoyando los procesos de recepción, clasificación, evaluación de criticidad, asignación, seguimiento, trazabilidad y cierre de los casos.

Proyecto realizado en el marco de la Cátedra de Ingeniería de Sistemas, Escuela de Informática, Universidad Nacional de Costa Rica.

**Proyecto N.º:** 16

## Integrantes

- Jose Castro Oviedo
- Daniel Chacón González
- Sebastián Gómez Solís
- Bryan López Juárez
- Julián Ramos Arias
- Aaron Ruiz Medina

## Stack tecnológico

- **Backend:** C# con ASP.NET Core (servicios RESTful)
- **Frontend:** React (JavaScript, **sin TypeScript**)
- **Base de datos:** Oracle Database 21c (para pruebas a lo largo del desarrollo)
- **Autenticación:** Módulo de Seguridad Integrado (MSI) del Ministerio de Salud
- **Integraciones externas:** Regístrelo, OneDrive
- **Entorno de desarrollo:** Visual Studio Code y Visual Studio Community
- **Control de versiones:** GitHub, siguiendo GitFlow

## Arquitectura

El sistema sigue el estilo arquitectónico **Modelo-Vista-Controlador (MVC)**:

- **Modelo:** entidades del dominio, reglas de negocio y acceso a datos (C# / ASP.NET Core / Oracle).
- **Vista:** interfaces de usuario (React), tanto el portal institucional (autenticado vía MSI) como el formulario público de denuncias (sin autenticación).
- **Controlador:** controladores ASP.NET Core que exponen los servicios REST y gestionan la comunicación con MSI y Regístrelo.

Subsistemas principales:

- Gestión de Denuncias
- Evaluación y Asignación
- Seguimiento y Trazabilidad
- Expediente Documental
- Resolución y Cierre
- Reportes y Dashboard
- Administración y Seguridad

Para el detalle completo de vistas (lógica, de desarrollo, de procesos, física y de escenarios), consultar el documento de Arquitectura del proyecto.

## Estado actual — Sprint 1

En este sprint se está trabajando sobre las siguientes historias de usuario:

| HU | Descripción |
|----|-------------|
| HU-002 | Recibir denuncia |
| HU-004 | Registrar producto(s) denunciado(s) y su(s) motivo(s) |
| HU-005 | Generar identificación y expediente digital |
| HU-007 | Gestionar disponibilidad de funcionarios |
| HU-008 | Dirigir la denuncia a área |
| HU-009 | Asignar denuncia a un funcionario validador |
| HU-010 | Registrar criterios técnicos de análisis |
| HU-011 | Evaluar criticidad de denuncias |
| HU-012 | Asignar denuncia a funcionario o reasignar denuncia a área |
| HU-016 | Registrar seguimiento y bitácora |

## Estado actual — Sprint 2

En este sprint se trabajó sobre las siguientes historias de usuario:

| HU | Descripción |
|----|-------------|
| HU-001 | Iniciar sesión al sistema (integración con MSI) |
| HU-014 | Gestionar plazos de atención de denuncias |
| HU-015 | Gestionar roles y permisos de usuarios |
| HU-017 | Gestionar aprobación de resolución de denuncias |
| HU-018 | Solicitar cierre de denuncia |
| HU-028 | Cerrar sesión del sistema |

## Pruebas
Cada historia de usuario desarrollada hasta el momento se relaciona con pruebas funcionales, pruebas unitarias y las tareas de usabilidad con usuarios, todas
en estado de aprobación.

> Este README se irá actualizando conforme avance el desarrollo del proyecto.

## Documentación relacionada

- Documento de Especificación de Requerimientos de Software (SRS)
- Documento de Arquitectura del Sistema (IS-008-V01)
- Documento de Estándares del Sistema
- Plan Pruebas Usabilidad con Usuarios
- Informe de Resultados de Pruebas de Usabilidad – SDPIS
- Plan de Administración del Proyecto (PAP)
