using AdoKt07Booking.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AdoKt07Booking.Tests;

internal sealed class TestDbFixture : IAsyncDisposable
{
	private TestDbFixture(SqliteConnection connection, AppDbContext dbContext)
	{
		Connection = connection;
		DbContext = dbContext;
	}

	public AppDbContext DbContext { get; }

	private SqliteConnection Connection { get; }

	public static async Task<TestDbFixture> CreateAsync()
	{
		var connection = new SqliteConnection("Data Source=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(connection)
			.Options;

		var dbContext = new AppDbContext(options);
		await dbContext.Database.EnsureCreatedAsync();

		return new TestDbFixture(connection, dbContext);
	}

	public async ValueTask DisposeAsync()
	{
		await DbContext.DisposeAsync();
		await Connection.DisposeAsync();
	}
}
