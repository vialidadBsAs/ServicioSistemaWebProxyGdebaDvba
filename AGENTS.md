# Guia de trabajo para agentes

Este archivo contiene reglas obligatorias para cualquier agente que analice o
modifique este repositorio. No reemplaza la documentacion de `docs/`: la resume
como criterio operativo y conserva decisiones acordadas durante el desarrollo.

## Antes de modificar codigo

1. Leer primero los documentos relevantes de `docs/`.
2. Revisar la implementacion existente y las convenciones del modulo afectado.
3. No modificar codigo cuando el usuario pide solamente analizar o explicar.
4. No introducir abstracciones, tablas, servicios o capas sin explicar antes
   que responsabilidad resuelven.
5. Mantener los cambios dentro de la rama activa indicada por el usuario.

## Contexto explicito en las llamadas

Toda llamada debe mostrar claramente quien ejecuta la operacion.

- Metodos de la misma instancia: usar siempre `this.`.
- Colaboradores inyectados o campos privados: usar siempre el campo con prefijo
  `_`.
- Metodos estaticos: usar el nombre del tipo.
- Variables locales: usar nombres que expresen su rol; no usar prefijo `_`.

Ejemplos:

```csharp
var expediente = await _expedienteCacheReadStore.CargarExpedienteAsync(
    numero.Valor,
    cancellationToken);

this.RegistrarCambiosExpediente(expediente, expedienteEsNuevo);

var numero = NumeroGdebaCompleto.Create(valor);
```

No escribir llamadas ambiguas como:

```csharp
RegistrarCambiosExpediente(expediente, expedienteEsNuevo);
ConsultarAsync(fecha, cancellationToken);
```

La lectura del codigo debe permitir distinguir inmediatamente entre:

- comportamiento de la clase actual;
- operacion delegada a otro servicio;
- comportamiento del aggregate;
- operacion de un value object;
- funcion estatica.

## Campos, parametros y variables

- Todos los campos privados de instancia usan prefijo `_`.
- No ocultar un campo con un parametro o variable local de significado ambiguo.
- Los nombres deben expresar responsabilidad, no solamente tecnologia.
- Evitar nombres generales como `manager`, `helper`, `processor`, `handler`,
  `registro` o `servicio` cuando no aclaran el caso de uso.

## Formato de metodos y llamadas

- Escribir la firma completa de un metodo en una sola linea mientras siga siendo
  razonablemente legible.
- Escribir las llamadas con sus argumentos en una sola linea mientras sea
  posible.
- Pasar a multiples lineas solamente cuando la longitud o la complejidad de los
  argumentos dificulte la lectura.
- No expandir automaticamente cada argumento en una linea distinta.
- Si una llamada necesita varias lineas, agrupar los argumentos de forma
  semantica y evitar un formato excesivamente vertical.

Preferir:

```csharp
private Expediente CrearExpediente(NumeroGdebaCompleto numeroGdebaCompleto)
```

```csharp
await this.RegistrarAuditoriaAsync(operacion, recurso, fuente, exitoso, fecha, cancellationToken);
```

Evitar, cuando la linea entra con claridad:

```csharp
private Expediente CrearExpediente(
    NumeroGdebaCompleto numeroGdebaCompleto)
```

```csharp
await this.RegistrarAuditoriaAsync(
    operacion,
    recurso,
    fuente,
    exitoso,
    fecha,
    cancellationToken);
```

## Aggregate roots

Los aggregate roots deben ser reconocibles tanto por ubicacion como por archivo.

- La entidad principal sigue siendo la entidad persistida.
- Su comportamiento de aggregate se implementa en
  `Domain/AggregateRoots/<Entidad>.AggregateRoot.cs` mediante clase parcial.
- No crear una segunda clase con sufijo `AggregateRoot`.
- Las modificaciones de colecciones internas se realizan mediante metodos del
  aggregate, no desde servicios manipulando listas directamente.
- En codigo de Application, nombrar la variable segun el aggregate concreto:
  `expediente`, `documento`, etc.; nunca `entidad`.

Aggregate roots actuales:

- `Expediente`
- `DocumentoGdeba`
- `TrataGdeba`

Antes de agregar o quitar un aggregate root, revisar y actualizar
`docs/07-arquitectura-tecnica-implementacion.md`.

## Value objects

Los value objects deben ser reconocibles por su ubicacion y semantica.

