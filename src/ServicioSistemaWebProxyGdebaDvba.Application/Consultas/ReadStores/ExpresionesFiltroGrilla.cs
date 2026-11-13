using System.Linq.Expressions;
using System.Reflection;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Consultas.ReadStores;

// Traduccion de los filtros de columna de las grillas (Infragistics, resueltos en el servidor) a expresiones que EF manda en un solo
// WHERE. Las usan todas las consultas paginadas con filtro remoto: expedientes y documentos por trata, y el historico de alertas.
public static class ExpresionesFiltroGrilla
{
    private static readonly MethodInfo MetodoContains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    // Filtro de texto: la columna contiene alguno de los valores tipeados (O entre valores de la misma columna).
    public static Expression<Func<T, bool>> ContieneAlguno<T>(IReadOnlyCollection<string> valores, Expression<Func<T, string?>> selector)
    {
        ParameterExpression parametro = selector.Parameters[0];
        Expression? cuerpo = null;
        foreach (string valor in valores)
        {
            MethodCallExpression contiene = Expression.Call(selector.Body, ExpresionesFiltroGrilla.MetodoContains, Expression.Constant(valor));
            cuerpo = cuerpo is null ? contiene : Expression.OrElse(cuerpo, contiene);
        }

        return Expression.Lambda<Func<T, bool>>(cuerpo!, parametro);
    }

    // Variante para condiciones que no son un "contiene" directo sobre una columna (p. ej. "algun vinculo del documento esta en un
    // expediente cuyo numero contiene el valor"): arma una condicion por valor y las une con O sobre un mismo parametro.
    public static Expression<Func<T, bool>> AlgunoCumple<T>(IReadOnlyCollection<string> valores, Func<string, Expression<Func<T, bool>>> condicionPorValor)
    {
        ParameterExpression parametro = Expression.Parameter(typeof(T), "x");
        Expression? cuerpo = null;
        foreach (string valor in valores)
        {
            Expression<Func<T, bool>> condicion = condicionPorValor(valor);
            Expression condicionSobreParametro = new ReemplazoDeParametro(condicion.Parameters[0], parametro).Visit(condicion.Body);
            cuerpo = cuerpo is null ? condicionSobreParametro : Expression.OrElse(cuerpo, condicionSobreParametro);
        }

        return Expression.Lambda<Func<T, bool>>(cuerpo ?? Expression.Constant(true), parametro);
    }

    // Filtro de fecha de una columna (arbol Y/O de rangos, como lo arma la grilla) traducido a una unica expresion con el mismo
    // mecanismo que ContieneAlguno: EF lo manda como un solo WHERE, p. ej. "mes pasado O este mes" = (f >= d1 AND f < h1) OR (f >= d2 AND f < h2).
    public static Expression<Func<T, bool>> CumpleFiltroFecha<T>(FiltroFecha filtro, Expression<Func<T, DateTimeOffset?>> selector)
    {
        Expression? cuerpo = ExpresionesFiltroGrilla.ConstruirFiltroFecha(filtro, selector.Body);
        return Expression.Lambda<Func<T, bool>>(cuerpo ?? Expression.Constant(true), selector.Parameters[0]);
    }

    private static Expression? ConstruirFiltroFecha(FiltroFecha filtro, Expression fecha)
    {
        bool esO = string.Equals(filtro.Operador, "or", StringComparison.OrdinalIgnoreCase);
        IEnumerable<Expression?> terminos = (filtro.Rangos ?? Array.Empty<RangoFecha>()).Select(rango => ExpresionesFiltroGrilla.ConstruirRangoFecha(rango, fecha))
            .Concat((filtro.Subfiltros ?? Array.Empty<FiltroFecha>()).Select(subfiltro => ExpresionesFiltroGrilla.ConstruirFiltroFecha(subfiltro, fecha)));
        Expression? cuerpo = null;
        foreach (Expression? termino in terminos)
        {
            if (termino is null) continue;
            cuerpo = cuerpo is null ? termino : esO ? Expression.OrElse(cuerpo, termino) : Expression.AndAlso(cuerpo, termino);
        }

        return cuerpo;
    }

    private static Expression? ConstruirRangoFecha(RangoFecha rango, Expression fecha)
    {
        Expression? condicion = null;
        if (rango.Desde is DateTimeOffset desde) condicion = Expression.GreaterThanOrEqual(fecha, Expression.Constant(desde, fecha.Type));
        if (rango.Hasta is DateTimeOffset hasta)
        {
            Expression menor = Expression.LessThan(fecha, Expression.Constant(hasta, fecha.Type));
            condicion = condicion is null ? menor : Expression.AndAlso(condicion, menor);
        }

        return condicion;
    }

    private sealed class ReemplazoDeParametro : ExpressionVisitor
    {
        private readonly ParameterExpression _original;
        private readonly ParameterExpression _reemplazo;

        public ReemplazoDeParametro(ParameterExpression original, ParameterExpression reemplazo)
        {
            _original = original;
            _reemplazo = reemplazo;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _original ? _reemplazo : base.VisitParameter(node);
        }
    }
}
