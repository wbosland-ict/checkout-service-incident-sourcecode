using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ShopFast.Checkout.Tests.Fixtures;

public class QueryCountingInterceptor : DbCommandInterceptor
{
    public int QueryCount { get; private set; }

    public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
        System.Data.Common.DbCommand command,
        CommandEventData eventData,
        InterceptionResult<System.Data.Common.DbDataReader> result)
    {
        QueryCount++;
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
        System.Data.Common.DbCommand command,
        CommandEventData eventData,
        InterceptionResult<System.Data.Common.DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}

public static class TestDbContextFactory
{
    public static (CheckoutDbContext DbContext, SqliteConnection Connection) CreateContext(
        QueryCountingInterceptor? interceptor = null)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using (var schemaContext = new CheckoutDbContext(
            new DbContextOptionsBuilder<CheckoutDbContext>().UseSqlite(connection).Options))
        {
            schemaContext.Database.EnsureCreated();
        }

        var dbContext = CreateContextOnConnection(connection, interceptor);

        return (dbContext, connection);
    }

    public static CheckoutDbContext CreateContextOnConnection(
        SqliteConnection connection,
        QueryCountingInterceptor? interceptor = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CheckoutDbContext>()
            .UseSqlite(connection)
            .UseLazyLoadingProxies();

        if (interceptor is not null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        return new CheckoutDbContext(optionsBuilder.Options);
    }
}
