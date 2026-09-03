# 11 — Análisis de rendimiento del worker de expediente detallado (sept. 2026)

Registro del diagnóstico de la degradación de tiempos de las corridas nocturnas observada entre el 30/08 y el 01/09 de 2026, y de la decisión de **no modificar el timeout HTTP** hacia GDEBA.

## 1. Síntoma

Las corridas de "Expediente detallado" (lote fijo de 500) saltaron de ~5-7 minutos a ~25 minutos de forma **abrupta** entre la última corrida del sábado 30/08 (04:54, 6m35s) y la primera del domingo 31/08 (04:02, 25m09s). No fue una degradación gradual: el viernes y el sábado oscilaron en el rango normal (305-537 s) y el domingo abrió directamente en 1.508 s. Con la ventana nocturna de 1 hora, entraron 2 corridas por noche en lugar de 5-6.

## 2. Causa: cambio de población consultada (cruce a 2023)

El worker recorre los expedientes sin detallar **del más nuevo al más viejo**. La auditoría muestra el año de los expedientes consultados por noche:

| Noche | Población consultada | Promedio HTTP por llamada exitosa |
|---|---|---|
| Vie 29/08 | 2024 y 2025 | 234 ms |
| Sáb 30/08 | 2024 (64%) + entra a 2023 (36%) | 332 ms |
| Dom 31/08 | 100% 2023 | 710 ms |
| Lun 01/09 | 100% 2023 | 829 ms |

Durante la noche del sábado se agotó 2024 y el domingo la corrida fue 2023 puro. Un expediente de 2023 arrastra ~3 años de historial de pases: la respuesta SOAP es mucho más grande y lenta de generar del lado GDEBA. El escalón de tiempos coincide exactamente con el cruce de año — no hubo cambio en GDEBA, en la red ni en la máquina local.

**Verificación experimental (03/09):** con un parche temporal (ya revertido) se corrió un lote manual de 500 refrescando los expedientes **más nuevos ya detallados**: duró **3m11s** contra los 18m54s de la corrida manual equivalente del 01/09 sobre 2023. Salvedad registrada: ese total mezcla dos efectos (respuestas GDEBA chicas + casi nada para escribir localmente por ser refrescos "sin cambios"), por lo que el reparto fino entre ambos se lee del promedio HTTP por llamada, no del total.

La medición base de todo el análisis es `DuracionMilisegundos` de `IntegracionGdeba_Invocaciones`: cronometra únicamente el `SendAsync` HTTP (Stopwatch en los gateways SOAP), sin parseo ni persistencia local.

## 3. Segundo factor: los errores pesan cada vez más

Los errores acompañan el envejecimiento de la población, y cada uno cuesta ~30 segundos:

| Noche | Fallidas | Tiempo en fallidas | % del tiempo GDEBA |
|---|---|---|---|
| 29/08 | 2 | 60 s | 4% |
| 30/08 | 5 | 151 s | 8% |
| 31/08 | 6 | 181 s | 11% |
| 01/09 | 19 | 578 s | **26%** |

A ~830 ms la llamada exitosa sobre 2023, **un error cuesta lo que ~36 llamadas buenas**. La interpretación: expediente más viejo → respuesta más pesada → más lenta y con más probabilidad de que GDEBA no llegue a armarla a tiempo. Errores puntuales tipo `cvc-elt.1.a` (validador XML de GDEBA que no resuelve su propio esquema) son transitorios del lado servidor (~1% de las llamadas de historial, misma tasa en números de 7 y 8 dígitos — **descartado** que sea un problema de formato/padding del número); el expediente queda `EstaCompleto = 0` y el recorrido lo reintenta naturalmente.

## 4. Por qué NO se toca el timeout (decisión del 03/09/2026)

Idea evaluada: si el promedio es ~830 ms, poner un timeout corto para que los errores fallen rápido en vez de quemar 30 s.

Hallazgos que la descartan:

1. **El corte de ~30 s no es nuestro: es de GDEBA.** El `HttpClient` de los gateways no tiene timeout configurado (default 100 s). Los ~30 s por error son el servidor de GDEBA rindiéndose y devolviendo el error. "Optimizar el timeout" significaría *agregar* uno propio por debajo de 30 s.
2. **No hay tajo limpio donde cortar.** Distribución de las llamadas **exitosas** sobre población 2023 (4.196 llamadas, 31/08 en adelante): 94% ≤ 2 s; 168 entre 2-5 s; 42 entre 5-10 s; 31 entre 10-20 s; **9 éxitos legítimos por encima de 20 s (máximo 29,8 s)**. La cola de éxitos llega justo hasta donde GDEBA corta: cualquier timeout propio cambia tiempo por completitud casi 1:1.
   - Timeout 20 s: ahorra ~3 min/noche (perfil del 01/09), sacrifica ~0,2% de éxitos.
   - Timeout 10 s: ahorra ~6-7 min/noche, mata ~1% de éxitos y convierte a los expedientes "gordos pero viables" en reincidentes eternos que fallan cada noche por nuestro propio corte.
3. **La palanca real es otra.** El expediente que da timeout noche tras noche seguirá quemando 20-30 s por noche con cualquier timeout. A ese lo cura la mejora pendiente de backoff/exclusión de reincidentes (consulta de historial por trata/estado, ya anotada como pendiente de administración).

**Conclusión: dejar el timeout como está.** El ahorro posible es chico (~3 min/noche en el mejor punto defendible, 20-25 s), el riesgo es degradar completitud, y el desperdicio grande se ataca con el tratamiento de reincidentes, no con el timeout.

## 5. Consecuencia operativa aceptada

Los ~20-25 minutos por lote de 500 son el **costo real de la etapa 2023-hacia-atrás** del recorrido, no una falla. Quedaban ~19.600 sin detallar al 03/09, todos 2023 o anteriores (2022 será previsiblemente peor). Con 2 lotes por noche en la ventana de 1 hora, el horizonte de cobertura se mide en meses. Si eso resulta inaceptable, el ajuste es de operación (alcance de tratas, ventana, lote) — decisión abierta, no tomada.

Queda también a observar si los expedientes con timeout **reinciden** noche tras noche (verificable en `Auditoria_Registros` comparando recursos fallidos entre fechas); si se confirma, prioriza la mejora de backoff/exclusión.
