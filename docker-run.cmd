@echo off
REM Скрипт для сборки и запуска проекта Kurator в Docker

echo === Kurator Docker Setup ===

REM Проверка Docker
echo.
echo Проверка Docker...
docker ps >nul 2>&1
if %errorlevel% neq 0 (
    echo ОШИБКА: Docker Desktop не запущен!
    echo Пожалуйста, запустите Docker Desktop и повторите попытку.
    exit /b 1
)
echo Docker доступен

REM Сборка образов
echo.
echo Сборка Docker образов...
docker-compose build
if %errorlevel% neq 0 (
    echo ОШИБКА при сборке образов!
    exit /b 1
)
echo Образы успешно собраны

REM Запуск контейнеров
echo.
echo Запуск контейнеров...
docker-compose up -d
if %errorlevel% neq 0 (
    echo ОШИБКА при запуске контейнеров!
    exit /b 1
)

echo.
echo === Проект успешно запущен! ===
echo.
echo Доступные сервисы:
echo   - Frontend:  http://localhost:3000
echo   - Backend:   http://localhost:5000
echo   - Swagger:   http://localhost:5000/swagger
echo   - Database:  localhost:5432
echo.
echo Полезные команды:
echo   - Просмотр логов:     docker-compose logs -f
echo   - Остановка:          docker-compose down
echo   - Перезапуск:         docker-compose restart
echo   - Статус:             docker-compose ps

