# Script to add UpdatedAt column to CampingTips table
# This script will run the SQL command directly

$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=ShopDB;Trusted_Connection=True;MultipleActiveResultSets=true"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CampingTips]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[CampingTips]
    ADD [UpdatedAt] datetime2 NULL;
    
    SELECT 'Column UpdatedAt added successfully to CampingTips table.' AS Result;
END
ELSE
BEGIN
    SELECT 'Column UpdatedAt already exists in CampingTips table.' AS Result;
END
"@
    
    $result = $command.ExecuteScalar()
    Write-Host $result -ForegroundColor Green
    
    $connection.Close()
    Write-Host "Migration completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}

