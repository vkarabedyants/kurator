#!/bin/bash
# ===========================================
# Database Backup Script
# Run as cron job: 0 2 * * * /opt/kurator/scripts/backup.sh
# ===========================================

set -e

APP_DIR="${APP_DIR:-/opt/kurator}"
BACKUP_DIR="${BACKUP_DIR:-/opt/kurator/backups}"
RETENTION_DAYS="${RETENTION_DAYS:-7}"

# Load environment variables
source $APP_DIR/.env

# Create backup directory
mkdir -p $BACKUP_DIR

# Generate backup filename
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="$BACKUP_DIR/kurator_backup_$TIMESTAMP.sql.gz"

echo "=== Database Backup ==="
echo "Timestamp: $TIMESTAMP"
echo "Backup file: $BACKUP_FILE"

# Create backup
echo "1. Creating database backup..."
docker exec kurator-db pg_dump -U $DB_USER kurator | gzip > $BACKUP_FILE

# Verify backup
if [ -s "$BACKUP_FILE" ]; then
    SIZE=$(du -h $BACKUP_FILE | cut -f1)
    echo "2. Backup created successfully: $SIZE"
else
    echo "ERROR: Backup file is empty!"
    rm -f $BACKUP_FILE
    exit 1
fi

# Remove old backups
echo "3. Removing backups older than $RETENTION_DAYS days..."
find $BACKUP_DIR -name "kurator_backup_*.sql.gz" -mtime +$RETENTION_DAYS -delete

# List current backups
echo "4. Current backups:"
ls -lh $BACKUP_DIR/kurator_backup_*.sql.gz 2>/dev/null || echo "   No backups found"

echo ""
echo "=== Backup Complete ==="
