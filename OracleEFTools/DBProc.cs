namespace OracleEFTools;

public class DBProc<TParams>
{
    private readonly Action<Procedure<TParams>> _buildProcedureAction;
    private readonly Procedure<TParams> _procedure;

    public DBProc(Action<Procedure<TParams>> buildProcedureAction, Procedure<TParams> procedure)
    {
        _buildProcedureAction = buildProcedureAction;
        _procedure = procedure;
    }

    public void Execute(TParams parameters)
    {
        _buildProcedureAction(_procedure);
        _procedure.Execute(parameters);
    }
}
