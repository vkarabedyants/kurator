#!/bin/bash
# ===========================================
# Kurator Server Setup Script for Ubuntu
# Run as root or with sudo
# ===========================================

set -e

echo "=== Kurator Server Setup ==="

# Update system
echo "1. Updating system packages..."
apt-get update && apt-get upgrade -y

# Install required packages
echo "2. Installing required packages..."
apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    git \
    ufw

# Install Docker
echo "3. Installing Docker..."
if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh
fi

# Install Docker Compose
echo "4. Installing Docker Compose..."
if ! command -v docker-compose &> /dev/null; then
    apt-get install -y docker-compose-plugin
fi

# Start and enable Docker
systemctl start docker
systemctl enable docker

# Create application directory
APP_DIR="/opt/kurator"
echo "5. Creating application directory at $APP_DIR..."
mkdir -p $APP_DIR
mkdir -p $APP_DIR/nginx/ssl

# Create application user
echo "6. Creating application user..."
if ! id "kurator" &>/dev/null; then
    useradd -r -s /bin/false kurator
    usermod -aG docker kurator
fi

# Configure firewall
echo "7. Configuring firewall..."
ufw --force enable
ufw allow ssh
ufw allow http
ufw allow https
ufw allow 3000/tcp  # Frontend (if no nginx)
ufw allow 5000/tcp  # Backend API (if no nginx)

# Create environment file template
echo "8. Creating environment template..."
cat > $APP_DIR/.env.template << 'EOF'
# Database
DB_USER=kurator_user
DB_PASSWORD=CHANGE_ME_STRONG_PASSWORD

# JWT Settings (generate with: openssl rand -base64 64)
JWT_SECRET=CHANGE_ME_GENERATE_RANDOM_STRING

# Encryption key (min 32 chars)
ENCRYPTION_KEY=CHANGE_ME_32_CHARACTERS_MINIMUM

# GitHub repository (for pulling images)
GITHUB_REPOSITORY=vkarabedyants/kurator

# API URL (your domain)
API_URL=https://your-domain.com/api
CORS_ORIGINS=https://your-domain.com
EOF

# Create docker-compose symlink
echo "9. Setting up Docker Compose..."
cat > $APP_DIR/docker-compose.yml << 'EOF'
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: kurator-db
    environment:
      POSTGRES_DB: kurator
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - kurator-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER} -d kurator"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: always

  backend:
    image: ghcr.io/${GITHUB_REPOSITORY}/backend:latest
    container_name: kurator-api
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: "http://+:8080"
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=kurator;Username=${DB_USER};Password=${DB_PASSWORD}"
      JwtSettings__Secret: ${JWT_SECRET}
      JwtSettings__Issuer: "Kurator"
      JwtSettings__Audience: "Kurator"
      JwtSettings__ExpiryMinutes: 480
      Encryption__Key: ${ENCRYPTION_KEY}
      DataProtection__KeysPath: "/app/dataprotection-keys"
      CorsOrigins: "${CORS_ORIGINS}"
    volumes:
      - dataprotection-keys:/app/dataprotection-keys
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - kurator-network
    restart: always
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  frontend:
    image: ghcr.io/${GITHUB_REPOSITORY}/frontend:latest
    container_name: kurator-frontend
    environment:
      NEXT_PUBLIC_API_URL: ${API_URL}
      NODE_ENV: production
    ports:
      - "3000:3000"
    depends_on:
      - backend
    networks:
      - kurator-network
    restart: always

volumes:
  postgres_data:
  dataprotection-keys:

networks:
  kurator-network:
    driver: bridge
EOF

# Set permissions
chown -R kurator:kurator $APP_DIR
chmod 600 $APP_DIR/.env.template

echo ""
echo "=== Setup Complete ==="
echo ""
echo "Next steps:"
echo "1. Copy .env.template to .env and configure:"
echo "   cp $APP_DIR/.env.template $APP_DIR/.env"
echo "   nano $APP_DIR/.env"
echo ""
echo "2. Generate secure secrets:"
echo "   JWT_SECRET: openssl rand -base64 64"
echo "   DB_PASSWORD: openssl rand -base64 32"
echo "   ENCRYPTION_KEY: openssl rand -base64 32"
echo ""
echo "3. Configure GitHub Secrets in your repository (Settings > Secrets):"
echo "   - SERVER_HOST: Your server IP"
echo "   - SERVER_USER: ubuntu (or your user)"
echo "   - SSH_PRIVATE_KEY: Your SSH private key"
echo "   - APP_DIR: /opt/kurator"
echo ""
echo "4. Login to GitHub Container Registry:"
echo "   echo YOUR_GITHUB_TOKEN | docker login ghcr.io -u YOUR_USERNAME --password-stdin"
echo ""
echo "5. Pull and start the application:"
echo "   cd $APP_DIR"
echo "   docker compose pull"
echo "   docker compose up -d"
echo ""
