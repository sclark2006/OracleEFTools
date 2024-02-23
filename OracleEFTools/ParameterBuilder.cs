using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace OracleEFTools;

public sealed class ParameterBuilder<TParams>
{
    private string _parameterName;
    private readonly string _attributeName;
    private  DbParameter _dbParameter;

    private readonly Procedure<TParams> _ownerProcedure;

    internal ParameterBuilder(Procedure<TParams> ownerProcedure, string attributeName)
    {
        _attributeName = attributeName;
        _ownerProcedure = ownerProcedure;
    }

    public ParameterBuilder<TParams> HasName(string parameterName)
    {
        _parameterName = parameterName;
        return this;
    }

    public ParameterBuilder<TParams> In(OracleDbType dbType)
    {
        _dbParameter = new OracleParameter(_parameterName ?? _attributeName, dbType, ParameterDirection.Input);
        _ownerProcedure.AddParameter((_attributeName,_dbParameter));
        return this;
    }
    public ParameterBuilder<TParams> In(string udtTypeName)
    {
        _dbParameter = new OracleParameter(_parameterName ?? _attributeName, OracleDbType.Object, ParameterDirection.Input) { UdtTypeName = udtTypeName };
        _ownerProcedure.AddParameter((_attributeName, _dbParameter));
        return this;
    }

    public ParameterBuilder<TParams> Out(OracleDbType dbType)
    {
        _dbParameter = new OracleParameter(_parameterName ?? _attributeName, dbType, ParameterDirection.Output);
        _ownerProcedure.AddParameter((_attributeName, _dbParameter));
        return this;
    }

    public ParameterBuilder<TParams> Out(string udtTypeName)
    {
        _dbParameter = new OracleParameter(_parameterName ?? _attributeName, OracleDbType.Object, ParameterDirection.Output) { UdtTypeName = udtTypeName };
        _ownerProcedure.AddParameter((_attributeName, _dbParameter));
        return this;
    }

    public ParameterBuilder<TParams> Out<TCustomObject>() where TCustomObject : INullable, IOracleCustomType, new()
    {
        var customObjectType = typeof(TCustomObject);
        var mappingAttribute = customObjectType.GetCustomAttribute<OracleCustomTypeMappingAttribute>() ??
            throw new ArgumentException($"The specificied type {customObjectType.Name} doesn't have the {nameof(OracleCustomTypeMappingAttribute)} attribute");

        return Out(mappingAttribute.UdtTypeName);
    }
}
