#!/bin/bash
# ===========================================
# Manual Deployment Script
# Run from /opt/kurator on the server
# ===========================================

set -e

APP_DIR="${APP_DIR:-/opt/kurator}"
cd $APP_DIR

echo "=== Kurator Deployment ==="

# Check if .env exists
if [ ! -f ".env" ]; then
    echo "ERROR: .env file not found!"
    echo "Copy .env.template to .env and configure it first."
    exit 1
fi

# Load environment variables
source .env

# Login to GitHub Container Registry
echo "1. Logging into GitHub Container Registry..."
if [ -z "$GITHUB_TOKEN" ]; then
    echo "Enter your GitHub Personal Access Token (with read:packages scope):"
    read -s GITHUB_TOKEN
fi
echo $GITHUB_TOKEN | docker login ghcr.io -u ${GITHUB_USER:-$USER} --password-stdin

# Pull latest images
echo "2. Pulling latest images..."
docker compose pull

# Stop existing containers
echo "3. Stopping existing containers..."
docker compose down

# Start new containers
echo "4. Starting containers..."
docker compose up -d

# Wait for services to be healthy
echo "5. Waiting for services to be healthy..."
sleep 30

# Check container status
echo "6. Container status:"
docker compose ps

# Health check
echo "7. Running health check..."
if curl -sf http://localhost:5000/health > /dev/null; then
    echo "   Backend: OK"
else
    echo "   Backend: FAILED"
fi

if curl -sf http://localhost:3000 > /dev/null; then
    echo "   Frontend: OK"
else
    echo "   Frontend: FAILED"
fi

# Cleanup old images
echo "8. Cleaning up old images..."
docker image prune -f

echo ""
echo "=== Deployment Complete ==="
echo ""
echo "Access the application at:"
echo "  Frontend: http://$(hostname -I | awk '{print $1}'):3000"
echo "  Backend:  http://$(hostname -I | awk '{print $1}'):5000/api"
echo ""
