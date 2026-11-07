#!/bin/sh
# Backs up VELTRIX on the server: the database (a SQL Server .bak file) and the uploaded avatars.
# Run it from the repository folder:  sh deploy/backup.sh [folder]     (default folder: ~/backups)
# Keeps the newest 14 of each. Copy the folder off the server as well (docs/DEPLOY.md, step 10).
set -eu

dest="${1:-$HOME/backups}"
stamp=$(date +%Y-%m-%d_%H%M)
compose="docker compose -f docker-compose.prod.yml"
mkdir -p "$dest"

# 1. Database: SQL Server writes the .bak file inside its own volume, then it is copied out.
#    The sa password is read INSIDE the container, so it never appears in this shell or its history.
$compose exec -T sqlserver mkdir -p /var/opt/mssql/backup
$compose exec -T sqlserver sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "BACKUP DATABASE Veltrix TO DISK = N'\''/var/opt/mssql/backup/veltrix.bak'\'' WITH INIT, CHECKSUM"'
$compose cp sqlserver:/var/opt/mssql/backup/veltrix.bak "$dest/veltrix-$stamp.bak"

# 2. Avatars (the uploads volume)
$compose cp api:/app/uploads "$dest/uploads-$stamp"
tar -czf "$dest/uploads-$stamp.tar.gz" -C "$dest" "uploads-$stamp"
rm -rf "$dest/uploads-$stamp"

# 3. Keep the newest 14 of each
ls -1t "$dest"/veltrix-*.bak | tail -n +15 | xargs -r rm --
ls -1t "$dest"/uploads-*.tar.gz | tail -n +15 | xargs -r rm --

echo "Backup written to $dest: veltrix-$stamp.bak and uploads-$stamp.tar.gz"
