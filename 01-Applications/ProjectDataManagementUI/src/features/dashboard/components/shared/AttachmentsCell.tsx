import React, { useState } from 'react';
import type { TrackedCostAttachmentWeb } from '../../types/projectDashboard.types';
import AppModal from '../../../../components/ui/AppModal';
import { COLOR_PALETTE } from '../../utils/colors';

interface AttachmentsCellProps {
  attachments: TrackedCostAttachmentWeb[];
  costName: string;
}

/** Ikonka spinacza z liczbą załączników. Kliknięcie otwiera listę z linkami. */
export function AttachmentsCell({ attachments, costName }: AttachmentsCellProps): React.ReactElement | null {
  const [isOpen, setIsOpen] = useState(false);

  if (attachments.length === 0) {
    return null;
  }

  return (
    <>
      <button
        onClick={() => setIsOpen(true)}
        title={`${attachments.length} załącznik(i)`}
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 3,
          fontSize: 12,
          padding: '2px 6px',
          background: COLOR_PALETTE.blue50,
          color: COLOR_PALETTE.blue600,
          border: `0.5px solid ${COLOR_PALETTE.blue400}`,
          borderRadius: 4,
          cursor: 'pointer',
        }}
      >
        {/* Paperclip SVG */}
        <svg
          width="11"
          height="11"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
        </svg>
        {attachments.length}
      </button>

      <AppModal
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        title={`Załączniki — ${costName}`}
        desktopSize="md"
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {attachments.map((att) => (
            <a
              key={att.id}
              href={att.fileUrl}
              target="_blank"
              rel="noopener noreferrer"
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                padding: '8px 12px',
                background: COLOR_PALETTE.gray50,
                border: `0.5px solid ${COLOR_PALETTE.border}`,
                borderRadius: 6,
                fontSize: 12,
                color: COLOR_PALETTE.blue600,
                textDecoration: 'none',
              }}
            >
              {/* File icon */}
              <svg
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                style={{ flexShrink: 0 }}
              >
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14 2 14 8 20 8" />
              </svg>
              <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {att.originalFileName}
              </span>
              <svg
                width="12"
                height="12"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                style={{ flexShrink: 0, color: COLOR_PALETTE.gray400 }}
              >
                <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6" />
                <polyline points="15 3 21 3 21 9" />
                <line x1="10" y1="14" x2="21" y2="3" />
              </svg>
            </a>
          ))}
        </div>
      </AppModal>
    </>
  );
}

export default AttachmentsCell;
