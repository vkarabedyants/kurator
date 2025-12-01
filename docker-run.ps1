# Скрипт для сборки и запуска проекта Kurator в Docker

Write-Host "=== Kurator Docker Setup ===" -ForegroundColor Cyan

# Проверка Docker
Write-Host "`nПроверка Docker..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "Docker доступен" -ForegroundColor Green
} catch {
    Write-Host "ОШИБКА: Docker Desktop не запущен!" -ForegroundColor Red
    Write-Host "Пожалуйста, запустите Docker Desktop и повторите попытку." -ForegroundColor Yellow
    exit 1
}

# Сборка образов
Write-Host "`nСборка Docker образов..." -ForegroundColor Yellow
docker-compose build

if ($LASTEXITCODE -ne 0) {
    Write-Host "ОШИБКА при сборке образов!" -ForegroundColor Red
    exit 1
}

Write-Host "Образы успешно собраны" -ForegroundColor Green

# Запуск контейнеров
Write-Host "`nЗапуск контейнеров..." -ForegroundColor Yellow
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "ОШИБКА при запуске контейнеров!" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Проект успешно запущен! ===" -ForegroundColor Green
Write-Host "`nДоступные сервисы:" -ForegroundColor Cyan
Write-Host "  - Frontend:  http://localhost:3000" -ForegroundColor White
Write-Host "  - Backend:   http://localhost:5000" -ForegroundColor White
Write-Host "  - Swagger:   http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  - Database:  localhost:5432" -ForegroundColor White

Write-Host "`nПолезные команды:" -ForegroundColor Cyan
Write-Host "  - Просмотр логов:     docker-compose logs -f" -ForegroundColor White
Write-Host "  - Остановка:          docker-compose down" -ForegroundColor White
Write-Host "  - Перезапуск:         docker-compose restart" -ForegroundColor White
Write-Host "  - Статус:             docker-compose ps" -ForegroundColor White

