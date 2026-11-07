# Deploying VELTRIX to a DigitalOcean Droplet

One server (Ubuntu 24.04, 4 GB RAM) runs three Docker containers, started by `docker-compose.prod.yml`:

```
Internet ──443/80──► caddy  (Angular app + HTTPS from Let's Encrypt)
                       │  /api, /hubs (WebSockets), /uploads
                       ▼
                      api   (ASP.NET Core 8, port 8080, not published)
                       │  internal Docker network only
                       ▼
                    sqlserver (SQL Server 2022 Express, port 1433, NOT published)
```

**Why this design (exam):** only Caddy is reachable from the internet. SQL Server has no published port and sits on an
`internal` network, so nobody outside the server can even try to log in to it. Every secret lives in one `.env` file on the
server, which is never committed (only `.env.example` is). Production starts with **no test accounts and no demo data**;
the ONE admin account is created from `.env` on the first start.

You need: a DigitalOcean account, a domain name you control, and access to the private GitHub repository.

---

## 1. Create the Droplet

1. DigitalOcean → **Create → Droplets**.
2. Image **Ubuntu 24.04 (LTS) x64**. Size: **Basic, Regular, 4 GB RAM / 2 CPUs** (SQL Server needs at least 2 GB; Intel/AMD, not ARM).
3. Region close to your players (e.g. Frankfurt).
4. Authentication: **SSH key** (add your PC's public key; on Windows: `type $env:USERPROFILE\.ssh\id_ed25519.pub`, create one first with `ssh-keygen -t ed25519` if needed). No password login.
5. Create, and note the Droplet's **public IPv4 address**.

## 2. Point your domain at it (DNS)

At your domain's DNS provider, add an **A record**: name `@` (or a subdomain such as `veltrix`) → the Droplet's IP.
Check from your PC: `nslookup veltrix.example.com` must show the Droplet's IP **before** step 7 (Caddy asks Let's Encrypt for
the certificate on the first start, and that only works when the domain already points to the server).

## 3. Firewall

DigitalOcean → **Networking → Firewalls → Create Firewall**, inbound rules:

| Type | Protocol | Port | Why |
|---|---|---|---|
| SSH | TCP | 22 | your login |
| HTTP | TCP | 80 | Let's Encrypt check + redirect to HTTPS |
| HTTPS | TCP | 443 | the site |
| Custom | UDP | 443 | HTTP/3 (optional) |

Apply it to the Droplet. Nothing else is open, 1433 included. (Use the DigitalOcean firewall rather than `ufw`: Docker writes
its own iptables rules, which bypass `ufw` for published ports.)

## 4. Prepare the server

```bash
ssh root@YOUR_IP

# A normal user for the app (instead of working as root)
adduser veltrix
usermod -aG sudo veltrix
rsync --archive --chown=veltrix:veltrix ~/.ssh /home/veltrix

# 2 GB swap: building the images (dotnet + Angular) needs more memory for a moment
fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab
exit
```

From now on log in as `ssh veltrix@YOUR_IP`.

## 5. Install Docker and get the code

```bash
# Docker from Docker's official repository (includes the "docker compose" plugin)
sudo apt-get update
sudo apt-get install -y ca-certificates curl git
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker veltrix
exit                      # log out and in again, so the docker group applies
```

The repository is private, so the server gets its own **read-only deploy key**:

```bash
ssh-keygen -t ed25519 -C "veltrix-droplet" -N "" -f ~/.ssh/id_ed25519
cat ~/.ssh/id_ed25519.pub
```

GitHub → the repository → **Settings → Deploy keys → Add deploy key**: paste the key, leave **Allow write access** OFF. Then:

```bash
git clone git@github.com:Chachi011/Veltrix.git ~/veltrix
cd ~/veltrix
git switch main           # the branch you deploy (the production files must be merged into it)
```

## 6. Create the `.env` file (the secrets)

```bash
cd ~/veltrix
cp .env.example .env
SA="Vx$(openssl rand -hex 16)9"
sed -i "s|^SA_PASSWORD=.*|SA_PASSWORD=$SA|; s|Password=CHANGE_ME;|Password=$SA;|" .env
sed -i "s|^JWT_KEY=.*|JWT_KEY=$(openssl rand -base64 48 | tr -d '\n')|" .env
sed -i "s|^ADMIN_PASSWORD=.*|ADMIN_PASSWORD=Ad$(openssl rand -hex 8)7|" .env
unset SA
chmod 600 .env
nano .env
```

These commands generate random values: the SQL Server password (written into both `SA_PASSWORD` and the connection string,
because they must match), the JWT signing key and the admin password. In `nano` set the rest:

