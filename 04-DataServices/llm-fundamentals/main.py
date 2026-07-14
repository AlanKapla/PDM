"""Uruchamianie lekcji LLM Fundamentals."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8")

# Umożliwia uruchomienie bez instalacji pakietu (python main.py)
_SRC_DIR: Path = Path(__file__).resolve().parent / "src"
if str(_SRC_DIR) not in sys.path:
    sys.path.insert(0, str(_SRC_DIR))

from llm_fundamentals.client import LLMClient
from llm_fundamentals.config import AzureOpenAISettings
from llm_fundamentals.lessons import LESSONS


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="LLM Fundamentals — pierwsza solucja edukacyjna PDM",
    )
    parser.add_argument(
        "--lesson",
        type=int,
        choices=sorted(LESSONS.keys()),
        help="Numer lekcji (1-4). Bez flagi uruchamiane są wszystkie.",
    )
    return parser


def main() -> int:
    args = _build_parser().parse_args()
    settings: AzureOpenAISettings = AzureOpenAISettings.from_env()
    client: LLMClient = LLMClient(settings)

    print("LLM Fundamentals")
    print("================")
    print(f"Deployment: {settings.deployment}")
    print(f"Tryb: {'DRY RUN (bez API)' if client.dry_run else 'LIVE (Azure OpenAI)'}")

    if client.dry_run:
        print(
            "\nUwaga: brak pełnej konfiguracji API lub LLM_DRY_RUN=true. "
            "Lekcje 1-3 zwrócą symulowane odpowiedzi."
        )

    lesson_numbers: list[int] = [args.lesson] if args.lesson else sorted(LESSONS.keys())

    for number in lesson_numbers:
        _name, runner = LESSONS[number]
        runner(client)

    print("Gotowe.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
