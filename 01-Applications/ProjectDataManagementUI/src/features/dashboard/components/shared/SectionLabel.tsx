import React from 'react';
import { useToken } from '@chakra-ui/react';

export interface SectionLabelProps {
  text: string;
}

/** Etykieta sekcji — uppercase, 10px, gray400. */
export function SectionLabel({ text }: SectionLabelProps): React.ReactElement {
  const [neutral400] = useToken('colors', ['neutral.400']);
  return (
    <div
      style={{
        fontSize: "xs",
      fontWeight: "semibold",
        color: neutral400,
        textTransform: 'uppercase',
        letterSpacing: '0.06em',
        margin: '16px 0 8px',
      }}
    >
      {text}
    </div>
  );
}

export default SectionLabel;
