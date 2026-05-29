# Notas de Trabajo y Decisiones Operativas

Version: 0.2  
Fecha: 29/05/2026  
Estado: Documento de continuidad

## 1. Proposito de este documento

Este documento existe para conservar el contexto de trabajo que se genero durante la construccion inicial del proyecto. No reemplaza a la documentacion tecnica formal, pero ayuda a retomar el desarrollo desde otra maquina, desde otra sesion de Codex o despues de varios dias sin perder decisiones importantes.

La conversacion de trabajo fue extensa e incluyo decisiones de arquitectura, problemas de Git, dudas sobre GDEBA, criterios de cache, autenticacion, auditoria y estructura de solucion. Como el historial del chat no debe ser la unica fuente de verdad, las conclusiones relevantes se consolidan aca.

## 2. Estado general del proyecto

El proyecto ya cuenta con una solucion .NET 8 inicial, separada por capas, con documentacion base, commit inicial y repositorio remoto en GitHub.

La solucion todavia esta en una etapa temprana. El objetivo de lo construido hasta ahora no fue cerrar la integracion real con GDEBA, sino dejar una base ordenada para poder avanzar sin mezclar responsabilidades tecnicas, reglas propias del proxy y detalles de transporte SOAP.

## 3. Repositorio, rutas y continuidad de Codex

La ruta real del proyecto es:

```text
C:\Users\Admin\Documents\ServicioSistemaWebProxyGdebaDvba
```

Durante el trabajo inicial, el chat de Codex habia quedado asociado a una carpeta anterior:

```text
C:\Users\Admin\Documents\New project
```

Para no perder continuidad dentro de este chat se creo un junction de Windows:

```text
C:\Users\Admin\Documents\New project
  -> C:\Users\Admin\Documents\ServicioSistemaWebProxyGdebaDvba
```

Esto permite que una herramienta que todavia apunte a `New project` trabaje realmente sobre la carpeta renombrada. Si se abre el proyecto desde cero en otra maquina, lo mas claro es abrir directamente la ruta real del repositorio.

## 4. Identidad Git y GitHub

El repositorio remoto debe permanecer bajo el propietario:

```text
vialidadBsAs
```

Los commits deben quedar atribuidos al usuario:

```text
pdelucca <pablofranciscodelucca@hotmail.com>
```

Durante las pruebas se detecto que el email:

```text
pdelucca@vialidad.gba.gov.ar
```

era valido a nivel Git, pero GitHub lo atribuia visualmente a `vialidadBsAs`. Por eso, para mantener la autoria personal correcta en GitHub, el repositorio local debe usar el email `pablofranciscodelucca@hotmail.com`.

Comandos recomendados dentro del repo:

```powershell
git config user.name "pdelucca"
git config user.email "pablofranciscodelucca@hotmail.com"
git log --format=fuller -1
```

La verificacion debe mostrar `Author` y `Commit` con el mismo usuario y email.

## 5. Remoto GitHub

El repositorio remoto se trabaja bajo `vialidadBsAs`, pero puede pushearse con `pdelucca` siempre que esa cuenta tenga permisos.

Si por algun motivo hubiera que recrear el remoto, conviene crearlo vacio: sin README, sin `.gitignore` y sin licencia. Luego se configura el remoto local y se sube `master`.

```powershell
git remote add origin https://github.com/vialidadBsAs/ServicioSistemaWebProxyGdebaDvba.git
git push -u origin master
```

Si el remoto ya existe, no debe recrearse ni forzarse nada sin revisar primero `git remote -v`, `git status` y `git log --format=fuller -1`.

## 6. Finales de linea en Windows

Visual Studio puede mostrar avisos de finales de linea inconsistentes, y Git puede emitir mensajes como:

```text
LF will be replaced by CRLF
```

Esto no significa que el proyecto este roto. Indica que Windows y Git estan normalizando finales de linea. Si se vuelve un problema recurrente, se puede agregar un `.gitattributes` para fijar una politica explicita por tipo de archivo.

## 7. Decision de arquitectura

Se decidio usar una arquitectura limpia con separacion en proyectos:

```text
Domain
Application
Infrastructure
Api
Worker
```

La razon principal es que, aunque el proyecto parece inicialmente un simple consumidor de servicios externos, en realidad tiene responsabilidades institucionales propias: auditoria, trazabilidad, cache, control de aplicaciones consumidoras, configuracion de ambientes, aislamiento de credenciales y evolucion futura del canal de integracion.

