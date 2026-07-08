# LLM Fundamentals

Pierwsza solucja edukacyjna w `04-DataServices` — progresywne lekcje od podstaw LLM.

## Lekcje

| # | Temat | Czego się uczysz |
|---|-------|------------------|
| 1 | Hello LLM | Pierwsze wywołanie chat completion |
| 2 | System prompt | Jak instrukcja systemowa zmienia odpowiedź |
| 3 | Historia rozmowy | Wieloetapowy dialog z kontekstem |
| 4 | Tokeny | Szacowanie długości promptu (`tiktoken`) |

## Uruchomienie

```powershell
# Z katalogu 04-DataServices (po aktywacji .venv)
python llm-fundamentals/main.py
python llm-fundamentals/main.py --lesson 2
```

## Struktura

```
llm-fundamentals/
├── main.py                 # CLI
├── src/llm_fundamentals/
│   ├── config.py           # .env / Azure OpenAI
│   ├── client.py           # Klient z trybem dry-run
│   └── lessons.py          # Lekcje 1-4
└── tests/
```

## Konfiguracja Azure

Skopiuj `../.env.example` do `../.env` i uzupełnij:

- `AZURE_OPENAI_ENDPOINT` — jak `AzureAIAgent:Endpoint` w WebApi
- `AZURE_OPENAI_API_KEY`
- `AZURE_OPENAI_DEPLOYMENT` — np. `gpt-4o`

Bez kluczy ustaw `LLM_DRY_RUN=true`, aby przejść lekcje lokalnie.
