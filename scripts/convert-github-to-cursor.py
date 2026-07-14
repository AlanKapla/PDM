#!/usr/bin/env python3
"""Convert .github/agents and .github/skills to Cursor-compatible format."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GITHUB_AGENTS = ROOT / ".github" / "agents"
GITHUB_SKILLS = ROOT / ".github" / "skills"
CURSOR_AGENTS = ROOT / ".cursor" / "agents"
CURSOR_SKILLS = ROOT / ".cursor" / "skills"
CURSOR_RULES = ROOT / ".cursor" / "rules"

AGENT_DESCRIPTIONS: dict[str, str] = {
    "api-refactor-agent": (
        "Implementuje zmiany w warstwie API (.NET) na podstawie gotowego promptu. "
        "Użyj gdy masz plan zmian API (CQRS, kontrolery, serwisy) i potrzebujesz implementacji."
    ),
    "audit-agent": (
        "Audytuje domenę CQRS — analizuje kod i zapisuje raport. "
        "Użyj przed refaktorem lub gdy potrzebujesz oceny jakości kodu w domenie. NIE modyfikuje kodu."
    ),
    "controller-test-agent": (
        "Pisze testy jednostkowe dla kontrolerów ASP.NET Core (xUnit + Moq). "
        "Użyj gdy potrzebujesz testów dla kontrolera WebApi."
    ),
    "example-feature-unify-cost-modal": (
        "Przykładowy workflow wdrożenia feature — unify cost modal. "
        "Użyj jako wzorca planowania audytu i refaktoru feature."
    ),
    "full-coverage-test-orchestrator-agent": (
        "Orkiestruje pełne pokrycie testami — deleguje do agentów testowych "
        "(handlery, walidatory, serwisy, kontrolery). Użyj gdy potrzebujesz testów dla wielu warstw."
    ),
    "uber-agent": (
        "Orkiestruje audyt i refaktor CQRS — koordynuje Audit Agent i Refactor Agent. "
        "Użyj do sekwencyjnego audytu domeny i wykonywania promptów refaktoru. NIE pisze kodu."
    ),
}

READONLY_AGENTS: set[str] = {
    "api-audit-agent",
    "ui-audit-agent",
    "audit-agent",
    "feature-planner-agent",
    "uber-agent",
    "unit-test-orchestrator-agent",
    "full-coverage-test-orchestrator-agent",
    "example-feature-unify-cost-modal",
}

SKILL_RULE_GLOBS: dict[str, str] = {
    "api-cqrs": "02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/**/*.cs",
    "api-controllers": "02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/**/*.cs",
    "api-validators": "02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/**/*Validator*.cs",
    "api-entities": "02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/**/*.cs",
    "api-repositories": "02-ApplicationServices/ProductDataManagementWebAPI/src/Repositories/**/*.cs",
    "api-services": "02-ApplicationServices/ProductDataManagementWebAPI/src/Business/**/*.cs",
    "api-unit-tests": "02-ApplicationServices/ProductDataManagementWebAPI/tests/**/*.cs",
    "ui-components": "01-Applications/ProjectDataManagementUI/src/**/*.tsx",
    "ui-hooks": "01-Applications/ProjectDataManagementUI/src/**/hooks/**/*.ts",
    "ui-types": "01-Applications/ProjectDataManagementUI/src/**/types/**/*.ts",
    "ui-theme": "01-Applications/ProjectDataManagementUI/src/theme/**/*.{ts,tsx}",
    "ui-api-client": "01-Applications/ProjectDataManagementUI/src/**/api/**/*.ts",
    "ui-forms-modals": "01-Applications/ProjectDataManagementUI/src/**/*{Modal,Form}*.tsx",
    "ui-unit-tests": "01-Applications/ProjectDataManagementUI/src/**/*.test.{ts,tsx}",
    "ui-accessibility": "01-Applications/ProjectDataManagementUI/src/**/*.axe.test.{ts,tsx}",
    "brickly-landing": "01-Applications/BricklyLandingPage/**/*.{ts,tsx}",
}


def slugify_agent_name(name: str) -> str:
    slug = name.strip().lower()
    slug = re.sub(r"[^a-z0-9]+", "-", slug)
    return slug.strip("-")


def parse_frontmatter(content: str) -> tuple[dict[str, str], str]:
    if not content.startswith("---"):
        return {}, content

    match = re.match(r"^---\r?\n(.*?)\r?\n---\r?\n?", content, re.DOTALL)
    if match is None:
        return {}, content

    frontmatter: dict[str, str] = {}
    for line in match.group(1).splitlines():
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        frontmatter[key.strip()] = value.strip().strip('"')

    body = content[match.end() :]
    return frontmatter, body


def adapt_content_for_cursor(body: str) -> str:
    replacements = [
        (".github/features/", ".opencode/features/"),
        (".github/subagents/rules/", ".opencode/subagents/rules/"),
        (".github/skills/", ".cursor/skills/"),
        ("`#codebase`", "Grep, Glob i Read"),
        ("#codebase", "Grep, Glob i Read"),
        ("przez MCP ", ""),
        (" (MCP wbudowany w VS Code)", ""),
    ]
    result = body
    for old, new in replacements:
        result = result.replace(old, new)
    return result


def build_cursor_agent_frontmatter(
    agent_slug: str,
    description: str,
    readonly: bool,
) -> str:
    escaped_description = description.replace('"', '\\"')
    lines = [
        "---",
        f"name: {agent_slug}",
        f'description: "{escaped_description}"',
        "model: inherit",
    ]
    if readonly:
        lines.append("readonly: true")
    lines.append("is_background: false")
    lines.append("---")
    return "\n".join(lines) + "\n\n"


def convert_agent(agent_slug: str) -> None:
    agent_file = GITHUB_AGENTS / f"{agent_slug}.agent.md"
    if not agent_file.exists():
        agent_file = GITHUB_AGENTS / f"{agent_slug}.md"

    content = agent_file.read_text(encoding="utf-8")
    frontmatter, body = parse_frontmatter(content)

    description = frontmatter.get("description") or AGENT_DESCRIPTIONS.get(agent_slug, "")
    if not description:
        first_line = body.strip().splitlines()[0] if body.strip() else agent_slug
        description = first_line.lstrip("# ").strip()

    readonly = agent_slug in READONLY_AGENTS
    adapted_body = adapt_content_for_cursor(body)
    output = build_cursor_agent_frontmatter(agent_slug, description, readonly) + adapted_body.lstrip()

    target = CURSOR_AGENTS / f"{agent_slug}.md"
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(output, encoding="utf-8", newline="\n")


def convert_skill(skill_dir_name: str) -> None:
    source = GITHUB_SKILLS / skill_dir_name / "SKILL.md"
    if not source.exists():
        return

    content = source.read_text(encoding="utf-8")
    frontmatter, body = parse_frontmatter(content)

    name = frontmatter.get("name") or skill_dir_name
    description = frontmatter.get("description") or f"Wzorce projektu PDM dla {skill_dir_name}."

    skill_output = (
        "---\n"
        f"name: {name}\n"
        f"description: {description}\n"
        "---\n\n"
        + body.lstrip()
    )

    skill_target = CURSOR_SKILLS / skill_dir_name / "SKILL.md"
    skill_target.parent.mkdir(parents=True, exist_ok=True)
    skill_target.write_text(skill_output, encoding="utf-8", newline="\n")

    glob_pattern = SKILL_RULE_GLOBS.get(skill_dir_name)
    if glob_pattern is not None:
        rule_name = skill_dir_name.replace("/", "-")
        rule_output = (
            "---\n"
            f"description: {description}\n"
            f"globs: {glob_pattern}\n"
            "alwaysApply: false\n"
            "---\n\n"
            + body.lstrip()
        )
        rule_target = CURSOR_RULES / f"{rule_name}.mdc"
        rule_target.parent.mkdir(parents=True, exist_ok=True)
        rule_target.write_text(rule_output, encoding="utf-8", newline="\n")


def collect_agent_slugs() -> list[str]:
    slugs: set[str] = set()
    for path in GITHUB_AGENTS.glob("*.md"):
        name = path.stem
        if name.endswith(".agent"):
            slugs.add(name.removesuffix(".agent"))
        elif not (GITHUB_AGENTS / f"{name}.agent.md").exists():
            slugs.add(name)
    return sorted(slugs)


def collect_skill_dirs() -> list[str]:
    return sorted(
        path.name
        for path in GITHUB_SKILLS.iterdir()
        if path.is_dir() and (path / "SKILL.md").exists()
    )


def main() -> None:
    agent_slugs = collect_agent_slugs()
    skill_dirs = collect_skill_dirs()

    for slug in agent_slugs:
        convert_agent(slug)
        print(f"agent: {slug}")

    for skill_dir in skill_dirs:
        convert_skill(skill_dir)
        print(f"skill: {skill_dir}")

    print(f"\nConverted {len(agent_slugs)} agents and {len(skill_dirs)} skills.")


if __name__ == "__main__":
    main()
