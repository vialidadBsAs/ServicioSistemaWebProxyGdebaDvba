# Credenciales SQL fuera del repositorio y composicion del string de conexion

Fecha: 2026-09-17
Estado: implementado y validado en Api y Worker contra `SI-DB-01` (rama `feature/credenciales-sql-secrets`)

## 1. Proposito

Conectar la Api y el Worker a una base SQL Server con un login SQL (usuario y
contrasena) sin que la contrasena quede en ningun archivo versionado. El nombre
del servidor, la base y las opciones de conexion no son secretos y se
mantienen en `appsettings`; lo unico que vive fuera del repo es la credencial.

## 2. Modelo

El string de conexion se arma en tiempo de arranque a partir de dos piezas:

- **String base** (versionable, en `appsettings.{Entorno}.json`): servidor,
  base y opciones. Sin `User Id` ni `Password`.
- **Credencial nombrada** (fuera del repo): un fragmento de connection string
  `User Id=...;Password=...` guardado bajo la clave `SqlLogins:{Nombre}`.

El entorno elige que credencial usar con la clave `ProxyGdebaLogin`:

```json
{
  "ProxyGdebaLogin": "LoginDev",
  "ConnectionStrings": {
    "ProxyGdeba": "Server=SI-DB-01;Database=ServicioSistemaWebProxyGdebaDvba;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
  }
}
```

Reglas:

- Si `ProxyGdebaLogin` no esta definido, el string base se usa tal cual. Asi
  el entorno local con `Integrated Security=true` sigue funcionando sin cambios.
- Si `ProxyGdebaLogin` esta definido pero no existe `SqlLogins:{Nombre}`, la
  aplicacion falla al arrancar con un mensaje explicito. Nunca cae en silencio
  a otra credencial.
- La credencial es un fragmento nombrado por **proposito** (`LoginDev`,
  `LoginApp`, `LoginMigrador`), no por usuario. Cambiar de usuario en un entorno
  es tocar `ProxyGdebaLogin`, no el codigo.

Como el codigo lee `SqlLogins:{Nombre}` de `IConfiguration`, la credencial
puede venir de User Secrets (desarrollo), de una variable de entorno
(`SqlLogins__LoginProd`, produccion) o de un vault, sin cambios de codigo.

## 3. Implementacion

`Infrastructure/DependencyInjection.cs`, metodo `ResolverConnectionString`:
toma `ConnectionStrings:ProxyGdeba`, y si hay `ProxyGdebaLogin` anexa
`SqlLogins:{Nombre}` al final y normaliza el resultado con
`SqlConnectionStringBuilder`. Es el unico lugar donde se compone el string;
Api y Worker lo comparten porque ambos hosts llaman a `AddInfrastructure`.

## 4. Usuario de desarrollo general: `dev_proxygdeba`

Login SQL unico para todo el trabajo de desarrollo contra el servidor
`SI-DB-01` (SQL Server 2025, instancia por defecto). Sirve tanto para la
aplicacion como para correr migraciones (`update-database`), por lo que tiene
DDL y DML pero no puede borrar la base ni tocar el servidor:

```sql
CREATE LOGIN [dev_proxygdeba] WITH PASSWORD = '***', CHECK_POLICY = ON;
USE [ServicioSistemaWebProxyGdebaDvba];
CREATE USER [dev_proxygdeba] FOR LOGIN [dev_proxygdeba];   -- esquema por defecto: dbo
ALTER ROLE db_ddladmin   ADD MEMBER [dev_proxygdeba];       -- crear/alterar/borrar tablas, indices, SP
ALTER ROLE db_datareader ADD MEMBER [dev_proxygdeba];
ALTER ROLE db_datawriter ADD MEMBER [dev_proxygdeba];
GRANT EXECUTE TO [dev_proxygdeba];
```

- No es miembro de `db_owner`: `DROP DATABASE` le esta denegado.
- No es dueno de ningun esquema. En SSMS los roles se tildan en la pestana
  *Membership*, no en *Owned Schemas* (esa hace al usuario dueno del esquema
  homonimo del rol y no otorga permisos).
- Las tablas que crea o altera quedan en el esquema `dbo`, propiedad de `dbo`:
  la propiedad de objetos es por esquema, no por quien ejecuta el DDL. Correr
  migraciones con este login no cambia el owner de nada.

Credencial de desarrollo: `SqlLogins:LoginDev = "User Id=dev_proxygdeba;Password=***"`.

