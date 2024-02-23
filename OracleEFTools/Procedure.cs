using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace OracleEFTools;

public class Procedure<TParams>
{
    private readonly List<(string,DbParameter)> _parameters = new();
    private readonly OracleDbContext _dbContext;
    internal Procedure(OracleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Procedure<TParams> ToProcedure(string procedureName, string schemaName)
    {
        Name = procedureName;
        Schema = schemaName;
        return this;
    }

    public string Name { get; private set; }
    public string Schema { get; private set; }
    public IReadOnlyList<(string,DbParameter)> Parameters => _parameters;

    public ParameterBuilder<TParams> Parameter<TParamType>(Expression<Func<TParams, TParamType>> parameterExpression)
    {
        if (parameterExpression is null || parameterExpression.GetLambdaOrNull() is null)
            throw new ArgumentNullException(nameof(parameterExpression));

        var memberAccess = parameterExpression.GetLambdaOrNull().GetMemberAccess();
        var parameterBuilder = new ParameterBuilder<TParams>(this, memberAccess.Name);

        return parameterBuilder;
    }

    internal void AddParameter((string, DbParameter) parameter)
    {
        _parameters.Add(parameter);
    }

    internal void Execute(TParams? paramsObject)
    {
        ArgumentNullException.ThrowIfNull(paramsObject, nameof(paramsObject));

        if(Parameters.Count == 0)
            throw new InvalidOperationException($"No se han definido los parametros para el procedimiento {Name}");

        var dbParams = Parameters.Select(x => x.Item2).ToList();
        var query = string.Format("BEGIN {0}.{1}({2}); END;", Schema, Name, string.Join(",", dbParams.Select((x, i) => string.Format(":{0}", x.ParameterName))));

        var properties = paramsObject?.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(k => k.Name, v => v);

        SetParamInValues(_parameters, paramsObject, properties);
 
        _dbContext.Database.ExecuteSqlRaw(query, dbParams);

        SetParamOutValues(_parameters, paramsObject, properties);
    }

    private static void SetParamInValues(List<(string, DbParameter)> parameters, TParams paramRef, Dictionary<string, PropertyInfo> properties)
    {
        parameters.Where(x => x.Item2.Direction == ParameterDirection.Input)
            .ToList()
            .ForEach(x => x.Item2.Value = properties?[x.Item1].GetValue(paramRef));
    }

    private static void SetParamOutValues(List<(string, DbParameter)> parameters, TParams paramRef, Dictionary<string, PropertyInfo> properties)
    {
        parameters.Where(x => x.Item2.Direction == ParameterDirection.Output)
            .ToList()
            .ForEach(x => properties?[x.Item1].SetValue(paramRef, x.Item2.Value));
    }

}
