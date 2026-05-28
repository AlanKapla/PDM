import { useEffect, useRef } from 'react'
import { createPortal } from 'react-dom'
import { X } from 'lucide-react'
import './LegalModal.css'

interface LegalModalProps {
  isOpen: boolean
  onClose: () => void
  title: string
  children: React.ReactNode
  returnFocusRef?: React.RefObject<HTMLButtonElement | null>
}

export default function LegalModal({
  isOpen,
  onClose,
  title,
  children,
  returnFocusRef,
}: LegalModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null)
  const titleId = `modal-title-${title.replace(/\s+/g, '-').toLowerCase()}`

  useEffect(() => {
    if (!isOpen) return
    dialogRef.current?.focus()
    document.body.style.overflow = 'hidden'
    return () => {
      document.body.style.overflow = ''
      returnFocusRef?.current?.focus()
    }
  }, [isOpen, returnFocusRef])

  useEffect(() => {
    if (!isOpen) return

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        onClose()
        return
      }
      if (e.key === 'Tab' && dialogRef.current) {
        const focusable = dialogRef.current.querySelectorAll<HTMLElement>(
          'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        )
        const first = focusable[0]
        const last = focusable[focusable.length - 1]
        if (e.shiftKey) {
          if (document.activeElement === first) {
            e.preventDefault()
            last.focus()
          }
        } else {
          if (document.activeElement === last) {
            e.preventDefault()
            first.focus()
          }
        }
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onClose])

  if (!isOpen) return null

  return createPortal(
    <div
      className="legal-modal__overlay"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="legal-modal__dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        ref={dialogRef}
        tabIndex={-1}
        onClick={e => e.stopPropagation()}
      >
        <div className="legal-modal__header">
          <h2 className="legal-modal__title" id={titleId}>
            {title}
          </h2>
          <button
            type="button"
            className="legal-modal__close"
            onClick={onClose}
            aria-label="Zamknij okno dialogowe"
          >
            <X size={20} aria-hidden="true" />
          </button>
        </div>
        <div className="legal-modal__body">{children}</div>
      </div>
    </div>,
    document.body
  )
}
