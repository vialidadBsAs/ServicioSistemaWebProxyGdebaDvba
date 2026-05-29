# Arquitectura Propuesta

## Enfoque

El Servicio Sistema Web Proxy GDEBA-DVBA se implementara con arquitectura limpia y orientacion al dominio.

Aunque el sistema consume servicios externos, posee reglas propias: autorizacion de aplicaciones internas, politicas de cache, trazabilidad, normalizacion de errores, control de consumo, manejo de ambientes y proteccion de credenciales.

## Capas

### Domain

Contiene conceptos y reglas propias del proxy, independientes de infraestructura:

- `ExpedienteGdeba`
- `DocumentoGdeba`
- `TrataGdeba`
- `TipoDocumentoGdeba`
- `PaseExpedienteGdeba`
- `AplicacionConsumidora`
- `ServicioGdebaAutorizado`
- `SolicitudIntegracion`
- `PoliticaCache`
- `RegistroAuditoria`
- `AmbienteGdeba`

### Application

Orquesta casos de uso del proxy:

- Consultar expediente.
- Consultar expediente detallado.
- Validar expediente.
- Consultar historial de pases.
- Consultar documento.
- Obtener PDF de documento.
- Buscar documentos en expedientes.
- Consultar tratas y tipos documentales.
- Sincronizar expedientes por trata.
- Invalidar cache.
- Registrar auditoria.

La capa Application depende de interfaces, no de implementaciones SOAP, EF Core o SQL Server.

### Infrastructure

Implementa detalles tecnicos:

- Cliente JWT GDEBA.
- Clientes SOAP GDEBA.
- Repositorios EF Core/URF.
- Unit of Work.
- SQL Server.
- Cache persistida.
- Logging tecnico.
- Configuracion segura.
- Serializacion XML UTF-8.

### API

Expone endpoints internos para aplicaciones institucionales.

La API no debe exponer contratos SOAP crudos. Debe exponer operaciones internas estables, por ejemplo:

- `GET /api/gdeba/expedientes/{numero}`
- `GET /api/gdeba/expedientes/{numero}/detalle`
- `GET /api/gdeba/expedientes/{numero}/historial-pases`
- `GET /api/gdeba/documentos/{numero}/detalle`
- `GET /api/gdeba/documentos/{numero}/pdf`
- `GET /api/gdeba/tratas/{codigo}`
- `GET /api/gdeba/tipos-documento/{acronimo}`

## Repositorios y Gateways

Para SQL Server se utilizara el patron Unit of Work / Repository mediante URF, manteniendo el desacoplamiento respecto de Entity Framework.

Para GDEBA no se recomienda usar el termino Repository, ya que no representa la persistencia local del sistema. Se recomienda usar Gateways o Adapters.

Ejemplo:

```csharp
public interface IGdebaExpedienteGateway
{
    Task<ExpedienteGdeba?> BuscarExpedienteAsync(
        NumeroExpediente numero,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PaseExpedienteGdeba>> BuscarHistorialPasesAsync(
        NumeroExpediente numero,
        CancellationToken cancellationToken);
}
```

Implementacion actual:

```csharp
public sealed class GdebaSoapExpedienteGateway : IGdebaExpedienteGateway
{
}
```

Implementacion futura si GDEBA migra a REST:

```csharp
public sealed class GdebaRestExpedienteGateway : IGdebaExpedienteGateway
{
}
```

## Separacion de Responsabilidades

El proxy decide:

- Si una aplicacion puede consumir una operacion interna.
- Si responde desde cache o consulta GDEBA.
- Como audita la solicitud.
- Como normaliza errores.
- Como protege credenciales y tokens.
- Como serializa XML y maneja UTF-8.

El sistema consumidor decide:

- Por que necesita consultar un expediente.
- Que regla administrativa aplica a una obra, licitacion o certificado.
- Cuando corresponde iniciar o asociar un expediente a un proceso propio.
- Que workflow funcional corresponde.

## Base de Datos

La base local sera SQL Server.

Usos principales:

- Cache de expedientes.
- Cache de documentos y metadatos.
- Cache de PDF cuando corresponda.
- Catalogos de tratas y tipos documentales.
- Auditoria de solicitudes.
- Registro de sincronizaciones.
- Estado de frescura de datos.

## Ambientes

El proxy debe soportar al menos:

- HML: ambiente de homologacion GDEBA.
- PROD: ambiente productivo GDEBA.

Un ambiente local no significa tener GDEBA local. Significa ejecutar el proxy en desarrollo contra configuracion controlada, normalmente con una de estas opciones:

- Local apuntando a HML.
- Local con mocks/stubs de GDEBA para pruebas.
- Local con respuestas grabadas para pruebas automatizadas.

La decision sobre mocks locales queda pendiente.

