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

        var procedure = new Procedure<TParams>();

        var procProperties = _dbContext.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.PropertyType == typeof(DBProc<TParams>)).ToList();

        if(procProperties.Count == 0)
            throw new InvalidOperationException($"No property of type DBProc<{typeof(TParams).Name}> was found");
        if (procProperties.Count > 1)
            throw new InvalidOperationException("You cannot have more than one procedure with the same parameters");

         procProperties[0].SetValue(_dbContext, new DBProc<TParams>(_dbContext,buildAction, procedure));

        _procedureDefinition[$"{procedure.Schema}.{procedure.Name}"] = procedure.Parameters.Select(x => x.Item2);
        return this;
    }
}
