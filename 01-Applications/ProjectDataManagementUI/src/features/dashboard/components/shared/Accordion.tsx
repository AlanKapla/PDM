import React, { useState } from 'react';
import type { ReactNode } from 'react';
import { COLOR_PALETTE } from '../../utils/colors';

export interface AccordionProps {
  header: ReactNode;
  children: ReactNode;
  defaultOpen?: boolean;
  headerBg?: string;
}

/** Animowany accordion z chevronem SVG. Domyślnie zamknięty. */
export function Accordion({
  header,
  children,
  defaultOpen = false,
  headerBg,
}: AccordionProps): React.ReactElement {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  return (
    <div
      style={{
        border: `0.5px solid ${COLOR_PALETTE.border}`,
        borderRadius: 10,
        overflow: 'hidden',
      }}
    >
      <div
        role="button"
        tabIndex={0}
        onClick={() => setIsOpen((prev) => !prev)}
        onKeyDown={(e) => e.key === 'Enter' && setIsOpen((prev) => !prev)}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          padding: '10px 14px',
          cursor: 'pointer',
          background: headerBg ?? '#fff',
          userSelect: 'none',
        }}
      >
        <svg
          width={14}
          height={14}
          viewBox="0 0 14 14"
          fill="none"
          style={{
            flexShrink: 0,
            transform: isOpen ? 'rotate(90deg)' : 'rotate(0deg)',
            transition: 'transform 0.2s ease',
            color: COLOR_PALETTE.gray400,
          }}
        >
          <path d="M5 3l4 4-4 4" stroke="currentColor" strokeWidth={1.5} strokeLinecap="round" strokeLinejoin="round" />
        </svg>
        {header}
      </div>
      {isOpen && (
        <div style={{ padding: '12px 14px', borderTop: `0.5px solid ${COLOR_PALETTE.border}` }}>
          {children}
        </div>
      )}
    </div>
  );
}

export default Accordion;
