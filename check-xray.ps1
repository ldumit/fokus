$dll = 'D:/src/fokus/src/Services/Fokus/Fokus.API/bin/Debug/net10.0/Microsoft.Data.Sqlite.dll'
[System.Reflection.Assembly]::LoadFrom($dll) | Out-Null
$conn = [Microsoft.Data.Sqlite.SqliteConnection]::new('Data Source=D:/src/fokus/src/Services/Fokus/Fokus.API/fokus.db')
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = 'SELECT COUNT(*) FROM TestExecutions'
$te = $cmd.ExecuteScalar()
$cmd.CommandText = 'SELECT COUNT(*) FROM TestRuns'
$tr = $cmd.ExecuteScalar()
$cmd.CommandText = 'SELECT COUNT(*) FROM TestSets'
$ts = $cmd.ExecuteScalar()
$conn.Close()
Write-Host "TestExecutions: $te | TestRuns: $tr | TestSets: $ts"
