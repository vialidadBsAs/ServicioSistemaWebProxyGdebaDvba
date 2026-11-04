// Parsea el outerHTML del arbol "Iniciar Produccion de Documento" de GEDO (ZK tree) y emite un TSV:
// Familia, Acronimo, Nombre, Descripcion, Caracteristicas. Uso: node parsear-tipos-gedo.js <entrada.html> <salida.tsv>
const fs = require('fs');
const [entrada, salida] = process.argv.slice(2);
const html = fs.readFileSync(entrada, 'utf8');

function decodificar(texto) {
  return texto
    .replace(/&nbsp;/g, ' ')
    .replace(/&quot;/g, '"')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&#(\d+);/g, (_, n) => String.fromCharCode(Number(n)))
    .replace(/&amp;/g, '&')
    .replace(/\s+/g, ' ')
    .trim();
}

function textoCelda(td) {
  const titulo = td.match(/^<td[^>]*\stitle="([^"]*)"/);
  if (titulo && decodificar(titulo[1]).length > 0) return decodificar(titulo[1]);
  const interior = td.replace(/<[^>]+>/g, ' ');
  return decodificar(interior);
}

const filas = html.match(/<tr id="[^"]+" class="z-treerow[^"]*">[\s\S]*?<\/tr>/g) || [];
let familia = '';
const tipos = [];
for (const fila of filas) {
  const celdas = fila.match(/<td[\s\S]*?<\/td>/g) || [];
  if (fila.includes('z-tree-icon') && celdas.length === 1) {
    familia = textoCelda(celdas[0]);
    continue;
  }
  if (celdas.length < 3) continue;
  const nombre = textoCelda(celdas[0]);
  const acronimo = textoCelda(celdas[1]);
  const descripcion = textoCelda(celdas[2]);
  const caracteristicas = celdas[3]
    ? (celdas[3].match(/title="([^"]*)"/g) || []).map(t => decodificar(t.slice(7, -1))).join(', ')
    : '';
  tipos.push({ familia, acronimo, nombre, descripcion, caracteristicas });
}

const lineas = ['Familia\tAcronimo\tNombre\tDescripcion\tCaracteristicas'];
for (const t of tipos) lineas.push([t.familia, t.acronimo, t.nombre, t.descripcion, t.caracteristicas].join('\t'));
fs.writeFileSync(salida, lineas.join('\n'), 'utf8');

const porFamilia = new Map();
for (const t of tipos) porFamilia.set(t.familia, (porFamilia.get(t.familia) || 0) + 1);
const acronimos = new Set(tipos.map(t => t.acronimo));
console.log(`Tipos: ${tipos.length} (acronimos distintos: ${acronimos.size}), familias: ${porFamilia.size}`);
for (const [f, n] of [...porFamilia.entries()].sort((a, b) => b[1] - a[1])) console.log(`${String(n).padStart(4)}  ${f}`);
const duplicados = tipos.map(t => t.acronimo).filter((a, i, arr) => arr.indexOf(a) !== i);
if (duplicados.length) console.log('Acronimos repetidos: ' + [...new Set(duplicados)].join(', '));