| Variable | Value |
|---|---|
| `DOMAIN` | your domain, e.g. `veltrix.example.com` (no `https://`) |
| `ACME_EMAIL` | your email (Let's Encrypt warnings) |
| `ADMIN_EMAIL` | the admin's login email |
| `ADMIN_NICKNAME` | optional, 3–20 letters/digits/underscore (default `admin`) |
| `N8N_WEBHOOK_URL`, `N8N_SECRET` | optional AI assistant; leave empty = off |

Save with Ctrl+O, Enter, Ctrl+X. Write the admin password down in your password manager: `grep ADMIN_PASSWORD .env`.
Never use a `$` in a value (Compose would read it as a variable). `.env` is in `.gitignore` and `.dockerignore`, so it
never goes to Git or into an image.

## 7. Start

```bash
cd ~/veltrix
docker compose -f docker-compose.prod.yml up -d --build      # the first build takes about 5–10 minutes
docker compose -f docker-compose.prod.yml ps                 # sqlserver "healthy", api and caddy "running"
docker compose -f docker-compose.prod.yml logs -f api        # Ctrl+C to stop watching
```

In the API log you should see `Applying database migrations...`, `Created the admin account ...` and
`Now listening on: http://[::]:8080`. Caddy's log (`logs caddy`) shows `certificate obtained successfully`.

What happens on the first start: SQL Server starts, the API waits until it is healthy, applies all migrations
(`appsettings.Production.json`: `MigrateOnStartup: true`, `SeedTestUsers: false`, `SeedDemoData: false`), creates the
admin from `.env`, and the crash engine starts running rounds.

## 8. Check it (first-start checklist)

- [ ] `https://YOUR_DOMAIN` opens with a padlock; `http://YOUR_DOMAIN` redirects to https.
- [ ] Log in with `ADMIN_EMAIL` / `ADMIN_PASSWORD` → the admin panel works.
- [ ] Register a player in a second browser: the multiplier moves live (WebSockets through Caddy); a bet and a cash-out work.
- [ ] Upload an avatar, run `docker compose -f docker-compose.prod.yml restart api`: the avatar is still there (volume).
- [ ] `https://YOUR_DOMAIN/swagger` does NOT show Swagger (it is off in Production, and Caddy only forwards `/api`, `/hubs`, `/uploads`).
- [ ] The test accounts (`admin@veltrix.dev` ...) do not exist.
- [ ] From your PC: `Test-NetConnection YOUR_DOMAIN -Port 1433` → `TcpTestSucceeded : False`.

## 9. Updates

```bash
cd ~/veltrix
sh deploy/backup.sh                                          # always back up first
git pull
docker compose -f docker-compose.prod.yml up -d --build      # new migrations are applied on start
docker image prune -f                                        # remove old image layers
```

The API is replaced in a few seconds. A round that is running at that moment is **cancelled and every bet refunded** on the
next start (core game rule), so update when few people play. To update SQL Server itself (security patches):
`docker compose -f docker-compose.prod.yml pull sqlserver` and then `up -d`.

## 10. Backups

`deploy/backup.sh` saves the database (`veltrix-<date>.bak`) and the avatars (`uploads-<date>.tar.gz`) into `~/backups`
and keeps the newest 14 of each. The sa password is read inside the container, so it never appears in your shell history.

```bash
cd ~/veltrix && sh deploy/backup.sh
```

Every night at 03:30 (`crontab -e`, add this line):

```
30 3 * * * cd /home/veltrix/veltrix && sh deploy/backup.sh >> /home/veltrix/backups/backup.log 2>&1
```

A backup that stays on the same server is lost with the server, so also copy it off, e.g. from your PC:
`scp -r veltrix@YOUR_IP:backups .\veltrix-backups` (or turn on DigitalOcean's paid Droplet backups as well).

**Restore** (replaces the current database):

```bash
cd ~/veltrix
C="docker compose -f docker-compose.prod.yml"
$C stop api
$C cp ~/backups/veltrix-YYYY-MM-DD_HHMM.bak sqlserver:/var/opt/mssql/backup/restore.bak
$C exec -u root sqlserver chmod 644 /var/opt/mssql/backup/restore.bak
$C exec sqlserver sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "RESTORE DATABASE Veltrix FROM DISK = N'\''/var/opt/mssql/backup/restore.bak'\'' WITH REPLACE"'
# Avatars
mkdir -p /tmp/restore && tar -xzf ~/backups/uploads-YYYY-MM-DD_HHMM.tar.gz -C /tmp/restore
$C cp /tmp/restore/uploads-YYYY-MM-DD_HHMM/. api:/app/uploads
$C start api
$C exec -u root api chown -R app:app /app/uploads
```

## 11. Troubleshooting

| Problem | Look at | Usual cause |
|---|---|---|
| `api` keeps restarting | `docker compose -f docker-compose.prod.yml logs api` (the last lines say why; it exits with code 1) | Admin values invalid (weak password, bad email, email/nickname already used, only one of email/password set); `CONNECTION_STRING` password differs from `SA_PASSWORD` ("Login failed for user 'sa'"); `JWT_KEY` shorter than 32 characters |
| `sqlserver` unhealthy | `logs sqlserver` | `SA_PASSWORD` too weak (8+ characters with upper, lower and digits), or less than 2 GB RAM |
| No HTTPS certificate | `logs caddy` | DNS does not point to the server yet, or ports 80/443 are closed. Caddy retries by itself; do not restart it again and again (Let's Encrypt limits failed attempts) |
| Compose says "Set ... in .env" | `.env` | That required value is missing |
| A player forgot their password | `docker compose -f docker-compose.prod.yml logs api \| grep "EMAIL to"` | There is no email service yet: the reset code is only written to the API log, so the admin reads it there |
| Change the admin password | the app: Profile → change password | `ADMIN_*` in `.env` is used on the very first start only; later changes are ignored |
| Memory | `docker stats` | SQL Server is limited to 2 GB (`MSSQL_MEMORY_LIMIT_MB`) so the API and Caddy have room |

Logs: every container keeps at most 5 × 10 MB (`x-logging` in `docker-compose.prod.yml`).
