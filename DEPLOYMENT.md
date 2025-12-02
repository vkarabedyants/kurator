# Kurator - Инструкция по деплою на Ubuntu сервер

## Обзор

Этот документ описывает процесс развертывания Kurator на Ubuntu сервере с использованием GitHub Actions для CI/CD.

---

## БЫСТРЫЙ СТАРТ: Подключение новой машины за 10 минут

### Шаг 1: Подключитесь к серверу

```bash
ssh root@<IP_АДРЕС_СЕРВЕРА>
```

### Шаг 2: Выполните эти команды на сервере

```bash
# Обновление и установка Docker
apt update && apt upgrade -y
curl -fsSL https://get.docker.com | bash
apt install -y docker-compose-plugin git

# Создание директории
mkdir -p /opt/kurator
cd /opt/kurator

# Генерация SSH ключа для GitHub Actions
ssh-keygen -t ed25519 -f ~/.ssh/github_deploy -N ""
cat ~/.ssh/github_deploy.pub >> ~/.ssh/authorized_keys

# Покажет приватный ключ - СКОПИРУЙТЕ ЕГО!
echo "=== СКОПИРУЙТЕ ЭТОТ КЛЮЧ В GITHUB SECRETS (SSH_PRIVATE_KEY) ==="
cat ~/.ssh/github_deploy
echo "=== КОНЕЦ КЛЮЧА ==="
```

### Шаг 3: Добавьте секреты в GitHub

Перейдите: `https://github.com/vkarabedyants/kurator/settings/secrets/actions`

Добавьте:

| Имя секрета | Значение |
|-------------|----------|
| `SERVER_HOST` | IP адрес сервера (например: `192.168.1.100`) |
| `SERVER_USER` | `root` |
| `SSH_PRIVATE_KEY` | Приватный ключ из шага 2 (весь текст включая BEGIN и END) |
| `SERVER_PORT` | `22` |
| `APP_DIR` | `/opt/kurator` |

### Шаг 4: Запустите деплой

```bash
# На локальной машине - сделайте любой коммит
git commit --allow-empty -m "Trigger deploy" && git push
```

### Шаг 5: Проверьте

```bash
# На сервере
cd /opt/kurator
docker compose ps
docker compose logs -f
```

**Готово!** Сайт доступен на `http://<IP_АДРЕС_СЕРВЕРА>`

---

## Архитектура деплоя

```
GitHub Repository
       │
       ▼
GitHub Actions (Build & Test)
       │
       ▼
GitHub Container Registry (ghcr.io)
       │
       ▼
Ubuntu Server (Docker Compose)
   ├── kurator-frontend (Next.js)
   ├── kurator-api (.NET 9)
   └── kurator-db (PostgreSQL 16)
```

## Требования

### Сервер
- Ubuntu 20.04 LTS или выше
- Минимум 2 GB RAM
- 20 GB свободного места на диске
- Публичный IP адрес
- Открытые порты: 22 (SSH), 80, 443

### GitHub
- Репозиторий на GitHub
- GitHub Personal Access Token с правами `read:packages`

---

## Часть 1: Настройка сервера

### 1.1 Подключение к серверу

```bash
ssh root@YOUR_SERVER_IP
```

### 1.2 Автоматическая настройка (рекомендуется)

Скачайте и запустите скрипт настройки:

```bash
curl -sSL https://raw.githubusercontent.com/vkarabedyants/kurator/main/scripts/server-setup.sh | bash
```

### 1.3 Ручная настройка

Если предпочитаете ручную настройку:

```bash
# 1. Обновление системы
apt-get update && apt-get upgrade -y

# 2. Установка Docker
curl -fsSL https://get.docker.com | bash

# 3. Установка Docker Compose
apt-get install -y docker-compose-plugin

# 4. Создание директории приложения
mkdir -p /opt/kurator
cd /opt/kurator

# 5. Настройка файрвола
ufw enable
ufw allow ssh
ufw allow http
ufw allow https
```

### 1.4 Создание конфигурации

```bash
cd /opt/kurator

# Создайте .env файл
nano .env
```

Содержимое `.env`:

```env
# Database
DB_USER=kurator_user
DB_PASSWORD=ваш_сложный_пароль_для_бд

# JWT (сгенерируйте: openssl rand -base64 64)
JWT_SECRET=ваш_jwt_секрет_минимум_64_символа

# Encryption (сгенерируйте: openssl rand -base64 32)
ENCRYPTION_KEY=ваш_ключ_шифрования_32_символа

# GitHub
GITHUB_REPOSITORY=vkarabedyants/kurator

# URLs
API_URL=https://rocketcredit.com.ua/api
CORS_ORIGINS=https://rocketcredit.com.ua
```

### 1.5 Генерация секретов

```bash
# JWT Secret
openssl rand -base64 64

# Database Password
openssl rand -base64 32

# Encryption Key
openssl rand -base64 32
```

---

## Часть 2: Настройка GitHub

### 2.1 Создание Personal Access Token

1. Перейдите в GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Нажмите "Generate new token (classic)"
3. Выберите права:
   - `read:packages` - для скачивания Docker образов
   - `write:packages` - для публикации Docker образов
