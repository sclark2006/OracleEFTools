using System.Data;
using System.Data.Common;
using System.Reflection;

namespace OracleEFTools;

public sealed class ProcedureBuilder
{
    internal ProcedureBuilder(OracleDbContext dbContext) => _dbContext = dbContext;

    private readonly Dictionary<string, IEnumerable<DbParameter>> _procedureDefinition = new();

    private readonly OracleDbContext _dbContext;
    public ProcedureBuilder Procedure<TParams>(Action<Procedure<TParams>> buildAction)
    {
        ArgumentNullException.ThrowIfNull(buildAction, nameof(buildAction));

        var procedure = new Procedure<TParams>(_dbContext);

        var procProperties = _dbContext.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.PropertyType == typeof(DBProc<TParams>)).ToList();

        if(procProperties.Count == 0)
            throw new InvalidOperationException($"No se encontró ninguna propiedad de tipo DBProc<{typeof(TParams).Name}>");
        if (procProperties.Count > 1)
            throw new InvalidOperationException("No se puede tener mas de un procedimiento con los mismos parámetros");

         procProperties[0].SetValue(_dbContext, new DBProc<TParams>(buildAction, procedure));

        _procedureDefinition[$"{procedure.Schema}.{procedure.Name}"] = procedure.Parameters.Select(x => x.Item2);
        return this;
    }

    public IReadOnlyDictionary<string, IEnumerable<DbParameter>> Definitions => _procedureDefinition;

}
