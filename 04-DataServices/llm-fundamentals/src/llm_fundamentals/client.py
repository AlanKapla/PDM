"""Klient Azure OpenAI z obsługą trybu demonstracyjnego."""

from __future__ import annotations

from openai import AzureOpenAI

from llm_fundamentals.config import AzureOpenAISettings


class LLMClient:
    """Cienka warstwa nad SDK OpenAI — ułatwia lekcje i testy."""

    def __init__(self, settings: AzureOpenAISettings) -> None:
        self._settings: AzureOpenAISettings = settings
        self._client: AzureOpenAI | None = None

        if settings.is_configured and not settings.dry_run:
            self._client = AzureOpenAI(
                azure_endpoint=settings.endpoint,
                api_key=settings.api_key,
                api_version=settings.api_version,
            )

    @property
    def deployment(self) -> str:
        return self._settings.deployment

    @property
    def dry_run(self) -> bool:
        return self._settings.dry_run or not self._settings.is_configured

    def chat(
        self,
        messages: list[dict[str, str]],
        temperature: float = 0.7,
        max_tokens: int = 512,
    ) -> str:
        """Wysyła wiadomości do modelu i zwraca treść odpowiedzi."""
        if self.dry_run:
            last_user: str = next(
                (message["content"] for message in reversed(messages) if message["role"] == "user"),
                "",
            )
            return (
                "[DRY RUN] Symulowana odpowiedź modelu. "
                f"Ostatni prompt użytkownika: {last_user!r}"
            )

        if self._client is None:
            raise RuntimeError(
                "Brak konfiguracji Azure OpenAI. Uzupełnij .env lub ustaw LLM_DRY_RUN=true."
            )

        response = self._client.chat.completions.create(
            model=self._settings.deployment,
            messages=messages,
            temperature=temperature,
            max_tokens=max_tokens,
        )

        content: str | None = response.choices[0].message.content
        return content or ""
