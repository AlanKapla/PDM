import { useLayoutEffect, useState } from 'react';

/** Chakra UI default breakpoint: md = 48em (768px) */
const MD_MEDIA_QUERY = '(min-width: 48em)';

/** Chakra UI default breakpoint: sm = 30em (480px) */
const SM_MEDIA_QUERY = '(min-width: 30em)';

type MobileBreakpoint = 'sm' | 'md';

function getMediaQuery(breakpoint: MobileBreakpoint): string {
  return breakpoint === 'sm' ? SM_MEDIA_QUERY : MD_MEDIA_QUERY;
}

function matchesMobile(breakpoint: MobileBreakpoint): boolean {
  if (typeof window === 'undefined') {
    return false;
  }
  return !window.matchMedia(getMediaQuery(breakpoint)).matches;
}

/**
 * Returns true when viewport is below the given Chakra breakpoint.
 * Unlike useBreakpointValue, reads matchMedia synchronously on first render
 * so desktop users do not briefly see the mobile layout.
 */
export function useIsMobile(breakpoint: MobileBreakpoint = 'md'): boolean {
  const [isMobile, setIsMobile] = useState<boolean>(() => matchesMobile(breakpoint));

  useLayoutEffect(() => {
    const mediaQuery = window.matchMedia(getMediaQuery(breakpoint));
    const onChange = (): void => {
      setIsMobile(!mediaQuery.matches);
    };
    onChange();
    mediaQuery.addEventListener('change', onChange);
    return () => mediaQuery.removeEventListener('change', onChange);
  }, [breakpoint]);

  return isMobile;
}
