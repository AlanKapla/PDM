"""Lekcje edukacyjne — podstawy pracy z LLM."""

from __future__ import annotations

from llm_fundamentals.client import LLMClient


def run_lesson_01_hello(client: LLMClient) -> None:
    """Lekcja 1: pierwsze wywołanie modelu (completion)."""
    print("\n=== Lekcja 1: Hello LLM ===")
    print("Cel: wysłać prosty prompt i odczytać odpowiedź tekstową.\n")

    messages: list[dict[str, str]] = [
        {
            "role": "user",
            "content": (
                "W jednym zdaniu wyjaśnij, czym jest Large Language Model (LLM). "
                "Odpowiedz po polsku."
            ),
        },
    ]

    answer: str = client.chat(messages, temperature=0.3, max_tokens=200)
    print(f"Odpowiedź modelu:\n{answer}\n")


def run_lesson_02_system_prompt(client: LLMClient) -> None:
    """Lekcja 2: rola systemowa kształtuje styl i kontekst odpowiedzi."""
    print("\n=== Lekcja 2: System prompt ===")
    print("Cel: porównać odpowiedź z instrukcją systemową i bez niej.\n")

    user_question: str = "Opisz, do czego służy harmonogram prac w projekcie budowlanym."

    without_system: list[dict[str, str]] = [{"role": "user", "content": user_question}]
    with_system: list[dict[str, str]] = [
        {
            "role": "system",
            "content": (
                "Jesteś asystentem PDM (Project Data Management). "
                "Odpowiadaj zwięźle, po polsku, maksymalnie 3 zdania. "
                "Używaj terminologii branży budowlanej."
            ),
        },
        {"role": "user", "content": user_question},
    ]

    print("--- Bez system prompt ---")
    print(client.chat(without_system, temperature=0.5, max_tokens=250))
    print("\n--- Z system prompt (kontekst PDM) ---")
    print(client.chat(with_system, temperature=0.5, max_tokens=250))
    print()


def run_lesson_03_conversation(client: LLMClient) -> None:
    """Lekcja 3: wieloetapowa rozmowa z historią wiadomości."""
    print("\n=== Lekcja 3: Historia rozmowy ===")
    print("Cel: model pamięta wcześniejsze wiadomości w tej samej sesji.\n")

    messages: list[dict[str, str]] = [
        {
            "role": "system",
            "content": "Pomagasz planować kosztorys budowlany. Odpowiadaj po polsku.",
        },
        {"role": "user", "content": "Projekt to budowa hali magazynowej 1200 m2."},
    ]

    first_reply: str = client.chat(messages, temperature=0.4, max_tokens=200)
    print(f"Asystent (runda 1):\n{first_reply}\n")

    messages.append({"role": "assistant", "content": first_reply})
    messages.append(
        {
            "role": "user",
            "content": "Jakie 3 główne grupy kosztów powinienem uwzględnić?",
        },
    )

    second_reply: str = client.chat(messages, temperature=0.4, max_tokens=300)
    print(f"Asystent (runda 2, z kontekstem):\n{second_reply}\n")


def run_lesson_04_tokens(client: LLMClient) -> None:
    """Lekcja 4: szacowanie tokenów — koszt i limity kontekstu."""
    import tiktoken

    print("\n=== Lekcja 4: Tokeny ===")
    print("Cel: zrozumieć, jak mierzyć długość promptu przed wysłaniem do API.\n")

    sample_text: str = (
        "Kosztorys inwestorski dla hali magazynowej obejmuje roboty ziemne, "
        "konstrukcję stalową, instalacje HVAC oraz wykończenia."
    )

    encoding = tiktoken.encoding_for_model("gpt-4o")
    token_count: int = len(encoding.encode(sample_text))

    print(f"Tekst: {sample_text}")
    print(f"Liczba tokenów (gpt-4o): {token_count}")
    print(f"Przybliżona liczba słów: {len(sample_text.split())}")
    print(
        "\nWskazówka: długi kontekst = więcej tokenów wejściowych = wyższy koszt "
        "i ryzyko przekroczenia limitu okna kontekstowego modelu."
    )

    if not client.dry_run:
        short_answer: str = client.chat(
            [{"role": "user", "content": f"Streść w 1 zdaniu: {sample_text}"}],
            temperature=0.2,
            max_tokens=80,
        )
        output_tokens: int = len(encoding.encode(short_answer))
        print(f"\nOdpowiedź modelu ({output_tokens} tokenów wyjściowych):\n{short_answer}")
    print()


LESSONS: dict[int, tuple[str, object]] = {
    1: ("hello", run_lesson_01_hello),
    2: ("system-prompt", run_lesson_02_system_prompt),
    3: ("conversation", run_lesson_03_conversation),
    4: ("tokens", run_lesson_04_tokens),
}