Separar las capas permite que un cambio tecnico de GDEBA, por ejemplo migrar de SOAP a REST, no obligue a modificar controladores, casos de uso o reglas internas del proxy.

## 8. Responsabilidad de cada capa

`Domain` contiene conceptos propios del proxy. No debe conocer HTTP, SOAP, EF Core, SQL Server ni archivos de configuracion. Dentro de esta capa hay tipos de distinta naturaleza: entidades, value objects, enumeraciones y clases base. No deben interpretarse todos como entidades de negocio equivalentes.

`Application` contiene casos de uso, DTOs e interfaces. Esta capa expresa lo que el proxy necesita hacer, pero no decide como se conecta tecnicamente a GDEBA ni como se persiste en SQL Server.

`Infrastructure` contiene implementaciones concretas: gateways, configuracion, acceso futuro a base de datos, servicios de auditoria y clientes tecnicos.

`Api` expone endpoints internos. No debe construir XML SOAP ni leer credenciales GDEBA.

`Worker` queda reservado para procesos de fondo, especialmente sincronizacion y refresco de cache.

## 9. GatewayMode

Se agrego `GatewayMode` para decidir la implementacion de integracion desde configuracion y no desde codigo.

Modos previstos:

- `Fake`: implementacion simulada para desarrollo.
- `Soap`: implementacion real contra servicios SOAP GDEBA.
- `Rest`: reservado por si GDEBA migra o expone servicios REST en el futuro.

Esta decision evita tener que cambiar registros manuales de dependencias cada vez que se quiera alternar entre una implementacion de prueba y una real.

## 10. Ambientes GDEBA

GDEBA tiene endpoints distintos por ambiente. Por eso la configuracion separa `HML` y `PROD`.

El ambiente activo se define con:

```json
"CurrentEnvironment": "HML"
```

La resolucion del ambiente no debe quedar hardcodeada en los casos de uso. Para eso se agrego `IGdebaExecutionContext`, que permite a Application conocer el ambiente activo sin depender directamente de `GdebaOptions`.

## 11. Middleware de aplicacion consumidora

Se implemento `ApplicationIdentificationMiddleware`, que lee el header:

```http
X-Application-Id
```

Este header es una convencion interna del proxy. En el estado actual sirve para identificar que aplicacion interna realizo la solicitud y registrar esa informacion en auditoria.

Por ahora no autoriza ni bloquea. Mas adelante puede evolucionar para validar aplicaciones habilitadas, aplicar politicas de consumo, registrar cuotas o rechazar consumidores desconocidos.

## 12. Autenticacion contra GDEBA

La autorizacion tecnica contra GDEBA se realiza mediante el servicio JWT:

```text
https://iop.gba.gob.ar/servicios/JWT/1/REST/jwt
```

El mecanismo informado es Basic Auth con username y password, para obtener un token JWT que luego se usa en las invocaciones autorizadas.

Las credenciales no deben enviarse por chat, no deben hardcodearse y no deben subirse al repositorio. La resolucion definitiva debe hacerse mediante configuracion segura: variables de entorno, user secrets, secret manager o mecanismo institucional equivalente.

## 13. SOAP y codificacion UTF-8

Durante las pruebas se detecto un punto no documentado en los contratos revisados: cuando la peticion XML contiene acentos o caracteres especiales, el servicio requiere declarar explicitamente el charset.

Header requerido:

```http
Content-Type: application/xml; charset=UTF-8
```

Sin ese charset, la respuesta podia no fallar tecnicamente, pero devolvia datos vacios. Este comportamiento fue indicado por el equipo tecnico que desarrolla el servicio GDEBA.

La implementacion SOAP real debe respetar ese header desde el inicio.

## 14. Servicios GDEBA prioritarios

Los metodos mas importantes para la siguiente etapa son:

- `buscarDatosExpedientePorCodigosTrata`
- `buscarHistorialPasesExpediente`

`buscarDatosExpedientePorCodigosTrata` es clave para obtener expedientes por trata, pero hoy tiene una limitacion operativa importante: obliga a recuperar muchos datos y filtrar localmente. Se solicito a GDEBA evaluar filtros por reparticion/dependencia y, eventualmente, criterios adicionales que permitan reducir el volumen.

