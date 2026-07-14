import { useState, useEffect, useRef } from 'react'
import { ChevronLeft, ChevronRight, X, ZoomIn } from 'lucide-react'
import { SCREENSHOTS } from '../config/screenshots'
import './BrowserMockup.css'

export default function BrowserMockup() {
  const [active, setActive] = useState(0)
  const [modalOpen, setModalOpen] = useState(false)

  const total = SCREENSHOTS.length
  const next = () => setActive(i => (i + 1) % total)
  const prev = () => setActive(i => (i - 1 + total) % total)

  const closeButtonRef = useRef<HTMLButtonElement>(null)
  const viewportRef = useRef<HTMLDivElement>(null)

  // Zamknij modal na Escape, nawiguj strzałkami
  useEffect(() => {
    if (!modalOpen) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setModalOpen(false)
        viewportRef.current?.focus()
      }
      if (e.key === 'ArrowRight') setActive(i => (i + 1) % total)
      if (e.key === 'ArrowLeft') setActive(i => (i - 1 + total) % total)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [modalOpen, total])

  // Focus close button gdy modal się otwiera
  useEffect(() => {
    if (modalOpen) {
      closeButtonRef.current?.focus()
    }
  }, [modalOpen])

  if (SCREENSHOTS.length === 0) return null

  const current = SCREENSHOTS[active]

  return (
    <>
      {/* ---- Browser chrome ---- */}
      <div className="browser" role="region" aria-label="Podgląd aplikacji">
        <div className="browser__bar" aria-hidden="true">
          <div className="browser__dots">
            <span className="browser__dot browser__dot--red" />
            <span className="browser__dot browser__dot--yellow" />
            <span className="browser__dot browser__dot--green" />
          </div>
          <div className="browser__url">
            <span className="browser__url-lock">●</span>
            app.brickly.pro
          </div>
          <div className="browser__bar-right" />
        </div>

        {/* Screenshot */}
        <div
          ref={viewportRef}
          className="browser__viewport"
          role="button"
          tabIndex={0}
          aria-label={`Powiększ zrzut ekranu: ${current.label}`}
          onClick={() => setModalOpen(true)}
          onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); setModalOpen(true) } }}
        >
          <img
            src={current.src}
            alt={current.label}
            className="browser__screen"
            loading="lazy"
          />
          <div className="browser__zoom-hint" aria-hidden="true">
            <ZoomIn size={16} aria-hidden="true" /> Kliknij, aby powiększyć
          </div>
        </div>

        {/* Nawigacja i etykieta – tylko gdy >1 screena */}
        {SCREENSHOTS.length > 1 && (
          <div className="browser__footer">
            <button type="button" className="browser__nav-btn" onClick={prev} aria-label="Poprzedni zrzut ekranu">
              <ChevronLeft size={16} aria-hidden="true" />
            </button>
            <span className="browser__label" aria-live="polite" aria-atomic="true">{current.label}</span>
            <button type="button" className="browser__nav-btn" onClick={next} aria-label="Następny zrzut ekranu">
              <ChevronRight size={16} aria-hidden="true" />
            </button>
          </div>
        )}
      </div>

      {/* ---- Modal lightbox ---- */}
      {modalOpen && (
        <div
          className="lightbox"
          role="dialog"
          aria-modal="true"
          aria-label={`Zrzut ekranu: ${current.label}`}
          onClick={() => { setModalOpen(false); viewportRef.current?.focus() }}
        >
          <button
            ref={closeButtonRef}
            className="lightbox__close"
            aria-label="Zamknij podgląd"
            onClick={() => { setModalOpen(false); viewportRef.current?.focus() }}
          >
            <X size={22} aria-hidden="true" />
          </button>

          {SCREENSHOTS.length > 1 && (
            <button
              className="lightbox__nav lightbox__nav--prev"
              onClick={e => { e.stopPropagation(); prev() }}
              aria-label="Poprzedni zrzut ekranu"
            >
              <ChevronLeft size={28} aria-hidden="true" />
            </button>
          )}

          <img
            src={current.src}
            alt={current.label}
            className="lightbox__img"
            onClick={e => e.stopPropagation()}
          />

          {SCREENSHOTS.length > 1 && (
            <button
              className="lightbox__nav lightbox__nav--next"
              onClick={e => { e.stopPropagation(); next() }}
              aria-label="Następny zrzut ekranu"
            >
              <ChevronRight size={28} aria-hidden="true" />
            </button>
          )}

          <div className="lightbox__label" aria-live="polite" aria-atomic="true">{current.label}</div>
        </div>
      )}
    </>
  )
}
