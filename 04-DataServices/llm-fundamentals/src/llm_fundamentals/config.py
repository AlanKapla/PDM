"""Ładowanie konfiguracji Azure OpenAI z pliku .env."""

from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path

from dotenv import load_dotenv


def _find_env_file() -> Path | None:
    """Szuka .env w katalogu 04-DataServices (2 poziomy nad tym modułem)."""
    current: Path = Path(__file__).resolve()
    for parent in current.parents:
        candidate: Path = parent / ".env"
        if candidate.is_file():
            return candidate
        if parent.name == "04-DataServices":
            break
    return None


@dataclass(frozen=True)
class AzureOpenAISettings:
    """Ustawienia kompatybilne z sekcją AzureAIAgent w WebApi."""

    endpoint: str
    api_key: str
    deployment: str
    api_version: str
    dry_run: bool

    @property
    def is_configured(self) -> bool:
        return bool(self.endpoint and self.api_key and self.deployment)

    @classmethod
    def from_env(cls) -> AzureOpenAISettings:
        env_path: Path | None = _find_env_file()
        if env_path is not None:
            load_dotenv(env_path)

        dry_run_raw: str = os.getenv("LLM_DRY_RUN", "false").strip().lower()
        dry_run: bool = dry_run_raw in {"1", "true", "yes", "on"}

        return cls(
            endpoint=os.getenv("AZURE_OPENAI_ENDPOINT", "").strip().rstrip("/"),
            api_key=os.getenv("AZURE_OPENAI_API_KEY", "").strip(),
            deployment=os.getenv("AZURE_OPENAI_DEPLOYMENT", "gpt-4o").strip(),
            api_version=os.getenv("AZURE_OPENAI_API_VERSION", "2024-10-21").strip(),
            dry_run=dry_run,
        )
