# Build pipeline

Skrypty bash wywołują Unity w trybie batchmode → C# build method → output do `Build/<target>/`. Cała ścieżka jest skryptowalna, bez klikania.

## Server (Linux dedicated, headless)

```sh
./Build/build-server.sh
# → Build/Server-Linux/MmoGameServer.x86_64
```

C# entry: `MmoGame.Editor.BuildScripts.BuildLinuxServer` w `Assets/Scripts/Editor/BuildScripts.cs`.

Wymaga: Unity 6.0.4.6f1 + Linux Build Support (IL2CPP) + Linux Dedicated Server module.

## Android client

```sh
./Build/build-android.sh
# → Build/Client-Android/MmoGame.apk
```

C# entry: `MmoGame.Editor.BuildScripts.BuildAndroidClient`.

Wymaga: Unity Android Build Support, Android SDK + NDK, JDK skonfigurowane w Unity Preferences. Domyślnie Unity Hub instaluje cały tooling jeśli zaznaczysz Android module.

## Logi

Każdy build pisze do `Build/last-<target>-build.log`. Te pliki są w `.gitignore`. Jeśli build się wywali — najpierw `tail -200 Build/last-server-build.log`.

## CI

Na razie brak. Plan: GitHub Actions z Unity license activation, build matrix Linux server + Android. Wprowadzimy gdy zacznie boleć ręczne buildowanie.
