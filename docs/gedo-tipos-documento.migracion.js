// Genera la parte de datos de la migracion EF "CargaCatalogoTiposDocumentoGedo" a partir de
// docs/gedo-tipos-documento.tsv (salida de gedo-tipos-documento.parsear.js).
//
// Uso: node docs/gedo-tipos-documento.migracion.js docs/gedo-tipos-documento.tsv \
//        src/ServicioSistemaWebProxyGdebaDvba.Infrastructure/Persistence/Migrations/<timestamp>_CargaCatalogoTiposDocumentoGedo.Datos.cs
//
// El archivo generado es una clase parcial con las sentencias SQL que la migracion ejecuta:
// - Up: MERGE sobre dbo.TiposDocumentoGdeba por Codigo. Inserta los tipos que faltan y, en los que ya
//   existen (nacidos de consultarTipoDocumento), solo rellena columnas que esten en NULL. Nunca pisa
//   nombre, sigla ni banderas informadas por GDEBA.
// - Down: borra los tipos de la semilla que siguen sin sigla y que ningun documento referencia.
//
// Mapeo de la columna "Caracteristicas" de GEDO a la entidad TipoDocumentoGdeba:
//   Importado / Libre / Template / Importado - Template -> TipoProduccion con el vocabulario que devuelve
//   consultarTipoDocumento (IMPORTADO, LIBRE, TEMPLATE, IMPORTADO-TEMPLATE); Template -> TieneTemplate
//   Firma Conjunta -> EsFirmaConjunta, Firma Externa -> EsFirmaExterna, Token -> TieneToken,
//   Especial -> EsEspecial, Notificable -> EsNotificable, Reservado -> EsConfidencial.
//   Transferible no tiene columna y se ignora. Una caracteristica ausente se interpreta como falsa,
//   porque GEDO solo muestra las que aplican.
// La familia se guarda tal cual la muestra GEDO: consultarTipoDocumento devuelve el mismo texto,
// incluido el literal "Sin Familia".
const fs = require('fs');
const [entradaTsv, salidaCs] = process.argv.slice(2);
if (!entradaTsv || !salidaCs) {
  console.error('Uso: node gedo-tipos-documento.migracion.js <entrada.tsv> <salida.Datos.cs>');
  process.exit(1);
}

const FILAS_POR_SENTENCIA = 500; // SQL Server limita el constructor VALUES a 1000 filas.
const TIPOS_PRODUCCION = [
  ['Importado - Template', 'IMPORTADO-TEMPLATE'],
  ['Importado', 'IMPORTADO'],
  ['Libre', 'LIBRE'],
  ['Template', 'TEMPLATE'],
];
const BANDERAS = [
  ['Firma Conjunta', 'EsFirmaConjunta'],
  ['Firma Externa', 'EsFirmaExterna'],
  ['Token', 'TieneToken'],
  ['Especial', 'EsEspecial'],
  ['Notificable', 'EsNotificable'],
  ['Reservado', 'EsConfidencial'],
];
const COLUMNAS_SEMILLA = ['Codigo', 'Nombre', 'Descripcion', 'Familia', 'TipoProduccion', 'TieneTemplate', ...BANDERAS.map(([, columna]) => columna)];
const COLUMNAS_RELLENABLES = COLUMNAS_SEMILLA.filter(columna => columna !== 'Codigo' && columna !== 'Nombre');

function literalTexto(valor) {
  return valor && valor.trim().length > 0 ? `N'${valor.trim().replace(/'/g, "''")}'` : 'NULL';
}

function literalBit(valor) {
  return `CAST(${valor ? 1 : 0} AS bit)`;
}

