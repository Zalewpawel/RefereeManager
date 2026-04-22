using Microsoft.EntityFrameworkCore;
using Sedziowanie.Data;

namespace Sedziowanie.Tests.TestHelpers;

public static class DbContextFactory
{
    public static DBObsadyContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<DBObsadyContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new DBObsadyContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
