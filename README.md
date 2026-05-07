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
- [x] **Tydzień 2** — Login (Nakama device-id), Knight spawn, FishNet host + MPPM client, dwóch graczy się widzi.
- [x] **Tydzień 3 etap 1-4** — Synty Knight visual, izometryczna kamera (45° follow), data-driven mapa z JSON, click-to-move via NavMesh.
- [x] **Tydzień 3.5 — Map authoring tooling** — auto-skanner Synty (423 entries → `Assets/Docs/maps/synty-catalog.md`), schema z `tilings[]` + `parent`/`note`, `MapValidator`, edit-time `MapPreview`, `.claude/agents/map-author.md` subagent, **Prontera V1** (200×200u walled city, plaza + 4 gates + 4 landmarks, 4859 instances).
- [x] **Tydzień 3.6 — Map Editor + V4 iteracja** — pełny `MapEditorWindow` (paleta prefabów z search + kategoriami, click-to-place w scenie z snap, IMGUI inspector dla zaznaczonego pieca, right-click context menu, hotkeys Del/Shift+D/G/R, floating toolbar, edytowalne tilings, round-trip JSON↔scena przez `MapPieceMarker`). Pipeline upgrades: `MapPreview` cache refresh, `Selected JSON ⌘⇧M`, `MapValidator` whitelist par prefiksów dla naturalnych stacks. **Prontera V4** — 140×140u zwarte miasto z prawdziwą siatką ulic, 4 zróżnicowane dzielnice (mieszkalna / handlowa / koszary / świątynna), 20 wież obronnych, kościół-landmark z cmentarzem, 268 pieces + 38 tilings = 3910 instances.
- [ ] **Tydzień 3 etap 5** — UI Toolkit HUD (HP/SP/level placeholdery).
- [ ] **Tydzień 4** — Walka target-based, mob AI, drop loot, Knight Animator Controller.
- [ ] **Tydzień 5** — Inventory, equip, persystencja postaci w Nakama Storage. Naprawa Synty mesh `isReadable=false` przed Android buildem.
- [ ] **Tydzień 6** — Chat, polish, Android build, demo.

### Mapa — workflow

**Primary: `MmoGame > Map Editor`** — dedykowane okno z paletą prefabów + scene round-trip:
- Lewy panel: search/kategoria filtr → klik prefab → place mode w scenie (klik = postaw, R = rotacja, Esc = wyjście). Snap to grid + snap to ground opcjonalne.
- Prawy panel: inspector zaznaczonego pieca (Vector3 fields), tilings list (min/max/step/parent inline edit).
- W scenie: zaznacz piec → przesuwasz/obracasz/skalujesz **normalnym Unity gizmem** (W/E/R), Map Editor mirroruje zmiany do manifestu; right-click = context menu (Duplicate / Drop to ground / Rotate ±90° / Reset / Move to group / Delete); hotkeys Delete, Shift+D, G, R; floating toolbar nad zaznaczonym obiektem.
- `Save` zapisuje manifest do `Assets/Resources/Maps/<name>.json`. `Reload` odrzuca lokalne edycje. `Validate` odpala `MapValidator`. `Rebuild Catalog` po imporcie nowych Synty packów.

**Alternatywne / dodatkowe:**
- Edycja JSON ręcznie w `Assets/Resources/Maps/<name>.json` (schema → `Assets/Scripts/World/MapManifest.cs`).
- `MmoGame > Preview Map > Selected JSON` (⌘⇧M / Ctrl+Shift+M na zaznaczonym pliku JSON w Project view) — szybki preview bez otwierania okna.
- `MmoGame > Validate Maps` — standalone pre-flight check (registry resolve / Y range / AABB overlap z whitelistą par dla naturalnych stacks).
- `MmoGame > Clear Map Preview` — cleanup preview ze sceny.

**Referencje** (czytaj **przed** authoringiem):
- `Assets/Docs/maps/synty-catalog.md` — auto-generated index (rozmiary + pivot offsets per prefab).
- `Assets/Docs/maps/prontera-reference.md` — design intent dla Prontery, używany przez `map-author` subagenta.

## Konwencje

- Identyfikatory w kodzie: **angielskie**. UI text: **polski** (gotowe do i18n).
- Server-authoritative wszystko. Klient renderuje + przewiduje, serwer decyduje.
- Async/await, nie coroutines.
- Data-driven content (YAML/JSON/ScriptableObject) > prefaby z handcrafted data.
- AI commituje autonomicznie. Recenzja przez `git log` + diff.

## Licencja

Niezdecydowane (MIT prawdopodobnie). Synty assetów nie commitujemy.
