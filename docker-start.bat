@echo off
echo Starting ApprooveIt application with Docker...
echo.

REM Check if Docker is running
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: Docker is not running. Please start Docker Desktop and try again.
    pause
    exit /b 1
)

echo Stopping any existing containers...
docker-compose down

echo Building and starting containers...
docker-compose up --build -d

echo.
echo Waiting for database to be ready...
timeout /t 10 /nobreak >nul

echo.
echo Checking container status...
docker-compose ps

echo.
echo ========================================
echo Application is starting!
echo API: http://localhost:5039
echo API Swagger: http://localhost:5039/swagger
echo Database: localhost:5432
echo ========================================
echo.
echo To view logs: docker-compose logs -f
echo To stop: docker-compose down
echo.
pause