- Se ubican exclusivamente en `Domain/ValueObjects`.
- Modelan un valor completo; no reemplazan la entidad que lo contiene.
- No tienen identidad persistente propia.
- Se construyen mediante una operacion explicita como `Create`.
- En llamadas, usar el tipo para que su naturaleza sea visible:
  `NumeroGdebaCompleto.Create(valor)`.
- No agregar sufijos `Dto`, `Entity` o `Model` a un value object.
- No llamar `Numero` a un identificador compuesto completo.

`NumeroGdebaCompleto` es el value object comun para identificadores GDEBA
compuestos. El tipo (`EX`, `IF`, etc.) determina la clase de elemento; no se
crean value objects separados solo por tratarse de expediente o documento.

## Application

- Application contiene casos de uso y coordinacion.
- Los servicios de aplicacion usan contratos pequenos y cohesivos.
- La persistencia local se accede mediante URF:
  `IRepository<T>`, `ITrackableRepository<T>` e `IUnitOfWork`.
- Application no usa `DbContext`.
- Las consultas de solo lectura no necesitan `IUnitOfWork`.
- Un caso de uso confirma sus cambios una sola vez con
  `IUnitOfWork.SaveChangesAsync`.
- Los servicios colaboradores preparan o agregan cambios, pero no guardan por
  su cuenta cuando participan de una operacion coordinada.

## Infrastructure

- Infrastructure implementa detalles tecnicos: EF Core, SQL Server, SOAP, JWT,
  mensajeria y filesystem.
- No contiene reglas de negocio ni coordina casos de uso.
- No usa repositorios URF para ejecutar logica de aplicacion.
- Las configuraciones EF pueden conocer entidades de Domain.
- Los gateways externos informan resultados tecnicos; Application decide como
  integrarlos al caso de uso y a su transaccion.

## URF y persistencia

- Las entidades persistidas heredan de `DomainEntity`, compatible con la
  `Entity` trackeable de URF.
- No crear implementaciones propias de Repository o Unit of Work.
- No llamar directamente a `SaveChangesAsync` desde Infrastructure.
- No ejecutar un `SaveChangesAsync` por cada servicio colaborador.
- No usar `DbContext` desde Application.
- Para cargar navegaciones, usar la abstraccion `IQuery<TEntity>.Include`.

## Modulos transversales

Auditoria, control de cuotas y seguridad se organizan por capa:

```text
Application/Transversales/{Auditoria,ControlCuotas,Seguridad}
Domain/Transversales/{Auditoria,ControlCuotas,Seguridad}
Infrastructure/Transversales/{Auditoria,ControlCuotas,Seguridad}
```

La ubicacion transversal no elimina la separacion de responsabilidades:

- Domain contiene conceptos y reglas.
- Application contiene contratos, modelos y casos de uso.
- Infrastructure contiene mecanismos tecnicos y configuraciones persistentes.
- API contiene controladores y middleware.

## Cache de expedientes

- `ExpedienteCacheControl` controla la vigencia del detalle del expediente.
- `HistorialExpedienteCacheControl` controla la vigencia del conjunto de
  movimientos.
- `DocumentoCacheControl` controla el cache del archivo fisico del documento.
- Los movimientos persistidos son datos del expediente, no registros de control
  de cache.
- Los movimientos legales anteriores no se reescriben innecesariamente; se
  agregan los nuevos y solo se actualiza el ultimo cuando corresponda.

## Integracion GDEBA

- GDEBA se representa mediante gateways, nunca mediante repositories.
- Cada invocacion debe indicar su origen:
  `Interactiva`, `RefrescoManual`, `WorkerProgramado`, `Mensajeria` o
  `Administrativo`.
- Una invocacion consume cuota solamente si el servidor SOAP respondio.
- La medicion HTTP/SOAP pertenece a Infrastructure.
- El registro, las reglas de cuota y las consultas pertenecen al modulo
  transversal de Application/Domain.

## Verificacion obligatoria

Despues de modificar codigo:

1. Ejecutar `dotnet build` sobre la solucion.
2. Verificar que no aparezcan accesos directos a `DbContext` o guardados fuera
   de la capa acordada.
3. Si cambia el modelo persistente, generar y revisar la migracion sin ejecutar
   `Update-Database`, salvo autorizacion explicita.
4. Si solo se reorganizan archivos, comprobar que EF no detecte cambios de
   modelo.
