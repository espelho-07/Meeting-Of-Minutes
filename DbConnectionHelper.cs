using Microsoft.Extensions.Configuration;

namespace Meeting_Of_Minutes
{
    public static class DbConnectionHelper
    {
        private static readonly object SyncRoot = new();
        private static string? _connectionString;

        public static string ConnectionString
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_connectionString))
                {
                    return _connectionString;
                }

                lock (SyncRoot)
                {
                    if (!string.IsNullOrWhiteSpace(_connectionString))
                    {
                        return _connectionString;
                    }

                    string? envConnection = Environment.GetEnvironmentVariable("MOM_CONNECTION_STRING");
                    if (!string.IsNullOrWhiteSpace(envConnection))
                    {
                        _connectionString = envConnection;
                        return _connectionString;
                    }

                    IConfigurationRoot configuration = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: true)
                        .AddJsonFile("appsettings.Development.json", optional: true)
                        .AddEnvironmentVariables()
                        .Build();

                    _connectionString = configuration.GetConnectionString("DefaultConnection");
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        throw new InvalidOperationException("Database connection string is not configured. Set ConnectionStrings:DefaultConnection or MOM_CONNECTION_STRING.");
                    }

                    return _connectionString;
                }
            }
        }

        public static void Reset()
        {
            lock (SyncRoot)
            {
                _connectionString = null;
            }
        }
    }
}
