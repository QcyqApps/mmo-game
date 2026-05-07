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

### 2026-05-07 — Sekrety Nakama nie interpolują się w `local.yml`

**Kontekst:** Konfigurując Nakama przez docker-compose, najpierw wpisałem `${SESSION_ENCRYPTION_KEY}` itp. bezpośrednio w `Server/nakama/local.yml`.
**Problem:** Nakama czyta `local.yml` po starcie containera — *nie* przepuszcza go przez interpolację shell/compose. Sekrety zostałyby literalne `${SESSION_ENCRYPTION_KEY}`.
**Diagnoza:** Compose interpoluje zmienne tylko w `docker-compose.yml`, nie w plikach mountowanych do containera.
**Rozwiązanie:** Sekrety przekazywane jako CLI flagi z entrypoint compose, gdzie interpolacja działa. `local.yml` zawiera tylko nie-sekretne wartości (logger, ports, match cap).
**Wniosek:** Każdy plik konfiguracyjny mountowany jako volume — sprawdź czy aplikacja sama interpoluje env vars czy nie. Default: nie zakładaj że tak.
