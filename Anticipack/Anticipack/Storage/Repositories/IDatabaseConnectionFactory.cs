using SQLite;

namespace Anticipack.Storage.Repositories;

/// <summary>
/// Abstraction for SQLite database connection (DIP: Depend on abstractions, not concretions)
/// </summary>
public interface IDatabaseConnectionFactory
{
    SQLiteAsyncConnection CreateConnection();
    Task InitializeTablesAsync(SQLiteAsyncConnection connection);
}

/// <summary>
/// SQLite implementation of database connection factory
/// </summary>
public class SqliteDatabaseConnectionFactory : IDatabaseConnectionFactory
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _connection;

    public SqliteDatabaseConnectionFactory(string dbPath)
    {
        _dbPath = dbPath;
    }

    public SQLiteAsyncConnection CreateConnection()
    {
        return _connection ??= new SQLiteAsyncConnection(_dbPath);
    }

    public async Task InitializeTablesAsync(SQLiteAsyncConnection connection)
    {
        await connection.CreateTablesAsync(CreateFlags.None, 
            typeof(PackingItem), 
            typeof(PackingActivity), 
            typeof(PackingHistoryEntry));
    }
}