function leerTipos(rutaTsv) {
  const lineas = fs.readFileSync(rutaTsv, 'utf8').split(/\r?\n/).filter(linea => linea.trim().length > 0);
  const encabezado = lineas.shift().split('\t');
  const indice = nombre => {
    const posicion = encabezado.indexOf(nombre);
    if (posicion < 0) throw new Error(`El TSV no tiene la columna ${nombre}`);
    return posicion;
  };
  const columnas = { familia: indice('Familia'), acronimo: indice('Acronimo'), nombre: indice('Nombre'), descripcion: indice('Descripcion'), caracteristicas: indice('Caracteristicas') };
  const tipos = [];
  const acronimosVistos = new Set();
  for (const linea of lineas) {
    const celdas = linea.split('\t');
    const acronimo = (celdas[columnas.acronimo] || '').trim().toUpperCase();
    const nombre = (celdas[columnas.nombre] || '').trim();
    if (!acronimo || !nombre) throw new Error(`Fila sin acronimo o nombre: ${linea}`);
    if (acronimosVistos.has(acronimo)) throw new Error(`Acronimo repetido en el TSV: ${acronimo}`);
    acronimosVistos.add(acronimo);
    const familia = (celdas[columnas.familia] || '').trim();
    const caracteristicas = (celdas[columnas.caracteristicas] || '').split(',').map(c => c.trim()).filter(c => c.length > 0);
    const [, tipoProduccion = ''] = TIPOS_PRODUCCION.find(([etiqueta]) => caracteristicas.includes(etiqueta)) || [];
    tipos.push({
      acronimo,
      nombre,
      descripcion: (celdas[columnas.descripcion] || '').trim(),
      familia,
      tipoProduccion,
      tieneTemplate: tipoProduccion.includes('TEMPLATE'),
      banderas: BANDERAS.map(([etiqueta]) => caracteristicas.includes(etiqueta)),
    });
  }
  return tipos;
}

function filaValues(tipo) {
  return `(${[
    literalTexto(tipo.acronimo),
    literalTexto(tipo.nombre),
    literalTexto(tipo.descripcion),
    literalTexto(tipo.familia),
    literalTexto(tipo.tipoProduccion),
    literalBit(tipo.tieneTemplate),
    ...tipo.banderas.map(literalBit),
  ].join(', ')})`;
}

function sentenciaMerge(tipos) {
  const asignaciones = COLUMNAS_RELLENABLES.map(columna => `    target.${columna} = COALESCE(target.${columna}, source.${columna})`).join(',\n');
  return `MERGE dbo.TiposDocumentoGdeba AS target
USING (VALUES
${tipos.map(filaValues).join(',\n')}
) AS source (${COLUMNAS_SEMILLA.join(', ')})
ON target.Codigo = source.Codigo
WHEN MATCHED THEN UPDATE SET
${asignaciones}
WHEN NOT MATCHED BY TARGET THEN INSERT
    (Id, Codigo, CodigoTipoDocumentoGdeba, Nombre, Descripcion, Familia, TipoProduccion, Estado, TieneTemplate, ${BANDERAS.map(([, columna]) => columna).join(', ')}, EsResolucion, Activo)
VALUES
    (NEWID(), source.Codigo, NULL, source.Nombre, source.Descripcion, source.Familia, source.TipoProduccion, NULL, source.TieneTemplate, ${BANDERAS.map(([, columna]) => `source.${columna}`).join(', ')}, CAST(0 AS bit), CAST(1 AS bit));`;
}

function sentenciaReversion(tipos) {
  return `DELETE target
FROM dbo.TiposDocumentoGdeba AS target
WHERE target.CodigoTipoDocumentoGdeba IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.DocumentosGdeba AS documento WHERE documento.TipoDocumentoId = target.Id)
  AND target.Codigo IN (
${tipos.map(tipo => `    ${literalTexto(tipo.acronimo)}`).join(',\n')}
);`;
}

function literalVerbatim(sql) {
  return `@"${sql.replace(/"/g, '""')}"`;
}

const tipos = leerTipos(entradaTsv);
const lotes = [];
for (let desde = 0; desde < tipos.length; desde += FILAS_POR_SENTENCIA) lotes.push(tipos.slice(desde, desde + FILAS_POR_SENTENCIA));

const contenido = `// <auto-generated>
// Generado por docs/gedo-tipos-documento.migracion.js a partir de docs/gedo-tipos-documento.tsv
// (${tipos.length} tipos documentales del arbol "Iniciar Produccion de Documento" de GEDO). No editar a mano.
// </auto-generated>
namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    public partial class CargaCatalogoTiposDocumentoGedo
    {
        private static readonly string[] SentenciasCarga =
        {
${lotes.map(lote => `            ${literalVerbatim(sentenciaMerge(lote))}`).join(',\n')}
        };

        private const string SentenciaReversion = ${literalVerbatim(sentenciaReversion(tipos))};
    }
}
`;
fs.writeFileSync(salidaCs, '﻿' + contenido, 'utf8'); // BOM, como el resto de las migraciones
console.log(`Tipos: ${tipos.length}, sentencias MERGE: ${lotes.length}, salida: ${salidaCs}`);
