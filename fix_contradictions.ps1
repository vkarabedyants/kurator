# PowerShell скрипт для исправления противоречий в final.md

$filePath = "C:\code\KURATOR\final.md"
$content = Get-Content $filePath -Raw

Write-Host "Исправление противоречий в final.md..." -ForegroundColor Green

# 1. Удалить BackupCurator из ENUM
$content = $content -replace "CREATE TYPE user_role AS ENUM \('Admin', 'Curator', 'BackupCurator', 'ThreatAnalyst'\);", "CREATE TYPE user_role AS ENUM ('Admin', 'Curator', 'ThreatAnalyst');
CREATE TYPE curator_type AS ENUM ('Primary', 'Backup');
CREATE TYPE action_type AS ENUM ('CREATE', 'UPDATE', 'DELETE', 'STATUS_CHANGE');"

# Удалить influence_status и influence_type ENUMs
$content = $content -replace "CREATE TYPE influence_status AS ENUM \('A', 'B', 'C', 'D'\);\r?\n", ""
$content = $content -replace "CREATE TYPE influence_type AS ENUM \('Navigational', 'Interpretational', 'Functional', 'Reputational', 'Analytical'\);\r?\n", ""

Write-Host "✓ Исправлено: удален BackupCurator из ENUM, убраны influence ENUMs" -ForegroundColor Cyan

# 3. Удалить дубликаты сущностей
$content = $content -replace "- ``Watchlist`` - реестр потенциальных угроз\r?\n- ``WatchlistHistory`` - история изменения уровня риска и статусов в Watchlist\r?\n- ``FAQ`` - FAQ/правила\r?\n- ``ReferenceValue`` - справочники\r?\n- ``Watchlist`` - реестр угроз\r?\n- ``FAQ`` - FAQ/правила\r?\n- ``ReferenceValue`` - справочники", "- ``Watchlist`` - реестр потенциальных угроз`n- ``WatchlistHistory`` - история изменения уровня риска и статусов в Watchlist`n- ``FAQ`` - FAQ/правила`n- ``ReferenceValue`` - справочники"

Write-Host "✓ Исправлено: удалены дубликаты сущностей" -ForegroundColor Cyan

# 4. Заменить DELETE на deactivate в API
$content = $content -replace "DELETE /{id}\s+# Для blocks", "PUT    /{id}/archive     # Архивация блока"
$content = $content -replace "DELETE /{id}\s+# Для contacts", "PUT    /{id}/deactivate  # Деактивация контакта"
$content = $content -replace "DELETE /{id}\s+# Для interactions", "PUT    /{id}/deactivate  # Деактивация касания"
$content = $content -replace "DELETE /{id}\s+# Для watchlist", "PUT    /{id}/deactivate  # Деактивация записи"
$content = $content -replace "DELETE /{id}\s+# Только админ", "PUT    /{id}/deactivate  # Деактивация FAQ (только админ)"

Write-Host "✓ Исправлено: заменены DELETE на deactivate в API" -ForegroundColor Cyan

# 6. Добавить информацию о MFA при первом входе
$mfaText = @"

**Настройка MFA при первом входе:**
1. Администратор создает пользователя с флагом is_first_login = true
2. Пользователь входит с временным паролем (MFA еще не требуется)
3. Система определяет первый вход и показывает:
   - Форму смены пароля
   - QR-код для настройки MFA
4. Пользователь сканирует QR-код в приложении (Google Authenticator, Authy)
5. Пользователь вводит код из приложения для подтверждения
6. Система сохраняет MFA секрет и устанавливает is_first_login = false
7. С этого момента вход требует логин + пароль + MFA код

**Важно:** После первого входа и настройки MFA, все последующие входы требуют двухфакторную аутентификацию.
"@

$content = $content -replace "\*\*Важно:\*\*\r?\n- Пользователи не могут регистрироваться самостоятельно", "$mfaText`n`n**Важно:**`n- Пользователи не могут регистрироваться самостоятельно"

Write-Host "✓ Исправлено: добавлена логика MFA при первом входе" -ForegroundColor Cyan

# 7. Явно указать что Watchlist не шифруется
$watchlistNote = "`n`n**ВАЖНО:** Данные в Watchlist НЕ шифруются и хранятся в открытом виде в базе данных. Доступ к Watchlist имеют только администраторы и аналитики угроз (доверенные лица безопасности)."

$content = $content -replace "(\*\*Ответственный наблюдатель \(watch_owner\)\*\* \| Назначенный сотрудник)", "`$1$watchlistNote"

