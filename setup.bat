@echo off
echo Installing frontend dependencies...
cd thumbs-up-client
call npm install
echo.
echo Dependencies installed successfully!
echo.
echo Next steps:
echo 1. Set up PostgreSQL database
echo 2. Update connection string in ThumbsUpApi\appsettings.json
echo 3. Run migrations: cd ThumbsUpApi ^&^& dotnet ef database update
echo 4. Start backend: cd ThumbsUpApi ^&^& dotnet run
echo 5. Start frontend: cd thumbs-up-client ^&^& npm run dev
pause
