using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace OracleEFTools;

public abstract class OracleBaseUdt : IOracleCustomType, INullable, IOracleCustomTypeFactory
{
    protected bool _isNull;

    private IEnumerable<PropertyInfo> GetTypeProperties() => GetType()
                   .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.GetCustomAttribute<OracleObjectMappingAttribute>() != null);


    [JsonIgnore]
    public bool IsNull => _isNull;

    public IOracleCustomType CreateObject()
    {

        var result = (IOracleCustomType)Activator.CreateInstance(GetType());
        return result;
    }

    public virtual void FromCustomObject(OracleConnection con, object udt)
    {

        foreach (var prop in GetTypeProperties())
        {
            var attr = prop.GetCustomAttribute<OracleObjectMappingAttribute>();
            OracleUdt.SetValue(con, udt, attr.AttributeName, prop.GetValue(this));
        }
    }

    public virtual void ToCustomObject(OracleConnection con, object udt)
    {
        foreach (var prop in GetTypeProperties())
        {
            var attr = prop.GetCustomAttribute<OracleObjectMappingAttribute>();
            prop.SetValue(this, OracleUdt.GetValue(con, udt, attr.AttributeName));
        }
    }

    public override string ToString()
    {
        var typeName = GetType().Name;

        if (_isNull)
            return $"{typeName}.Null";
        else
            return $"{typeName}: {JsonSerializer.Serialize(this, GetType())}";
    }


}

public abstract class BaseUdt<T> : OracleBaseUdt where T : BaseUdt<T>, new()
{
    public static T Null => new()
    {
        _isNull = true
    };
}

public abstract class BaseCollectionUdt<T> : OracleBaseUdt, IOracleArrayTypeFactory where T : BaseUdt<T>, new()
{
    [JsonIgnore]
    public OracleUdtStatus[] StatusArray { get; set; }
    private PropertyInfo GetProperty() => GetType()
               .GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .FirstOrDefault(p => p.GetCustomAttribute<OracleArrayMappingAttribute>() != null);

    public override void FromCustomObject(OracleConnection con, object udt)
    {
        var prop = GetProperty() ?? throw new KeyNotFoundException("No se ha definido una campo o propiedad de tipo arreglo con el atributo OracleArrayMapping");
        OracleUdt.SetValue(con, udt, 0, prop.GetValue(this));

    }
    public override void ToCustomObject(OracleConnection con, object udt)
    {
        var prop = GetProperty() ?? throw new KeyNotFoundException("No se ha definido una campo o propiedad de tipo arreglo con el atributo OracleArrayMapping");
        prop.SetValue(this, (T[])OracleUdt.GetValue(con, udt, 0, out object objectStatusArray));
        StatusArray = (OracleUdtStatus[])objectStatusArray;
    }

    public Array CreateArray(int numElems)
    {
        return new T[numElems];
    }

    public Array CreateStatusArray(int numElems)
    {
        return new OracleUdtStatus[numElems];
    }
}