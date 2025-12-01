# PowerShell script to setup and run the KURATOR application
# Prerequisites: .NET 9 SDK, Node.js 20+, PostgreSQL 16

param(
    [Parameter(Mandatory=$false)]
    [switch]$SkipDatabase,

    [Parameter(Mandatory=$false)]
    [switch]$SkipMigrations,

    [Parameter(Mandatory=$false)]
    [switch]$SkipFrontend,

    [Parameter(Mandatory=$false)]
    [switch]$RunOnly
)

# Color output functions
function Write-Success {
    Write-Host $args -ForegroundColor Green
}

function Write-Info {
    Write-Host $args -ForegroundColor Cyan
}

function Write-Warning {
    Write-Host $args -ForegroundColor Yellow
}

function Write-Error {
    Write-Host $args -ForegroundColor Red
}

# Check prerequisites
Write-Info "Checking prerequisites..."

# Check .NET SDK
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Error "ERROR: .NET SDK not found. Please install .NET 9 SDK from https://dotnet.microsoft.com/download"
    exit 1
} else {
    Write-Success "✓ .NET SDK found: $dotnetVersion"
}

# Check Node.js
$nodeVersion = node --version 2>$null
if (-not $nodeVersion) {
    Write-Error "ERROR: Node.js not found. Please install Node.js 20+ from https://nodejs.org/"
    exit 1
} else {
    Write-Success "✓ Node.js found: $nodeVersion"
}

# Check PostgreSQL
$pgVersion = psql --version 2>$null
if (-not $pgVersion -and -not $SkipDatabase) {
    Write-Warning "WARNING: PostgreSQL client not found. Make sure PostgreSQL 16 is installed and running."
}

# Load environment variables
if (Test-Path ".env") {
    Write-Info "Loading environment variables from .env file..."
    Get-Content ".env" | ForEach-Object {
        if ($_ -match '^([^#].+?)=(.+)$') {
            $name = $matches[1].Trim()
            $value = $matches[2].Trim()
            Set-Item -Path "env:$name" -Value $value
        }
    }
} else {
    Write-Warning "WARNING: .env file not found. Using default values."
    $env:DB_USER = "kurator_user"
    $env:DB_PASSWORD = "KuratorSecurePass2024!"
    $env:JWT_SECRET = "ThisIsAVerySecureJWTSecretKeyWithAtLeast32Characters2024!"
    $env:ENCRYPTION_KEY = "MyVerySecureEncryptionKey32BytesForAES256Encryption!"
}

# Database setup
if (-not $SkipDatabase) {
    Write-Info "`nSetting up database..."

    # Create database if it doesn't exist
    $dbName = "kurator"
    $dbUser = $env:DB_USER
    $dbPassword = $env:DB_PASSWORD

    Write-Info "Creating database and user (if not exists)..."

    # Create SQL script for database setup
    $sqlScript = @"
-- Check if database exists
SELECT 'CREATE DATABASE $dbName'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$dbName')\gexec

-- Connect to the database
\c $dbName

-- Create user if not exists
DO `$`$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_user WHERE usename = '$dbUser') THEN
        CREATE USER $dbUser WITH PASSWORD '$dbPassword';
    END IF;
END
`$`$;

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE $dbName TO $dbUser;
GRANT ALL ON SCHEMA public TO $dbUser;

-- Enable extensions
CREATE EXTENSION IF NOT EXISTS pgcrypto;
"@

    $sqlScript | Out-File -FilePath ".\database\setup.sql" -Encoding UTF8

    Write-Info "Please run the following command as PostgreSQL superuser:"
    Write-Warning "psql -U postgres -f .\database\setup.sql"
    Write-Info "Press Enter after you've created the database, or Ctrl+C to skip..."
    Read-Host
}

if (-not $RunOnly) {
    # Backend setup
    Write-Info "`nSetting up backend..."
    Set-Location ".\backend"

    # Restore dependencies
    Write-Info "Restoring backend dependencies..."
    dotnet restore

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to restore backend dependencies"
        exit 1
    }

    # Build backend
    Write-Info "Building backend..."
    dotnet build --configuration Release

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build backend"
        exit 1
    }

    # Apply migrations
    if (-not $SkipMigrations) {
        Write-Info "Applying database migrations..."

        # Install EF Core tools if not present
        $efVersion = dotnet ef --version 2>$null
        if (-not $efVersion) {
            Write-Info "Installing Entity Framework Core tools..."
            dotnet tool install --global dotnet-ef
        }

        # Create migration if it doesn't exist
        if (-not (Test-Path ".\Kurator.Infrastructure\Data\Migrations")) {
            Write-Info "Creating initial migration..."
            dotnet ef migrations add InitialCreate --project Kurator.Infrastructure --startup-project Kurator.Api
        }

        # Apply migrations
        dotnet ef database update --project Kurator.Infrastructure --startup-project Kurator.Api

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to apply migrations. Database might not be accessible."
        }
    }

    Set-Location ".."
}

if (-not $SkipFrontend -and -not $RunOnly) {
    # Frontend setup
    Write-Info "`nSetting up frontend..."
    Set-Location ".\frontend"

    # Install dependencies
    Write-Info "Installing frontend dependencies..."
    npm install

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install frontend dependencies"
        exit 1
    }

    # Build frontend
    Write-Info "Building frontend..."
    npm run build

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build frontend"
        exit 1
    }

    Set-Location ".."
}

# Start services
Write-Info "`nStarting services..."

# Start backend
Write-Info "Starting backend API on http://localhost:5000..."
$backendJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD\backend
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://localhost:5000"
    dotnet run --project Kurator.Api --no-build
}

# Wait for backend to start
Write-Info "Waiting for backend to start..."
Start-Sleep -Seconds 5

# Check if backend is running
$backendHealth = $null
try {
    $backendHealth = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get
    Write-Success "✓ Backend API is running"
} catch {
    Write-Warning "Backend API is not responding yet. It might take a few more seconds to start."
}

# Start frontend
Write-Info "Starting frontend on http://localhost:3000..."
$frontendJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD\frontend
    $env:NEXT_PUBLIC_API_URL = "http://localhost:5000/api"
    npm run dev
}

# Wait for frontend to start
Write-Info "Waiting for frontend to start..."
Start-Sleep -Seconds 5

Write-Success "`n✓ Setup complete!"
Write-Info "`nApplication URLs:"
Write-Info "  Frontend:    http://localhost:3000"
Write-Info "  Backend API: http://localhost:5000/api"
Write-Info "  Swagger UI:  http://localhost:5000/swagger"
Write-Info "`nDefault admin credentials:"
Write-Info "  Login:    admin"
Write-Info "  Password: Admin123!"
Write-Warning "`nIMPORTANT: Change the admin password and setup MFA on first login!"
Write-Info "`nPress Ctrl+C to stop all services..."

# Keep script running
try {
    while ($true) {
        Start-Sleep -Seconds 1
    }
} finally {
    Write-Info "Stopping services..."
    Stop-Job $backendJob -PassThru | Remove-Job
    Stop-Job $frontendJob -PassThru | Remove-Job
    Write-Success "Services stopped."
}