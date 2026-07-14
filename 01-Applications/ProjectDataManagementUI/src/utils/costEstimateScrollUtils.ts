/** Scrolluje do wiersza/karty etapu w kontenerze widoku drzewa lub kart. */
export function scrollToCostEstimateGroup(
  groupId: string,
  scrollContainer?: HTMLElement | null,
): void {
  const selector = `[data-ce-group-id="${CSS.escape(groupId)}"]`;
  const element = document.querySelector(selector);
  if (!(element instanceof HTMLElement)) {
    return;
  }

  if (scrollContainer) {
    const elementRect = element.getBoundingClientRect();
    const containerRect = scrollContainer.getBoundingClientRect();
    const targetTop = elementRect.top - containerRect.top + scrollContainer.scrollTop - 16;
    scrollContainer.scrollTo({ top: Math.max(0, targetTop), behavior: 'smooth' });
    return;
  }

  element.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}
