# Servicio Sistema Web Proxy GDEBA-DVBA

## Contexto

La Direccion Provincial de Vialidad de la Provincia de Buenos Aires requiere integrar distintos sistemas internos con la plataforma GDEBA a traves de los servicios autorizados por IOP.

Los sistemas consumidores previstos incluyen, inicialmente:

- Sistema de Proyectos de Obras.
- Sistema de Licitaciones de Obras.
- Sistema de Seguimiento y Ejecucion de Obras.
- Otros sistemas internos que requieran consultar expedientes, documentos o informacion relacionada con GDEBA.

Actualmente algunos consumos pueden estar implementados dentro de aplicaciones especificas. El objetivo del proyecto es extraer esa responsabilidad hacia un servicio institucional transversal.

## Objetivo General

Implementar el "Servicio Sistema Web Proxy GDEBA-DVBA" como capa institucional de interoperabilidad entre los sistemas internos de DVBA y los servicios web autorizados de GDEBA.

El proxy centralizara:

- Autenticacion y autorizacion tecnica contra GDEBA.
- Consumo de servicios SOAP y REST autorizados.
- Gestion de cache local en SQL Server.
- Auditoria de solicitudes internas.
- Identificacion de la aplicacion interna consumidora.
- Normalizacion de respuestas y errores.
- Control de consumo, frecuencia y disponibilidad.
- Aislamiento de detalles tecnicos de GDEBA respecto de los sistemas internos.

## Alcance

El proxy no reemplaza la logica de negocio de los sistemas consumidores. Su responsabilidad es proveer una capa de soporte institucional para comunicarse con GDEBA.

La logica propia de cada proceso administrativo debe permanecer en su sistema de dominio correspondiente. Por ejemplo, la decision de generar un expediente asociado a un certificado de obra pertenece al backend del sistema de certificacion de obras. El proxy solo deberia proveer la capacidad tecnica para ejecutar la operacion autorizada contra GDEBA y registrar su trazabilidad.

## Beneficios Esperados

- Reducir llamadas directas desde multiples aplicaciones hacia GDEBA.
- Evitar duplicacion de codigo de integracion SOAP/JWT.
- Mejorar seguridad al centralizar credenciales y tokens.
- Mejorar trazabilidad institucional mediante auditoria uniforme.
- Disminuir trafico innecesario mediante cache local.
- Permitir continuidad operativa parcial ante interrupciones de conectividad.
- Facilitar cambios futuros si GDEBA modifica servicios SOAP, habilita REST o agrega filtros incrementales.

## Principios

- El proxy es una capacidad institucional, no un backend de negocio de Obras, Licitaciones o Certificaciones.
- Las aplicaciones consumidoras no deben conocer credenciales GDEBA.
- Las aplicaciones consumidoras no deben depender de contratos SOAP externos.
- Las reglas de cache, auditoria, autorizacion interna y consumo externo pertenecen al dominio del proxy.
- Las reglas funcionales propias de cada sistema consumidor permanecen fuera del proxy.

