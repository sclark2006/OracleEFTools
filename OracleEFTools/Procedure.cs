using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using System.Data.Common;
using System.Linq.Expressions;

namespace OracleEFTools;

public class Procedure<TParams>
{
    private readonly List<(string,DbParameter)> _parameters = new();

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
}
