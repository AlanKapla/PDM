import React from 'react';

export interface BadgeProps {
  text: string;
  bg: string;
  color: string;
  small?: boolean;
}

/** Generyczny badge statusu/etykiety. */
export function Badge({ text, bg, color, small = false }: BadgeProps): React.ReactElement {
  return (
    <span
      style={{
        display: 'inline-block',
        background: bg,
        color,
        borderRadius: 20,
        padding: small ? '1px 6px' : '2px 8px',
        fontSize: small ? 10 : 11,
        fontWeight: 500,
        lineHeight: '16px',
        whiteSpace: 'nowrap',
      }}
    >
      {text}
    </span>
  );
}

export default Badge;
