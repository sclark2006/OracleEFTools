namespace OracleEFTools;

public class DBProc<TParams>
{
    private readonly Action<Procedure<TParams>> _buildProcedureAction;
    private readonly Procedure<TParams> _procedure;
    private readonly OracleDbContext _dbContext;

    internal DBProc(OracleDbContext dbContext, Action<Procedure<TParams>> buildProcedureAction, Procedure<TParams> procedure)
    {
        _dbContext = dbContext;
        _buildProcedureAction = buildProcedureAction;
        _procedure = procedure;
    }

    public void Execute(TParams parameters)
    {
        _buildProcedureAction(_procedure);
        _dbContext.ExecuteProc(_procedure, parameters);
    }
}
