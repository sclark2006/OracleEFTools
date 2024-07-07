
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace OracleEFTools;

public abstract class OracleDbContext : DbContext
{
    public OracleDbContext()
    {
        _proceduresBuilder = new ProcedureBuilder(this);
        OnProcedureCall(_proceduresBuilder);
    }

    private readonly ProcedureBuilder _proceduresBuilder;


    protected virtual void Proc(string procedureName, params DbParameter[] parameters)
    {
        var query = string.Format("BEGIN {0}({1}); END;", procedureName, string.Join(",", parameters.Select((x, i) => string.Format(":{0}", x.ParameterName))));
        Database.ExecuteSqlRaw(query, parameters);
    }

    protected virtual void Proc(string procedureName, IList<DbParameter> procParams, params object[] parameterValues)
    {
        if(procParams.Count == 0)
            throw new ArgumentException($"Parameters have not been defined for the procedure {procedureName}");

        var query = string.Format("BEGIN {0}({1}); END;", procedureName, string.Join(",", procParams.Select((x, i) => string.Format(":{0}", x.ParameterName))));
        procParams.Where(x => x.Direction == ParameterDirection.Input)
            .ToList()
            .ForEach(x => x.Value = parameterValues[procParams.IndexOf(x)]);
        Database.ExecuteSqlRaw(query, procParams);
        procParams.Where(x => x.Direction == ParameterDirection.Output)
            .ToList()
            .ForEach(x => ((OutParam)parameterValues[procParams.IndexOf(x)]).Value = x.Value);
    }
    internal void ExecuteProc<TParams>(Procedure<TParams> procedure, TParams parameterValues)
    {
        ArgumentNullException.ThrowIfNull(procedure, nameof(procedure));

        if(procedure.Parameters.Count == 0)
            throw new ArgumentException("No parameters have been defined for the procedure");

        var dbParams = procedure.Parameters.Select(x => x.Item2).ToList();
        var query = string.Format("BEGIN {0}.{1}({2}); END;", procedure.Schema, procedure.Name, string.Join(",", dbParams.Select((x, i) => string.Format(":{0}", x.ParameterName))));

        var properties = typeof(TParams).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(k => k.Name, v => v);

        SetParamInValues(procedure.Parameters, parameterValues, properties);

        Database.ExecuteSqlRaw(query, dbParams);

        SetParamOutValues(procedure.Parameters, parameterValues, properties);
    }

    private static void SetParamInValues<TParams>(IEnumerable<(string, DbParameter)> parameters, TParams paramRef, Dictionary<string, PropertyInfo> properties)
    {
        parameters.Where(x => x.Item2.Direction == ParameterDirection.Input)
            .ToList()
            .ForEach(x => x.Item2.Value = properties?[x.Item1].GetValue(paramRef));
    }

    private static void SetParamOutValues<TParams>(IEnumerable<(string, DbParameter)> parameters, TParams paramRef, Dictionary<string, PropertyInfo> properties)
    {
        parameters.Where(x => x.Item2.Direction == ParameterDirection.Output)
            .ToList()
            .ForEach(x => properties?[x.Item1].SetValue(paramRef, x.Item2.Value));
    }


    protected abstract void OnProcedureCall(ProcedureBuilder procedureBuilder);
    
}