`buscarHistorialPasesExpediente` es importante para movimientos y pases. Seria especialmente util contar con filtros por fecha para trabajar de forma incremental y evitar traer historiales completos en cada invocacion.

## 15. Cache y sincronizacion

La cache local es una responsabilidad central del proxy. No se trata solo de optimizar rendimiento; tambien permite reducir llamadas a GDEBA, mejorar disponibilidad ante cortes de conectividad y dar una respuesta mas estable a los sistemas internos.

La politica de cache no debe ser unica para todos los datos. Un PDF o documento oficial puede tener una frescura distinta a un expediente en tramitacion. Los expedientes, pases y estados requieren politicas mas cuidadosas porque pueden cambiar con frecuencia.

El Worker sera importante si se decide ejecutar refrescos programados, sincronizaciones por trata, precargas o procesos incrementales. Sin Worker, la cache solo se actualizaria como consecuencia de requests HTTP, lo cual simplifica el despliegue pero limita la estrategia de actualizacion.

## 16. Auditoria y trazabilidad

La auditoria debe permitir responder, como minimo:

- Que aplicacion interna consulto.
- Que operacion pidio.
- Que identificador funcional uso, por ejemplo numero de expediente.
- En que ambiente GDEBA se ejecuto.
- Si la respuesta vino de cache o de GDEBA.
- Si la operacion fue exitosa.
- Fecha y hora de resolucion.

En el estado actual la auditoria es inicial. Mas adelante debe persistirse en SQL Server y evitar registrar credenciales, tokens, XML sensible o contenido documental innecesario.

## 17. Seguridad interna

Por ahora el proxy se considera un servicio interno consumido por aplicaciones institucionales conocidas. Aun asi, conviene distinguir entre identificacion y autorizacion.

Identificacion significa saber quien llama, por ejemplo mediante `X-Application-Id`. Autorizacion significa decidir si esa aplicacion puede ejecutar determinada operacion.

El proyecto todavia no implementa autorizacion real de consumidores. Esta decision queda pendiente porque la institucion tambien evalua un servicio troncal de seguridad y roles. El proxy debe poder integrarse con esa estrategia sin duplicar innecesariamente reglas de seguridad.

## 18. Persistencia y URF

Se preve SQL Server como base local.

El equipo suele trabajar con una abstraccion sobre Entity Framework mediante Unit of Work y Repository, usando URF. La idea general es conservar ese criterio tambien en este proyecto para desacoplar dominio/aplicacion de infraestructura de persistencia.

Esto aplica a datos propios del proxy, por ejemplo cache, auditoria, aplicaciones consumidoras o configuracion persistida. No debe confundirse Repository con clientes de servicios externos: para GDEBA corresponde hablar de gateways o clients, no de repositorios.

## 19. Criterio para agregar un nuevo metodo GDEBA

Para sumar un metodo nuevo conviene seguir este orden:

1. Definir el caso de uso en Application.
2. Definir request/response internos, sin exponer SOAP crudo.
3. Extender o crear una interfaz de gateway.
4. Implementar respuesta fake para desarrollo.
5. Agregar endpoint en Api.
6. Registrar auditoria.
7. Implementar SOAP real en Infrastructure.
8. Incorporar cache si corresponde.
9. Agregar pruebas.

Este orden permite avanzar de forma incremental sin esperar a tener resuelta toda la infraestructura externa.

## 20. Cosas a evitar

No construir XML SOAP en controladores.

No leer headers HTTP dentro de servicios de Application.

No hardcodear `HML` o `PROD` en casos de uso.

No inyectar `GdebaOptions` directamente en Application.

No exponer SOAP crudo como contrato de la API interna.

No registrar credenciales, tokens, XML sensible o documentos completos en logs.

No trasladar reglas de negocio de Obras, Licitaciones o Certificaciones al proxy.

## 21. Pendientes tecnicos

Quedan pendientes para siguientes iteraciones:

- Implementar cliente JWT real.
- Implementar cliente SOAP real.
- Modelar SQL Server.
- Incorporar URF en persistencia.
- Definir tablas de auditoria.
- Definir tablas de cache.
- Definir tabla o mecanismo para aplicaciones consumidoras habilitadas.
- Implementar cache con politica de frescura.
- Implementar `buscarDatosExpedientePorCodigosTrata`.
- Implementar `buscarHistorialPasesExpediente`.
- Agregar pruebas unitarias y de integracion.
- Revisar `.gitattributes` para finales de linea.

