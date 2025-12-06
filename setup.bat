@echo off
echo Installing frontend dependencies...
cd approoveit-client
call npm install
echo.
echo Dependencies installed successfully!
echo.
echo Next steps:
echo 1. Set up PostgreSQL database
echo 2. Update connection string in approoveit-api\appsettings.json
echo 3. Run migrations: cd approoveit-api ^&^& dotnet ef database update
echo 4. Start backend: cd approoveit-api ^&^& dotnet run
echo 5. Start frontend: cd approoveit-client ^&^& npm run dev
pause
