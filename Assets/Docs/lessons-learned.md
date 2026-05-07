# Lessons learned

Żywy log nauki — wpadki, niespodzianki, "wiedzieliśmy gdybyśmy wiedzieli wcześniej". Każda pozycja ma kontekst: co się stało, dlaczego, jak rozwiązaliśmy/obeszliśmy.

Format jednego wpisu:
```
### YYYY-MM-DD — Krótki tytuł

**Kontekst:** co robiliśmy
**Problem:** co poszło nie tak / niespodzianka
**Diagnoza:** dlaczego
**Rozwiązanie:** jak to naprawiliśmy lub obeszliśmy
**Wniosek:** co stąd wynika dla przyszłej pracy
```

---

### 2026-05-07 — Synty meshes blokują runtime NavMesh build na Android

**Kontekst:** `MapLoader` w runtime'ie wywołuje `NavMeshSurface.BuildNavMesh()` po instantiate map pieces. W Editor playmode generuje 15× warning `RuntimeNavMeshBuilder: Source mesh ... does not allow read access. This will work in playmode in the editor but not in player`.
**Problem:** Synty paczki importują meshes z `isReadable=false` (default Unity import setting). Runtime NavMesh build wymaga CPU read access do mesh data. Działa w Editor (Unity hostuje data side-band), ale standalone Player build nie ma access — bake fail bez NavMesh = brak walkable area.
**Rozwiązanie (TBD):** dwie ścieżki:
- (a) Editor script iteruje Synty `*.asset` ImportSettings, ustawia `isReadable=true`, reimport. Acceptable runtime memory cost (CPU copy of meshes).
- (b) Pre-bake NavMesh w editor-time + serializuj `NavMeshData` jako asset per mapę. `MapLoader` w runtime instaluje pre-bakowany NavMesh przez `NavMesh.AddNavMeshData(...)` zamiast Build w runtime.
**Wniosek:** mvp w Editor działa, ale przed pierwszym Android buildem (Tydzień 6 / późniejsze) trzeba wybrać jedną ścieżkę. (b) jest production-grade ale wymaga edit-time virtual instantiation map.

### 2026-05-07 — Docker Compose YAML `>` folded scalar z mieszanym wcięciem psuje shell

**Kontekst:** Definicja `entrypoint:` Nakamy w `Server/docker-compose.yml` z `>` folded scalar i kontynuacjami flag wcięte głębiej dla czytelności:
```yaml
- >
  /nakama/nakama migrate up
    --database.address nakama:${POSTGRES_PASSWORD}@postgres:5432/nakama
  && exec /nakama/nakama
    --config /nakama/data/local.yml
    ...
```
**Problem:** Nakama padał z `dial tcp 127.0.0.1:26257: connect: connection refused` — to default port CockroachDB. Czyli flaga `--database.address` w ogóle nie docierała.
**Diagnoza:** YAML `>` folduje linie do spacji **tylko jeśli mają to samo wcięcie co pierwsza**. Linie z większym wcięciem **zachowują newline'y**. Wynikowy string ma faktyczne `\n` w środku → `sh -c "..."` traktuje je jako separatory komend. `migrate up` startuje **bez** `--database.address`, łapie default CockroachDB, łomocze.
**Rozwiązanie:** `>-` z **identycznym wcięciem** wszystkich linii. Po fixie `docker compose config` pokazuje entrypoint jako jedną długą linię — `sh` widzi single command z poprawnymi flagami.
**Wniosek:** Multi-line shell w YAML — albo `>-` z jednym poziomem wcięcia, albo `|-` z explicit `\` line continuations. Nigdy nie ufaj wizualnemu wcięciu pod `>`. Verify przez `docker compose config | grep -A 5 entrypoint`.

### 2026-05-07 — Sekrety Nakama nie interpolują się w `local.yml`

**Kontekst:** Konfigurując Nakama przez docker-compose, najpierw wpisałem `${SESSION_ENCRYPTION_KEY}` itp. bezpośrednio w `Server/nakama/local.yml`.
**Problem:** Nakama czyta `local.yml` po starcie containera — *nie* przepuszcza go przez interpolację shell/compose. Sekrety zostałyby literalne `${SESSION_ENCRYPTION_KEY}`.
**Diagnoza:** Compose interpoluje zmienne tylko w `docker-compose.yml`, nie w plikach mountowanych do containera.
**Rozwiązanie:** Sekrety przekazywane jako CLI flagi z entrypoint compose, gdzie interpolacja działa. `local.yml` zawiera tylko nie-sekretne wartości (logger, ports, match cap).
**Wniosek:** Każdy plik konfiguracyjny mountowany jako volume — sprawdź czy aplikacja sama interpoluje env vars czy nie. Default: nie zakładaj że tak.