Write-Host "✓ Исправлено: явно указано что Watchlist не шифруется" -ForegroundColor Cyan

# 8. Удалить поле видимости из FAQ
$content = $content -replace "   - Видимость: ""Только кураторы""\r?\n", ""

Write-Host "✓ Исправлено: удалено поле видимости из FAQ" -ForegroundColor Cyan

# 9. Заменить "удаление" на "деактивация" везде
$content = $content -replace "удаляет контакты и касания", "деактивирует контакты и касания"
$content = $content -replace "\*\*удаляет записи\*\*", "**деактивирует записи**"
$content = $content -replace "не может удалять", "не может деактивировать"
$content = $content -replace "удаление доступно только", "деактивация доступна только"
$content = $content -replace "мягкое удаление", "деактивация"

Write-Host "✓ Исправлено: заменено 'удаление' на 'деактивация' везде" -ForegroundColor Cyan

# 11. Явно указать права доступа к Watchlist
$watchlistAccess = @"

**Права доступа к Watchlist:**
- **Администратор:** Полный доступ (создание, чтение, редактирование, деактивация)
- **Аналитик угроз:** Создание и редактирование записей, просмотр
- **Куратор:** Нет доступа к Watchlist
"@

$content = $content -replace "(\| \*\*История изменений\*\* \| Как в обычных контактах, включая логи всех правок \|)", "`$1$watchlistAccess"

Write-Host "✓ Исправлено: явно указаны права доступа к Watchlist" -ForegroundColor Cyan

# 12. Установить лимиты на файлы
$content = $content -replace "- Максимальный размер файла: настраивается администратором \(рекомендуется до 10 МБ\)", "- Максимальный размер файла: 10 МБ"
$content = $content -replace "- Количество файлов на одно касание: \*\*неограниченно\*\* \(но рекомендуется ограничить разумным количеством через настройки\)", "- Максимальное количество файлов на касание: 10"

Write-Host "✓ Исправлено: установлены лимиты на файлы (10 файлов, 10 МБ)" -ForegroundColor Cyan

# 13. Добавить block_id в таблицу Contacts
$contactsTable = @"
**Таблица "Contacts"**

| Поле | Описание |
|------|----------|
| **ID контакта** | Автогенерация по шаблону ``BLOCKCODE-###`` |
| **Блок (block_id)** | Внешний ключ на таблицу Blocks (обязательное поле) |
| **ФИО** | Зашифровано |
"@

$content = $content -replace "\*\*Таблица ""Contacts""\*\*\r?\n\r?\n\| Поле \| Описание \|\r?\n\|------\|----------\|\r?\n\| \*\*ID контакта\*\* \| Автогенерация по шаблону ``BLOCKCODE-###``[^\n]+\r?\n\| \*\*Блок\*\* \| Выбор блока[^\n]+\r?\n\| \*\*ФИО\*\* \| Данные шифруются", $contactsTable

Write-Host "✓ Исправлено: добавлен block_id в таблицу Contacts" -ForegroundColor Cyan

# 14. Заменить TIMESTAMP на TIMESTAMP WITH TIME ZONE
$content = $content -replace "TIMESTAMP DEFAULT CURRENT_TIMESTAMP", "TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP"
$content = $content -replace "TIMESTAMP NOT NULL", "TIMESTAMP WITH TIME ZONE NOT NULL"
$content = $content -replace "\| \*\*Время действия \(timestamp\)\*\* \| Автоматически \|", "| **Время действия (timestamp)** | TIMESTAMP WITH TIME ZONE - автоматически |"

Write-Host "✓ Исправлено: TIMESTAMP заменены на TIMESTAMP WITH TIME ZONE" -ForegroundColor Cyan

# 15. Указать что для CREATE операций old_values = NULL
$auditNote = "`n`n**Примечание:** Для операций CREATE поле old_values = NULL (так как старых значений не существует)."

$content = $content -replace "(\*\*Важно:\*\*\r?\n- Старые и новые значения сохраняются)", "$auditNote`n`n`$1"

Write-Host "✓ Исправлено: указано что для CREATE old_values = NULL" -ForegroundColor Cyan

# Сохранить исправленный файл
$content | Set-Content $filePath -Encoding UTF8

Write-Host "`n✅ Все исправления применены успешно!" -ForegroundColor Green
Write-Host "Файл сохранен: $filePath" -ForegroundColor Yellow
