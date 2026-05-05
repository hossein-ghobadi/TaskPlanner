# PowerShell script to add ParentTaskId column to BoardTasks table
# This script reads the connection string from environment and runs the SQL

# Load environment variables from .env file (if exists)
if (Test-Path "..\.env") {
    Get-Content "..\.env" | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]*)\s*=\s*(.*)$') {
            $name = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
}

$connectionString = [Environment]::GetEnvironmentVariable("CONNECTION_MVPTest")

if ([string]::IsNullOrEmpty($connectionString)) {
    Write-Host "Error: CONNECTION_MVPTest environment variable not found!" -ForegroundColor Red
    Write-Host "Please set it or ensure .env file exists in the project root." -ForegroundColor Yellow
    exit 1
}

Write-Host "Connection string found. Adding ParentTaskId column to BoardTasks table..." -ForegroundColor Green

# SQL script
$sqlScript = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BoardTasks]') AND name = 'ParentTaskId')
BEGIN
    ALTER TABLE [BoardTasks]
    ADD [ParentTaskId] int NULL;

    CREATE INDEX [IX_BoardTasks_ParentTaskId] ON [BoardTasks] ([ParentTaskId]);

    ALTER TABLE [BoardTasks]
    ADD CONSTRAINT [FK_BoardTasks_BoardTasks_ParentTaskId] 
    FOREIGN KEY ([ParentTaskId]) REFERENCES [BoardTasks] ([Id]) ON DELETE NO ACTION;

    PRINT 'ParentTaskId column added successfully to BoardTasks table.';
END
ELSE
BEGIN
    PRINT 'ParentTaskId column already exists in BoardTasks table.';
END
"@

try {
    # Try to use sqlcmd if available
    $sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($sqlcmdPath) {
        Write-Host "Using sqlcmd to execute SQL..." -ForegroundColor Cyan
        # Extract server and database from connection string
        $server = $null
        $database = $null
        $integratedSecurity = $false
        
        if ($connectionString -match "Server=([^;]+)") {
            $server = $matches[1]
        }
        if ($connectionString -match "Database=([^;]+)") {
            $database = $matches[1]
        }
        if ($connectionString -match "Integrated Security=([^;]+)") {
            $integratedSecurity = $matches[1] -eq "True"
        }
        
        if ($server -and $database) {
            $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
            $sqlScript | Out-File -FilePath $tempFile -Encoding UTF8
            
            if ($integratedSecurity) {
                & sqlcmd -S $server -d $database -i $tempFile -E
            } else {
                # Extract user and password if needed
                $userId = $null
                $password = $null
                if ($connectionString -match "User ID=([^;]+)") {
                    $userId = $matches[1]
                }
                if ($connectionString -match "Password=([^;]+)") {
                    $password = $matches[1]
                }
                if ($userId -and $password) {
                    & sqlcmd -S $server -d $database -U $userId -P $password -i $tempFile
                } else {
                    Write-Host "Could not extract credentials from connection string. Please run the SQL script manually." -ForegroundColor Yellow
                    Write-Host "SQL Script location: AddParentTaskIdToBoardTasks.sql" -ForegroundColor Yellow
                }
            }
            
            Remove-Item $tempFile -ErrorAction SilentlyContinue
            Write-Host "Migration completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Could not parse connection string. Please run the SQL script manually." -ForegroundColor Yellow
            Write-Host "SQL Script location: AddParentTaskIdToBoardTasks.sql" -ForegroundColor Yellow
        }
    } else {
        Write-Host "sqlcmd not found. Please run the SQL script manually in SQL Server Management Studio." -ForegroundColor Yellow
        Write-Host "SQL Script location: AddParentTaskIdToBoardTasks.sql" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "Please run the SQL script manually in SQL Server Management Studio." -ForegroundColor Yellow
    Write-Host "SQL Script location: AddParentTaskIdToBoardTasks.sql" -ForegroundColor Yellow
}

