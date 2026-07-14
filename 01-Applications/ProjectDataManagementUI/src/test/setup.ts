import '@testing-library/jest-dom';
import * as axeMatchers from 'vitest-axe/matchers';
import { expect } from 'vitest';
import { configure } from '@testing-library/react';

// Rejestracja matcherów AXE (toHaveNoViolations)
expect.extend(axeMatchers);

// Dłuższy timeout dla testów async (np. axe)
configure({ asyncUtilTimeout: 5000 });

// Mock window.matchMedia — wymagany dla Chakra UI useBreakpointValue w jsdom
Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
    }),
});
