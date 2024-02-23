
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace OracleEFTools;

//[DbConfigurationType(typeof(OracleDbConfiguration))]
public abstract class OracleDbContext : DbContext
{
    public OracleDbContext()
    {
        _proceduresBuilder = new ProcedureBuilder(this);
        OnProcedureCall(_proceduresBuilder);
    }

    private readonly ProcedureBuilder _proceduresBuilder;

    internal void Proc<TParams>(Procedure<TParams> procedure, params object[] values)
    {
        throw new NotImplementedException();
    }

    protected virtual void Proc(string procedureName, params DbParameter[] parameters)
    {
        var query = string.Format("BEGIN {0}({1}); END;", procedureName, string.Join(",", parameters.Select((x, i) => string.Format(":{0}", x.ParameterName))));
        Database.ExecuteSqlRaw(query, parameters);
    }

    protected virtual void Proc(string procedureName, IList<DbParameter> procParams, params object[] parameterValues)
    {
        if(procParams.Count == 0)
            throw new ArgumentException($"No se han definido los parametros para el procedimiento {procedureName}");

        var query = string.Format("BEGIN {0}({1}); END;", procedureName, string.Join(",", procParams.Select((x, i) => string.Format(":{0}", x.ParameterName))));
        procParams.Where(x => x.Direction == ParameterDirection.Input)
            .ToList()
            .ForEach(x => x.Value = parameterValues[procParams.IndexOf(x)]);
        Database.ExecuteSqlRaw(query, procParams);
        procParams.Where(x => x.Direction == ParameterDirection.Output)
            .ToList()
            .ForEach(x => ((OutParam)parameterValues[procParams.IndexOf(x)]).Value = x.Value);
    }


    protected abstract void OnProcedureCall(ProcedureBuilder procedureBuilder);
    
}
