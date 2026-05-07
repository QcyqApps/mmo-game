# Server stack

Self-hostowany backend dla MmoGame: **Nakama** (auth, friends, chat, persistent storage) + **PostgreSQL**. Działa lokalnie do dev/test, identyczny stack deployowany na OVH VPS produkcyjnie.

## Lokalny start

Wymaga: Docker + Docker Compose.

```sh
cd Server
cp .env.example .env
# edytuj .env, uzupełnij hasła i klucze
docker compose up -d
```

Po starcie:
- **HTTP API**: http://localhost:7350
- **gRPC API**: localhost:7349
- **Console**: http://localhost:7351 (login: `admin` / `$CONSOLE_PASSWORD` z .env)

Sprawdź:
```sh
curl http://localhost:7350/healthcheck
docker compose logs -f nakama
```

## Stop / restart

```sh
docker compose stop      # zatrzymuje, dane zostają
docker compose down      # zatrzymuje + usuwa kontenery (dane w postgres-data/ zostają)
docker compose down -v   # USUWA też volumeny — pełny reset
```

## Logika serwerowa (Nakama modules)

Lua/JS/Go moduły z gameplay logic (questy, handel, walidacje) idą do `nakama/modules/`. Nakama auto-loaduje przy starcie. Edycje wymagają `docker compose restart nakama`.

## Deploy na OVH VPS (planowane)

```sh
# Na VPS:
git clone <repo>
cd MmoGame/Server
cp .env.example .env && nano .env   # PRODUKCYJNE, mocne hasła
docker compose up -d
```

Reverse proxy (np. Caddy/Traefik) z TLS przed Nakama jest wymagany dla produkcji — porty 7349/7350 nie wystawiamy bezpośrednio na świat. **TBD: pisanie konfigu reverse proxy gdy ruszymy deploy.**

## Sekrety

`.env` jest w `.gitignore`. Nigdy nie commitujemy haseł. Na produkcji sekrety przez systemd EnvironmentFile lub menedżer sekretów.