## 5. Como cargar la credencial en desarrollo (User Secrets)

Los User Secrets son por proyecto. La Api ya tenia `UserSecretsId`
(`dotnet-ServicioSistemaWebProxyGdebaDvba.Api`) y en ese mismo almacen viven
las credenciales JWT de GDEBA; se agrega la credencial SQL al mismo lugar.

```powershell
dotnet --% user-secrets set "SqlLogins:LoginDev" "User Id=dev_proxygdeba;Password=***" --project src/ServicioSistemaWebProxyGdebaDvba.Api
dotnet user-secrets list --project src/ServicioSistemaWebProxyGdebaDvba.Api
```

Advertencias operativas:

- **PowerShell parte el argumento** aunque vaya entre comillas: el valor tiene
  espacio, `=` y `;`, y la clave puede quedar guardada como
  `SqlLogins:LoginDev User`. Por eso el `--%` (stop-parsing), que pasa el resto
  de la linea sin procesar. Alternativa: editar directamente
  `%APPDATA%\Microsoft\UserSecrets\dotnet-ServicioSistemaWebProxyGdebaDvba.Api\secrets.json`
  y agregar la entrada `"SqlLogins:LoginDev": "User Id=...;Password=..."`.
- Verificar siempre con `user-secrets list` que la clave quedo exacta.
- Los User Secrets **no cifran**: su unica funcion es sacar la credencial del
  arbol del proyecto. Quedan en el perfil de Windows, protegidos solo por los
  permisos de archivo del usuario. Se cargan unicamente en entorno
  `Development` (la Api lee `ASPNETCORE_ENVIRONMENT`; el Worker,
  `DOTNET_ENVIRONMENT`).

## 6. Worker

El Worker accede a la base directamente (descubrimiento, detallado,
enriquecimiento documental corren en su proceso), asi que necesita la misma
credencial. Tiene su propio `UserSecretsId`
(`dotnet-ServicioSistemaWebProxyGdebaDvba.Worker-...`), por lo que la
credencial se carga por separado con el mismo comando apuntando a
`src/ServicioSistemaWebProxyGdebaDvba.Worker`, y su `appsettings.Development.json`
lleva el mismo string base y `ProxyGdebaLogin`. Configurado y validado con una
corrida de descubrimiento contra `SI-DB-01`.

El usuario GDEBA generico del Worker (`UsuarioConsulta`) es otra cosa: es un
nombre de usuario, no una credencial de base, y sigue en `appsettings`.

## 7. Produccion (criterio)

- Login **acotado** para la aplicacion: `db_datareader` + `db_datawriter` +
  `EXECUTE`, sin DDL. Migraciones con un login separado con `db_ddladmin`.
- Credencial por variable de entorno de la cuenta de servicio
  (`SqlLogins__LoginProd`) y `ProxyGdebaLogin=LoginProd`. Misma clave, otra
  fuente, mismo codigo. Un vault o DPAPI son mejoras posibles sobre esto.

## 8. Montaje de la base en otro servidor (referencia de motor)

Lo aplicado para llevar la base local (SQLEXPRESS) al developer `SI-DB-01`:

- `RESTORE DATABASE` crea la base; no hace falta crearla antes. Sin
  `WITH REPLACE` cuando es nueva.
- Al venir de otro servidor hay que reubicar los archivos (`WITH MOVE`; en el
  asistente de SSMS, *Files -> Relocate all files to folder*), porque el backup
  trae las rutas fisicas del origen.
- El `.bak` va en el directorio de backup de la instancia destino (la cuenta
  de servicio ya tiene lectura ahi) y tiene que estar en el disco del servidor.
- El backup trae los usuarios de base pero no los logins (viven en `master`).
  El owner de la base pasa automaticamente al login que ejecuta el restore; se
  fija a `sa` con `ALTER AUTHORIZATION ON DATABASE::[...] TO [sa]` para que no
  dependa de una cuenta personal. Con `sa` deshabilitado la base funciona igual.
- `sa` se deshabilita al final, y solo despues de verificar que se puede entrar
  con el login Windows `sysadmin` de la instancia (SSMS remoto usa la cuenta
  logoneada; con otra cuenta Windows, `runas /netonly`).
- Verificacion post-montaje: base `ONLINE`, owner `sa`, login habilitado,
  usuario mapeado (no huerfano) con esquema `dbo`, roles esperados, `EXECUTE`
  otorgado y ningun esquema en propiedad del usuario.
