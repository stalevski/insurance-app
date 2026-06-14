# Deployment (Docker + VPS)

Short guide for running the app on a Docker-capable Linux VPS (Phase 4). For build/run on a dev
machine see [USAGE.md](USAGE.md).

## Build the image

From the repository root:

```bash
docker build -t insurance-integration .
```

The multi-stage [Dockerfile](../../Dockerfile) restores/publishes with the .NET 10 SDK image and runs
on the smaller ASP.NET runtime image as a non-root user.

## Run the container

```bash
docker run -d \
  --name insurance-integration \
  -p 8080:8080 \
  -v insurance-data:/data \
  --restart unless-stopped \
  insurance-integration
```

Key facts:

- The app listens on **port 8080** inside the container (`ASPNETCORE_URLS=http://+:8080`).
- The SQLite database is written to **`/data/integration.db`** (overridden via
  `ConnectionStrings__Integration`). The named volume `insurance-data` keeps data across
  container replacement; migrations run automatically on startup.
- Environment defaults to `Production`, which disables Swagger UI, the development data
  seeder, and the read-only DB browser at `/database`. Set `-e ASPNETCORE_ENVIRONMENT=Development`
  to get them back (not for real exposure), or `-e DatabaseBrowser__Enabled=true` to force-enable
  just the DB browser.
- A container `HEALTHCHECK` probes `GET /health`.

Verify:

```bash
curl http://localhost:8080/health
```

## VPS checklist

1. Provision a Linux VPS with root access (Docker requires it; shared/cPanel hosting will not work).
2. Install Docker Engine: https://docs.docker.com/engine/install/ (pick your distro).
3. Copy the repo to the VPS (`git clone` or `scp`), then build and run as above.
4. Put a reverse proxy with TLS in front (Caddy is the least-config option):

   ```bash
   # /etc/caddy/Caddyfile
   your-domain.example {
       reverse_proxy localhost:8080
   }
   ```

   Caddy obtains and renews Let's Encrypt certificates automatically. Nginx + certbot works too.
5. **Do not expose the app without a proxy/firewall.** The read-only DB browser at `/database`
   is now **gated**: it is off by default outside Development and returns `404` (see `PROGRESS.md`).
   To deliberately expose it, set `DatabaseBrowser__Enabled=true` — and even then put auth in front
   at the proxy (e.g. basic auth or IP allowlist on `/database`).

## Updating

```bash
git pull
docker build -t insurance-integration .
docker stop insurance-integration && docker rm insurance-integration
# re-run the docker run command above; data persists in the insurance-data volume
```

## Backup

The whole database is one SQLite file:

```bash
docker run --rm -v insurance-data:/data -v "$PWD":/backup alpine \
  cp /data/integration.db /backup/integration-$(date +%F).db
```
