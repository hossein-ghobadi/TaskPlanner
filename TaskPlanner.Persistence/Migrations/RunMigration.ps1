# PowerShell script to create UserProjectFolders table
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

Write-Host "Connection string found. Creating UserProjectFolders table..." -ForegroundColor Green

# SQL script
$sqlScript = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserProjectFolders]') AND type in (N'U'))
BEGIN
    CREATE TABLE [UserProjectFolders] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserId] nvarchar(450) NOT NULL,
        [ProjectId] int NOT NULL,
        [FolderId] int NULL,
        [Order] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserProjectFolders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserProjectFolders_ProjectFolders_FolderId] FOREIGN KEY ([FolderId]) REFERENCES [ProjectFolders] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_UserProjectFolders_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_UserProjectFolders_FolderId] ON [UserProjectFolders] ([FolderId]);
    CREATE INDEX [IX_UserProjectFolders_ProjectId] ON [UserProjectFolders] ([ProjectId]);
    CREATE UNIQUE INDEX [IX_UserProjectFolders_UserId_ProjectId] ON [UserProjectFolders] ([UserId], [ProjectId]);
    
    PRINT 'UserProjectFolders table created successfully!';
END
ELSE
BEGIN
    PRINT 'UserProjectFolders table already exists.';
END
"@

try {
    # Try to use sqlcmd if available
    $sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($sqlcmdPath) {
        Write-Host "Using sqlcmd to execute SQL..." -ForegroundColor Cyan
        # Extract server and database from connection string
        if ($connectionString -match "Server=([^;]+)") {
            $server = $matches[1]
        }
        if ($connectionString -match "Database=([^;]+)") {
            $database = $matches[1]
        }
        
        if ($server -and $database) {
            $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
            $sqlScript | Out-File -FilePath $tempFile -Encoding UTF8
            & sqlcmd -S $server -d $database -i $tempFile -E
            Remove-Item $tempFile
            Write-Host "Table created successfully!" -ForegroundColor Green
        } else {
            Write-Host "Could not parse connection string. Please run the SQL script manually." -ForegroundColor Yellow
            Write-Host "SQL Script saved to: CreateUserProjectFoldersTable.sql" -ForegroundColor Yellow
        }
    } else {
        Write-Host "sqlcmd not found. Please run the SQL script manually in SQL Server Management Studio." -ForegroundColor Yellow
        Write-Host "SQL Script location: CreateUserProjectFoldersTable.sql" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "Please run the SQL script manually in SQL Server Management Studio." -ForegroundColor Yellow
    Write-Host "SQL Script location: CreateUserProjectFoldersTable.sql" -ForegroundColor Yellow
}
















