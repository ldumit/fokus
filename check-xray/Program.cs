using Microsoft.Data.Sqlite;

var conn = new SqliteConnection("Data Source=../src/Services/Fokus/Fokus.API/fokus.db");
conn.Open();

string[] tables = ["TestExecutions", "TestRuns", "TestSets", "TestExecutionLinks"];
foreach (var table in tables)
{
    var cmd = conn.CreateCommand();
    cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
    var count = cmd.ExecuteScalar();
    Console.WriteLine($"{table}: {count}");
}

conn.Close();
