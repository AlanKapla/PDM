# 04-DataServices

Warstwa Python w repozytorium PDM — eksperymenty, pipeline'y danych i nauka LLM-ów.

## Wymagania

- Python 3.12+ (testowane na 3.14)
- Konto Azure OpenAI (opcjonalnie — lekcje działają też w trybie `LLM_DRY_RUN`)

## Szybki start

```powershell
cd 04-DataServices

# 1. Wirtualne środowisko
python -m venv .venv
.\.venv\Scripts\Activate.ps1

# 2. Zależności
pip install -r requirements.txt

# 3. Konfiguracja (skopiuj i uzupełnij klucze)
copy .env.example .env

# 4. Pierwsza solucja — lekcje LLM
python llm-fundamentals/main.py
```

## Projekty

| Projekt | Opis |
|---------|------|
| [llm-fundamentals](./llm-fundamentals/) | Pierwsza solucja edukacyjna — podstawy promptów, chatu i tokenów |

## Komendy

```powershell
# Wszystkie lekcje
python llm-fundamentals/main.py

# Jedna lekcja
python llm-fundamentals/main.py --lesson 1

# Tryb bez API
$env:LLM_DRY_RUN="true"; python llm-fundamentals/main.py

# Testy
pytest
```
