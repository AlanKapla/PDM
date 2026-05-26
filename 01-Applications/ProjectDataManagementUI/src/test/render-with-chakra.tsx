import { render, type RenderOptions } from '@testing-library/react';
import { ChakraProvider } from '@chakra-ui/react';
import type { ReactElement } from 'react';

/**
 * Renderuje komponent z ChakraProvider.
 * Wymagany dla testów AXE komponentów używających Chakra UI.
 */
export function renderWithChakra(
    ui: ReactElement,
    options?: Omit<RenderOptions, 'wrapper'>
) {
    return render(ui, {
        wrapper: ({ children }) => <ChakraProvider>{children}</ChakraProvider>,
        ...options,
    });
}
