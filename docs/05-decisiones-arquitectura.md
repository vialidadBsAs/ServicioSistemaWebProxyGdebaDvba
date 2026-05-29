# Decisiones Arquitectonicas

Este documento registra decisiones iniciales. Debe actualizarse a medida que madure el proyecto.

## ADR-001: Implementar un proxy institucional independiente

Fecha: 2026-05-28

Decision:

Implementar el Servicio Sistema Web Proxy GDEBA-DVBA como servicio independiente consumido por aplicaciones internas.

Contexto:

Varios sistemas requieren acceder a GDEBA. Centralizar la integracion evita duplicar credenciales, SOAP, JWT, cache y auditoria.

Consecuencias:

- Las aplicaciones consumidoras consumen una API interna estable.
- El proxy concentra seguridad tecnica, cache, auditoria y adaptacion a GDEBA.
- Requiere gobernar claramente su alcance para no absorber logica funcional de otros sistemas.

## ADR-002: Mantener fuera del proxy la logica de negocio de sistemas consumidores

Fecha: 2026-05-28

Decision:

El proxy no decidira reglas propias de Obras, Licitaciones, Certificaciones u otros dominios.

Contexto:

El proxy es una capa de soporte transversal. Las decisiones funcionales pertenecen al backend de cada sistema consumidor.

Consecuencias:

- El proxy expone capacidades de integracion GDEBA.
- Los sistemas consumidores orquestan sus casos de uso de negocio.
- Se reduce el riesgo de convertir el proxy en un backend monolitico de multiples dominios.

## ADR-003: Usar arquitectura limpia y orientacion al dominio

Fecha: 2026-05-28

Decision:

Separar el sistema en Domain, Application, Infrastructure y API.

Contexto:

El proxy tiene reglas propias: cache, autorizacion interna, auditoria, consumo externo y normalizacion.

Consecuencias:

- Las reglas centrales quedan desacopladas de EF, SOAP, SQL Server y HTTP.
- Aumenta la mantenibilidad ante cambios de GDEBA.

## ADR-004: Usar URF, Repository y Unit of Work para SQL Server

Fecha: 2026-05-28

Decision:

Utilizar URF con Repository y Unit of Work para persistencia local en SQL Server.

Contexto:

La institucion ya utiliza este enfoque para desacoplar dominio e infraestructura.

Consecuencias:

- Mantiene consistencia con practicas existentes.
- Los repositorios representan la base local/cache, no los servicios externos.

## ADR-005: Usar Gateways/Adapters para GDEBA

Fecha: 2026-05-28

Decision:

Representar el consumo de GDEBA mediante interfaces tipo Gateway o Adapter.

Contexto:

GDEBA es un sistema externo, no la persistencia local. SOAP puede cambiar o migrar a REST.

Consecuencias:

- La capa Application depende de interfaces.
- La implementacion SOAP queda encapsulada.
- Si aparece REST, se reemplaza o agrega otra implementacion.

## ADR-006: Soportar sincronizacion actual y futura sin duplicar el sistema

Fecha: 2026-05-28

Decision:

Prever estrategias de sincronizacion reemplazables: modo actual por consulta general y modo futuro incremental si GDEBA habilita filtros.

Contexto:

Actualmente no hay filtros por reparticion o fecha suficientes. Se solicito a GDEBA evaluar mejoras.

Consecuencias:

- En fase inicial se implementa el modo compatible con servicios actuales.
- La arquitectura queda preparada para una estrategia incremental futura.
- La API interna no deberia cambiar por mejoras externas.

## ADR-007: Identificar aplicacion consumidora internamente

Fecha: 2026-05-28

Decision:

El proxy debe identificar que aplicacion interna realiza cada solicitud para auditoria y control.

Contexto:

Cada aplicacion institucional tiene su propio esquema de seguridad. El proxy necesita trazabilidad, pero no necesariamente reemplazar la autenticacion funcional de cada sistema.

Consecuencias:

- GDEBA vera al proxy como aplicacion consumidora.
- Internamente se registrara la aplicacion solicitante.
- Queda pendiente definir mecanismo tecnico: API key interna, certificado, header firmado, token interno o integracion futura con servicio troncal de seguridad.

