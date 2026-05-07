# MmoGame

Izometryczny MMORPG w stylu Ragnarok Online — Unity 6 + FishNet + Nakama. Android pierwszy, PC follow-up. Mid-scale (dziesiątki–niskie setki CCU). Solo dev + AI implementation partner.

> Pełna wizja → [`Assets/Docs/spec.md`](Assets/Docs/spec.md)
> Architektura → [`Assets/Docs/architecture.md`](Assets/Docs/architecture.md)
> MCP setup → [`Assets/Docs/setup-mcp.md`](Assets/Docs/setup-mcp.md)

## Stack (TL;DR)

| | |
|---|---|
| Engine | **Unity 6.0.4.6f1** (URP) |
| Networking | **FishNet** 4.7.2 — server-authoritative, prediction |
| Backend | **Nakama** 3.21.1 self-hosted (Docker, OVH) — auth/social/persist |
| Database | **PostgreSQL** 16 |
| AI bridge | **Coplay MCP for Unity** v9.6.8 |
| UI | **UI Toolkit** (UXML + USS) |
| Assets | **Synty** (POLYGON packs) |

Wszystko OSS, samohostowane, deklaratywne tam gdzie się da.

## Layout

```
Assets/
  Docs/        # spec, architecture, setup, lessons-learned
  Scripts/     # MmoGame.asmdef + MmoGame.Editor.asmdef + Bootstrap/
  Settings/    # URP, Input, Quality (już skonfigurowane)
  Scenes/      # SampleScene (placeholder)
Packages/
  manifest.json # FishNet + Nakama + Coplay MCP + Unity defaults
Server/
  docker-compose.yml  # Nakama + PostgreSQL
  nakama/local.yml    # Nakama config
Build/
  build-server.sh     # Linux dedicated server (Unity batchmode)
  build-android.sh    # Android client APK
.mcp.json      # Claude Code → Coplay MCP HTTP @ localhost:8080
```

## Pierwsze uruchomienie

```sh
# 1. Backend stack (lokalnie)
cd Server
cp .env.example .env  # uzupełnij hasła i klucze
docker compose up -d
# Konsola Nakama: http://localhost:7351

# 2. Unity Editor
# Otwórz folder MmoGame/ przez Unity Hub.
# Pierwsze otwarcie zaimportuje UPM dependencies (FishNet, Nakama, Coplay MCP)
# — może zająć kilka minut.

# 3. MCP for Claude Code
# W Unity: Window > MCP for Unity > Start Server
# .mcp.json w repo już ma odpowiedni wpis — Claude Code zapyta o autoryzację.

# 4. Headless server build (po pełnym imporcie pakietów)
./Build/build-server.sh
# → Build/Server-Linux/MmoGameServer.x86_64
```

Wymagane Unity modules (przez Hub): Linux Build Support (IL2CPP), Linux Dedicated Server, Android Build Support, Android SDK + NDK, OpenJDK.

## Roadmap

Vertical slice (6 tygodni): jedna mapa, jeden mob, jedna walka, login, dwóch graczy.

- [x] **Tydzień 1** — Fundament: git, server stack, build pipeline, packages, MCP, asmdefs.
- [ ] **Tydzień 2** — Login (Nakama), spawn, FishNet networking, dwóch graczy się widzi.
- [ ] **Tydzień 3** — Mapa Synty z YAML manifestu, izometryczna kamera, click-to-move.
- [ ] **Tydzień 4** — Walka target-based, mob AI, drop loot.
- [ ] **Tydzień 5** — Inventory, equip, persystencja postaci.
- [ ] **Tydzień 6** — Chat, polish, Android build, demo.

## Konwencje

- Identyfikatory w kodzie: **angielskie**. UI text: **polski** (gotowe do i18n).
- Server-authoritative wszystko. Klient renderuje + przewiduje, serwer decyduje.
- Async/await, nie coroutines.
- Data-driven content (YAML/JSON/ScriptableObject) > prefaby z handcrafted data.
- AI commituje autonomicznie. Recenzja przez `git log` + diff.

## Licencja

Niezdecydowane (MIT prawdopodobnie). Synty assetów nie commitujemy.
