# Integracion GDEBA

## Autenticacion Tecnica

La autorizacion hacia GDEBA se realiza mediante JWT:

- Endpoint PROD: `https://iop.gba.gob.ar/servicios/JWT/1/REST/jwt`
- Autenticacion: `Authorization` con Basic Auth.
- Credenciales: `username` y `password`.

Las credenciales deben almacenarse mediante configuracion segura:

- Variables de entorno.
- User Secrets para desarrollo .NET.
- Secret manager institucional si estuviera disponible.
- Archivo de configuracion local excluido de control de version solo para desarrollo.

No se deben registrar en logs:

- Username/password.
- Token JWT completo.
- Header Authorization completo.

## SOAP

Los servicios autorizados son SOAP 1.0.

El consumo SOAP debe encapsularse en gateways/adapters de infraestructura. La capa Application no debe construir XML ni conocer detalles de WSDL.

Requisito tecnico:

```http
Content-Type: application/xml; charset=UTF-8
```

Este requisito es especialmente importante para parametros con acentos o caracteres especiales.

## Manejo de Ambientes

La configuracion debe soportar:

- `HML`
- `PROD`

Cada ambiente debe definir:

- Endpoint JWT.
- WSDL o endpoint SOAP por servicio.
- Credenciales.
- Timeouts.
- Politicas de reintento.
- Flags de logging tecnico.

## Errores

Los errores externos deben normalizarse antes de llegar a aplicaciones consumidoras.

Categorias recomendadas:

- Error de autenticacion GDEBA.
- Token expirado o invalido.
- Servicio GDEBA no disponible.
- Timeout.
- Fault SOAP.
- Peticion invalida por formato.
- Recurso inexistente.
- Respuesta vacia no esperada.
- Respuesta parcial.

El proxy debe registrar el detalle tecnico internamente, pero devolver a consumidores una respuesta estable.

## Observabilidad

Cada llamada a GDEBA debe registrar:

- Aplicacion interna solicitante.
- Operacion interna solicitada.
- Metodo GDEBA invocado.
- Ambiente.
- Fecha y hora.
- Duracion.
- Resultado.
- Si uso cache o llamada externa.
- Identificador de correlacion.
- Numero de expediente/documento cuando aplique.

Datos sensibles deben enmascararse.

## Evolucion

Si GDEBA migra de SOAP a REST o habilita nuevos metodos:

- Se agregara o reemplazara la implementacion del gateway.
- Se mantendran contratos internos del proxy siempre que sea posible.
- No deberia requerirse modificar sistemas consumidores.