4. Сохраните токен!

### 2.2 Настройка Repository Secrets

Перейдите в репозиторий → Settings → Secrets and variables → Actions

Добавьте следующие секреты:

| Секрет | Описание | Пример |
|--------|----------|--------|
| `SERVER_HOST` | IP адрес или домен сервера | `192.168.1.100` |
| `SERVER_USER` | SSH пользователь | `root` или `ubuntu` |
| `SSH_PRIVATE_KEY` | Приватный SSH ключ | Содержимое `~/.ssh/id_rsa` |
| `SERVER_PORT` | SSH порт (опционально) | `22` |
| `APP_DIR` | Директория приложения | `/opt/kurator` |
| `API_URL` | URL API для фронтенда | `http://your-domain.com/api` |
| `HEALTH_CHECK_URL` | URL проверки здоровья | `http://localhost:5000/health` |

### 2.3 Настройка SSH ключа

На локальной машине:

```bash
# Генерация SSH ключа (если нет)
ssh-keygen -t ed25519 -C "github-actions"

# Скопируйте публичный ключ на сервер
ssh-copy-id root@YOUR_SERVER_IP

# Скопируйте приватный ключ для GitHub Secret
cat ~/.ssh/id_ed25519
```

---

## Часть 3: Деплой

### 3.1 Автоматический деплой

После настройки, каждый push в `main` ветку автоматически:

1. Запускает тесты (backend + frontend)
2. Собирает Docker образы
3. Публикует образы в GitHub Container Registry
4. Деплоит на сервер через SSH

### 3.2 Первый деплой (ручной)

На сервере:

```bash
cd /opt/kurator

# Логин в GitHub Container Registry
echo YOUR_GITHUB_TOKEN | docker login ghcr.io -u YOUR_USERNAME --password-stdin

# Скачивание образов
docker compose pull

# Запуск
docker compose up -d

# Проверка статуса
docker compose ps
docker compose logs -f
```

### 3.3 Проверка деплоя

```bash
# Статус контейнеров
docker compose ps

# Логи
docker compose logs -f backend
docker compose logs -f frontend

# Health check
curl http://localhost:5000/health
```

---

## Часть 4: SSL/HTTPS (опционально)

### 4.1 Использование Let's Encrypt

```bash
# Установка certbot
apt-get install -y certbot

# Получение сертификата
certbot certonly --standalone -d rocketcredit.com.ua -d www.rocketcredit.com.ua

# Копирование сертификатов
cp /etc/letsencrypt/live/rocketcredit.com.ua/fullchain.pem /opt/kurator/nginx/ssl/
cp /etc/letsencrypt/live/rocketcredit.com.ua/privkey.pem /opt/kurator/nginx/ssl/

# Запуск с nginx
COMPOSE_PROFILES=with-nginx docker compose up -d
```

### 4.2 Обновление сертификатов

Добавьте в crontab:

```bash
0 0 1 * * certbot renew && docker compose restart nginx
```

---

## Часть 5: Обслуживание

### 5.1 Резервное копирование

```bash
# Ручной бэкап
/opt/kurator/scripts/backup.sh

# Автоматический бэкап (cron)
crontab -e
# Добавить: 0 2 * * * /opt/kurator/scripts/backup.sh
```

### 5.2 Мониторинг

```bash
# Статус контейнеров
docker compose ps

# Использование ресурсов
docker stats

# Логи в реальном времени
docker compose logs -f

# Проверка здоровья
curl http://localhost:5000/health
```

### 5.3 Обновление

Обновление происходит автоматически при push в main. Для ручного обновления:

```bash
cd /opt/kurator
docker compose pull
docker compose up -d
docker image prune -f
```

### 5.4 Откат

```bash
# Список образов
docker images | grep kurator

# Откат на предыдущую версию
docker compose down
# Измените тег в docker-compose.yml на предыдущий
docker compose up -d
```

---

## Устранение неполадок

### Контейнер не запускается

```bash
# Проверьте логи
docker compose logs backend
docker compose logs frontend

# Проверьте конфигурацию
docker compose config
```

### База данных недоступна

```bash
# Проверьте статус PostgreSQL
docker compose exec postgres pg_isready

# Проверьте логи
docker compose logs postgres
```

### GitHub Actions не работает

1. Проверьте секреты в Settings → Secrets
2. Проверьте логи в Actions → выберите workflow
3. Убедитесь что SSH ключ корректный

### 502 Bad Gateway (если используется nginx)

```bash
# Проверьте что backend запущен
docker compose ps
curl http://localhost:5000/health

# Проверьте логи nginx
docker compose logs nginx
```

---

## Полезные команды

```bash
# Перезапуск всех сервисов
docker compose restart

# Перезапуск конкретного сервиса
docker compose restart backend

# Полная пересборка
docker compose down
docker compose up -d --build

# Очистка
docker system prune -af
docker volume prune -f

# Вход в контейнер
docker compose exec backend bash
docker compose exec postgres psql -U kurator_user -d kurator
```

---

## Контакты

Если возникли проблемы:
1. Создайте issue в репозитории
2. Проверьте логи: `docker compose logs`
3. Проверьте GitHub Actions workflow
