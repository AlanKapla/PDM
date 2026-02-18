import { useRef, useCallback } from 'react';

/**
 * Hook dodający obsługę przeciągania elementów na urządzeniach dotykowych (smartfony, tablety).
 * Natywne zdarzenia HTML drag & drop (draggable/onDragStart/onDragOver/onDragEnd) nie działają
 * na urządzeniach dotykowych — ten hook uzupełnia tę funkcjonalność za pomocą zdarzeń touch.
 *
 * Użycie: W komponencie wywołaj hook, a potem dodaj {...createTouchHandlers(index, ...)} do każdego
 * przeciągalnego elementu obok istniejących atrybutów draggable/onDragStart/onDragOver/onDragEnd.
 * 
 * Parametry reorderingu (draggedIndex, setDraggedIndex, onReorder) przekazywane są
 * do createTouchHandlers, dzięki czemu hook można wywoływać na najwyższym poziomie komponentu,
 * a touch handlery generować wewnątrz render function.
 */

interface UseTouchReorderOptions {
  /** Selektor CSS elementów-dzieci w kontenerze (domyślnie: '[data-touch-draggable]') */
  itemSelector?: string;
}

export function useTouchReorder({
  itemSelector = '[data-touch-draggable]',
}: UseTouchReorderOptions = {}) {
  const currentOverIndex = useRef<number | null>(null);
  const containerRef = useRef<HTMLElement | null>(null);

  const getIndexAtPoint = useCallback(
    (clientY: number, container: HTMLElement): number | null => {
      const items = container.querySelectorAll(itemSelector);
      for (let i = 0; i < items.length; i++) {
        const rect = items[i].getBoundingClientRect();
        if (clientY >= rect.top && clientY <= rect.bottom) {
          return i;
        }
      }
      return null;
    },
    [itemSelector]
  );

  /**
   * Tworzy zestaw touch handlerów dla elementu o danym indeksie.
   * @param index - indeks elementu w liście
   * @param draggedIndex - aktualnie przeciągany indeks (ze stanu komponentu)
   * @param setDraggedIndex - setter stanu przeciągania
   * @param onReorder - callback(fromIndex, toIndex) wywoływany przy zmianie pozycji
   */
  const createTouchHandlers = useCallback(
    (
      index: number,
      draggedIndex: number | null,
      setDraggedIndex: (idx: number | null) => void,
      onReorder: (fromIndex: number, toIndex: number) => void
    ) => ({
      'data-touch-draggable': true,
      onTouchStart: (e: React.TouchEvent) => {
        const target = e.target as HTMLElement;
        const interactiveEl = target.closest('input, textarea, select, button, [role="checkbox"], label');
        if (interactiveEl) return;

        currentOverIndex.current = index;
        containerRef.current = (e.currentTarget as HTMLElement).parentElement;
        setDraggedIndex(index);
      },
      onTouchMove: (e: React.TouchEvent) => {
        if (draggedIndex === null || !containerRef.current) return;

        e.preventDefault();

        const touch = e.touches[0];
        const targetIndex = getIndexAtPoint(touch.clientY, containerRef.current);

        if (targetIndex !== null && targetIndex !== currentOverIndex.current) {
          onReorder(currentOverIndex.current ?? draggedIndex, targetIndex);
          currentOverIndex.current = targetIndex;
        }
      },
      onTouchEnd: () => {
        setDraggedIndex(null);
        currentOverIndex.current = null;
        containerRef.current = null;
      },
    }),
    [getIndexAtPoint]
  );

  return { createTouchHandlers };
}
