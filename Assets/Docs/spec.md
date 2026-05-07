# BRIEF PROJEKTU

## Wizja
MMORPG inspirowane klasycznym Ragnarok Online — izometryczny klimat, walka oparta na drużynie, system klas/profesji, grindowa progresja, huby społecznościowe.

## Skala
Multiplayer średniej skali (dziesiątki do niskich setek graczy jednocześnie, nie tysiące).

## Dostępne zasoby
- Duża biblioteka paczek assetów 3D Synty (modułowe środowiska, postacie, propy oraz cały warstwa UI)
- Claude Code jako główny partner implementacyjny

## Główne ograniczenie
Maksymalna autonomia AI. Człowiek-współpracownik chce projektować, decydować i recenzować — nie klikać po edytorach. Wszystko co może być kodem, danymi albo skryptem — powinno być kodem, danymi albo skryptem. Człowiek ma doświadczenie z Unity, ale jest otwarty na alternatywy jeśli lepiej służą celowi autonomii.

## Co docelowo musi istnieć
- Trwały świat z wieloma mapami
- Tworzenie postaci, klasy, statystyki, umiejętności
- Walka (target-based, w stylu RO)
- Inventory, ekwipunek, loot
- NPC, dialogi, questy
- System drużyn, czat
- Autorytatywny serwer, persystencja
- Klient z UI dla wszystkich powyższych systemów

## Twoje pierwsze zadanie
Zanim napiszesz jakikolwiek kod albo zadeklarujesz się przy konkretnym stacku — zbadaj obecny krajobraz (jest maj 2026, sytuacja zmienia się szybko). Zbadaj:

- Który silnik gier (obecnie jesteśmy w najnowszym Unity - prawdopodobnie to najlepszy wybór) najlepiej wspiera workflow, w którym agent AI wykonuje większość implementacji, włącznie z budowaniem świata, przy minimalnym klikaniu człowieka w edytorze
- Jakie serwery MCP, integracje z edytorem albo AI-native narzędzia istnieją dla kandydujących silników
- Jak assety Synty integrują się z każdym kandydatem
- Rozwiązania sieciowe odpowiednie dla architektury MMO małej skali
- Frameworki UI w kandydujących silnikach, które są text/code-first
- Wszelkie "AI-first" silniki albo frameworki warte rozważenia, które nie są oczywistym wyborem

Stwórz pisemną rekomendację: wybrany stack, uzasadnienie, co zostało odrzucone i dlaczego, jakie są główne ryzyka, oraz jak wyglądałby prototyp pionowy (vertical slice) w pierwszych tygodniach.

Nie zaczynaj budowania, dopóki rekomendacja nie zostanie zrecenzowana i zatwierdzona.

## Porponowane zasady pracy (nie są sztywny, możesz zmienić jeśli znajdziesz lepsze)
- Data-driven zamiast editor-driven. Świat, content i konfiguracja żyją w wersjonowanych plikach tekstowych tam, gdzie to możliwe.
- Prototypuj pionowo zanim pójdziesz wszerz. Jedna mapa, jeden mob, jedna walka, jeden login, dwóch graczy widzących się nawzajem — zanim jakiekolwiek skalowanie contentu.
- Sygnalizuj niepewność uczciwie. Jeśli coś wykracza poza to, co AI może autonomicznie zrobić dobrze — powiedz to wprost, od razu.

## Target gry
Android (Google Play)