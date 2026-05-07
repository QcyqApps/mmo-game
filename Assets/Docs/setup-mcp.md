# MCP setup — Unity ↔ Claude Code

Cel: Claude Code może czytać scenę, tworzyć GameObjecty, edytować skrypty, runować testy itd. bez Twojego klikania w edytorze.

## Wybór: MCP for Unity (Coplay)

Po researchu (zob. plan stacku) wybraliśmy **Coplay's MCP for Unity** jako primary:
- Czysty UPM install (bez clone'owania repo, bez Python/uv)
- HTTP transport — `.mcp.json` to jednolinijkowy URL
- 40+ narzędzi pokrywających scenę, GameObjecty, components, prefab, scripts, tests, builds
- MIT, aktywny rozwój (v9.6.8 z 2026)

**Backup (jeśli Coplay nie wystarczy):** AnkleBreaker Unity MCP — 288 narzędzi, więcej zaawansowanych funkcji (Shader Graph, terrain, UMA), wymaga Node.js i clone'owania repo.

## Pierwsze uruchomienie

1. **Otwórz projekt w Unity** — UPM auto-importuje `com.coplaydev.unity-mcp` po pierwszym otwarciu (Packages/manifest.json już zawiera dependency).
2. **`Window > MCP for Unity`** — otwiera panel pluginu.
3. Kliknij **Start Server** — uruchomi się HTTP server na `localhost:8080/mcp`.
4. Z dropdownu wybierz **Claude Code**, kliknij **Configure** (zapisuje konfigurację user-scoped) — nasz `.mcp.json` w repo już ma identyczne wpisy, więc to redundantne, ale bezpieczne.
5. W Claude Code (jeśli sesja już aktywna) — restart lub spróbuj polecenia testowego: *"Pokaż mi hierarchię sceny"*.

## Jak działa

```
Claude Code  ←HTTP→  Coplay MCP plugin (in Unity Editor)  →  Unity Editor API
```

Plugin podsłuchuje na 127.0.0.1:8080 dopóki Unity Editor jest otwarty. Gdy zamykasz edytor — server gaśnie, narzędzia MCP przestają działać do następnego startu.

## Plik `.mcp.json` w repo

```json
{
  "mcpServers": {
    "unityMCP": {
      "type": "http",
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

Project-scoped, commitowany. Claude Code zobaczy go przy starcie i poprosi Cię o zatwierdzenie tego MCP servera (security prompt) — odpowiedz *yes*.

## Problemy

| Symptom | Co sprawdzić |
|---|---|
| "MCP unityMCP unavailable" | Czy Unity jest otwarte? Czy `Window > MCP for Unity > Start Server` zostało kliknięte? |
| Port 8080 zajęty | W panelu MCP for Unity zmień port; zaktualizuj `.mcp.json` URL. |
| Plugin się nie zaimportował | `Window > Package Manager > In Project > MCP for Unity`. Jeśli czerwony — sprawdź konsolę pod git/firewall errors. |
| Tools timeout | Coplay ma `batch_execute` — dla wielu operacji używaj batchy zamiast pojedynczych wywołań (10-100× szybsze). |

## Aktualizacje

```jsonc
// Packages/manifest.json
"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v9.6.8"
//                                                                          ↑ podbij tag tu
```

Po commitcie `manifest.json` Unity zauważy zmianę przy następnym otwarciu i zaimportuje nową wersję.
