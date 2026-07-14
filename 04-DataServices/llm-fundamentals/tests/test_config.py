"""Testy konfiguracji — bez wywołań API."""

from __future__ import annotations

import os

from llm_fundamentals.config import AzureOpenAISettings


def test_from_env_reads_dry_run(monkeypatch) -> None:
  monkeypatch.setenv("AZURE_OPENAI_ENDPOINT", "https://example.openai.azure.com")
  monkeypatch.setenv("AZURE_OPENAI_API_KEY", "test-key")
  monkeypatch.setenv("AZURE_OPENAI_DEPLOYMENT", "gpt-4o")
  monkeypatch.setenv("LLM_DRY_RUN", "true")

  settings: AzureOpenAISettings = AzureOpenAISettings.from_env()

  assert settings.dry_run is True
  assert settings.is_configured is True
  assert settings.deployment == "gpt-4o"


def test_is_configured_false_when_missing_key(monkeypatch) -> None:
  monkeypatch.delenv("AZURE_OPENAI_API_KEY", raising=False)
  monkeypatch.setenv("AZURE_OPENAI_ENDPOINT", "")
  monkeypatch.setenv("LLM_DRY_RUN", "false")

  settings: AzureOpenAISettings = AzureOpenAISettings.from_env()

  assert settings.is_configured is False
