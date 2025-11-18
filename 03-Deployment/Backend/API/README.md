Instrukcje Docker (Windows) dla ProductDataManagementWebAPI

📁 Zawartość katalogu 03-Deployment
Plik	Rola
docker-compose.yml	Konfiguracja bazowa środowiska Release / Production
docker-compose.override.yml	Nadpisania dla Debug / Development podczas lokalnego uruchamiania
.env.example	Przykładowe zmienne środowiskowe – skopiuj jako .env
🔍 Dlaczego dwa pliki Compose?

Docker Compose automatycznie ładuje docker-compose.yml oraz docker-compose.override.yml.

Polecenie docker compose up uruchamia środowisko lokalne (Debug / Development).

Uruchomienie produkcyjne pomija plik override – wtedy działa tylko konfiguracja Release.

🔐 Wymagane zmienne środowiskowe

Zaczerpnięte z src/Entities/appsettings.json:

ConnectionStrings__DefaultConnection


W pliku .env.example znajduje się gotowy wzór — skopiuj go jako .env i uzupełnij wartości.

Opcjonalnie można nadpisać ustawienia JWT (JwtSettings__*).

🪟 Windows Containers

Obrazy bazują na mcr.microsoft.com/dotnet/*:8.0-nanoserver-1809

Upewnij się, że Docker Desktop działa w trybie Windows Containers
(prawy klik na ikonę Docker → Switch to Windows containers)

🧪 Uruchamianie lokalne – Debug / Development

Przejdź do katalogu 03-Deployment

Utwórz plik .env na podstawie .env.example i ustaw zmienne (w tym CONNECTIONSTRINGS__DEFAULTCONNECTION)

Uruchom:

docker compose up --build -d


Aplikacja będzie dostępna pod adresem:

http://localhost:8080

🚀 Uruchomienie produkcyjne – Release

Uruchamiane bez pliku override:

docker compose -f docker-compose.yml up --build -d


Port aplikacji pozostaje 8080.

⚙️ Zmienne środowiskowe w kontenerze
Zmienna	Przykładowa wartość
ASPNETCORE_ENVIRONMENT	Development / Production
ASPNETCORE_URLS	http://+:8080
ConnectionStrings__DefaultConnection	Wczytywany przez aplikację jako DefaultConnection
🔌 Porty
Host	Kontener	Opis
8080	8080	Kestrel

Aby zmienić porty — edytuj pliki Compose.

🧹 Czyszczenie
Akcja	Polecenie
Zatrzymanie kontenerów	docker compose down
Pełna przebudowa	docker compose build --no-cache