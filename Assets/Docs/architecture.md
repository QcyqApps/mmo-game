# Architektura MmoGame — żywy dokument

> Ten dokument odzwierciedla aktualne decyzje architektoniczne. Zmienia się wraz z projektem. Pełne uzasadnienie wyborów zob. plan stacku w `~/.claude/plans/`.

## Wysokopoziomowy obraz

```
┌───────────────────┐                    ┌──────────────────────┐
│  Klient (Unity)   │ ──── FishNet UDP ──→  Game Server (Unity)  │
│  Android / PC     │ ←─── world state ──   headless, Linux x64  │
└──────┬────────────┘                    └──────────┬───────────┘
       │                                            │
       │  REST/gRPC (auth, persist, social)         │ pisze/czyta
       │                                            ↓ persist data
       └──────────→  Nakama (HTTP/gRPC) ─────→ PostgreSQL
                     auth, friends, chat,
                     storage, leaderboards
```

**Podział odpowiedzialności:**

| Co | Gdzie | Dlaczego |
|---|---|---|
| Pozycje, walka, abilities, real-time state | **FishNet** (Unity Game Server) | Niska latencja, prediction, server-authoritative gameplay |
| Konta, login, postaci, ekwipunek persistowany, friends, party, chat globalny | **Nakama** | Gotowe rozwiązanie, OSS, Lua/JS scripting |
| Dane statyczne (itemy, skille, mapy, drop tables, NPC) | **YAML/JSON w `Assets/Data/`** + ScriptableObject runtime | Data-driven, edytowalne tekstowo, diff-friendly |
| Geometria mapy | **Synty modular pieces** + YAML manifest | Mapa jako lista `(prefab, position, rotation)` w danych |
| UI (HUD, inventory, character, dialogs) | **UI Toolkit** (UXML + USS) | Tekstowa, deklaratywna, AI edytuje XML/CSS bezpośrednio |

## Kluczowe biblioteki / wersje (planned)

- **Unity** 6.0.4.6f1 (LTS) — URP 17.4.0
- **FishNet** — najnowsza stable (instalowana po pierwszym buildzie testowym)
- **Nakama Unity SDK** — z `https://github.com/heroiclabs/nakama-unity.git` przez UPM
- **Addressables** — UPM (`com.unity.addressables`), dodawane przy potrzebie hot-swap content
- **Nakama server** — `heroiclabs/nakama:latest` (Docker)
- **PostgreSQL** — `postgres:16-alpine` (Docker)

## Layout repo (docelowy)

```
MmoGame/
├── Assets/
│   ├── Docs/                      # spec.md, architecture.md, lessons-learned.md
│   ├── Data/                      # YAML/JSON: items, mobs, maps, quests, drops
│   ├── Scripts/
│   │   ├── Bootstrap/             # entry points, network manager wiring
│   │   ├── Networking/            # FishNet handlers, sync logic
│   │   ├── Backend/               # Nakama client wrapper
│   │   ├── Combat/                # damage formulas, target system
│   │   ├── World/                 # map loading, NPC spawning
│   │   └── Editor/                # build scripts, dev tools
│   ├── UI/                        # UXML + USS dla HUDu, inventory, dialogów
│   ├── Content/                   # Synty assets (po imporcie)
│   └── Settings/                  # URP, Input, Quality (już są)
├── Server/
│   ├── docker-compose.yml         # Nakama + Postgres lokalnie
│   ├── nakama/local.yml           # config Nakama
│   ├── nakama/modules/            # Lua/JS gameplay logic dla Nakama
│   └── README.md                  # jak uruchomić
├── Build/
│   ├── build-server.sh            # Linux dedicated server (batchmode)
│   ├── build-android.sh           # Android client APK
│   └── README.md
└── Packages/manifest.json         # UPM dependencies
```

## Konwencje

- **Język kodu:** C# (klient + Unity server), Lua (Nakama gameplay logic), bash (build/deploy scripts).
- **Identyfikatory w kodzie:** angielskie. **UI text:** polskie (z prep pod i18n).
- **Asynchroniczność:** `async/await`, nie coroutines (Nakama SDK i nowoczesny C#).
- **DI:** brak frameworka — manualny wiring w Bootstrap, `Singleton` tylko gdy uzasadnione.
- **Serializacja danych statycznych:** YAML (czytelność) lub JSON (perf), nigdy ręcznie wpisywane w Inspectorze.
- **Server-authoritative wszystko.** Klient renderuje + przewiduje, serwer decyduje.

## Decyzje już podjęte

| Data | Decyzja | Powód |
|---|---|---|
| 2026-05-07 | Unity 6 zamiast Godot/Unreal | MCP ecosystem density, Synty native, networking choice |
| 2026-05-07 | FishNet zamiast Mirror/Photon/NGO | MMO-scale battle-tested, native prediction, OSS |
| 2026-05-07 | Nakama zamiast custom backend | Auth/social/persist out-of-the-box |
| 2026-05-07 | Self-host na OVH zamiast managed | OSS-only constraint, własny VPS already-paid |
| 2026-05-07 | UI Toolkit zamiast UGUI | Text-first, diff-friendly, fits AI-driven workflow |
| 2026-05-07 | Polski w UI, angielski w kodzie | User-facing PL, code/identifiers EN |
| 2026-05-07 | AI commituje autonomicznie | Maximum AI autonomy principle |
| 2026-05-07 | MPPM 1.6 zamiast 2.0.2 | 2.x w Unity 6.4 brak UI configuratora — niedopracowane |
| 2026-05-07 | Map JSON manifest + auto-skaner Synty | 12-entry hard-coded list nie skaluje do Prontery; auto-scan daje 423 entries + bounds |
| 2026-05-07 | `.claude/agents/map-author.md` subagent | Map authoring = osobny domain (catalog + schema + RO conventions); izolacja kontekstu |
| 2026-05-07 | Tent placeholdery dla landmarków Prontery | Knights pack nie ma tile-roof town buildings; podmiana po imporcie POLYGON Town |

## Map authoring pipeline (post Tydzień 3.5)

```
Synty packs (Assets/Synty/)
       │
       ▼ MmoGame > Rebuild Synty Catalog
[SyntyCatalogScanner] ──► MapPrefabRegistry.asset (423 entries)
                          synty-catalog.md (size + pivot per piece)
                          
JSON manifest (Assets/Resources/Maps/<name>.json)
       │
       ├─► MmoGame > Validate Maps   (MapValidator → console)
       ├─► MmoGame > Preview Map > … (MapPreview → edit-time spawn, no Play)
       └─► MapLoader.Load(name)      (runtime spawn + NavMeshSurface bake)
```

Subagent `map-author` (`.claude/agents/map-author.md`) konsumuje catalog + reference docs, edytuje JSON, validuje, preview'uje, iteruje. Dla Prontery dał 9 pieces + 10 tilings → 4859 instances, validator clean.

## Otwarte pytania (do rozstrzygnięcia w czasie)

- Specs OVH VPS (RAM, CPU) → przed pierwszym deploymentem
- Synty animation pack — czy kupować MEGA Animation Pack, czy używać Mixamo + retargeting
- Hot-reload server-side Lua (Nakama) — workflow do iteracji
- Sharding/zoning strategy — kiedy wprowadzić (nie w vertical slice)
- Anti-cheat — out of scope na razie, ale planować server-authoritative wszystko
