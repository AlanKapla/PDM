import { useState, useEffect } from 'react'
import { ChevronLeft, ChevronRight, X, ZoomIn } from 'lucide-react'
import { SCREENSHOTS } from '../config/screenshots'
import './BrowserMockup.css'

export default function BrowserMockup() {
  const [active, setActive] = useState(0)
  const [modalOpen, setModalOpen] = useState(false)

  const next = () => setActive(i => (i + 1) % SCREENSHOTS.length)
  const prev = () => setActive(i => (i - 1 + SCREENSHOTS.length) % SCREENSHOTS.length)

  // Zamknij modal na Escape
  useEffect(() => {
    if (!modalOpen) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setModalOpen(false)
      if (e.key === 'ArrowRight') next()
      if (e.key === 'ArrowLeft') prev()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [modalOpen])

  if (SCREENSHOTS.length === 0) return null

  const current = SCREENSHOTS[active]

  return (
    <>
      {/* ---- Browser chrome ---- */}
      <div className="browser">
        <div className="browser__bar">
          <div className="browser__dots">
            <span className="browser__dot browser__dot--red" />
            <span className="browser__dot browser__dot--yellow" />
            <span className="browser__dot browser__dot--green" />
          </div>
          <div className="browser__url">
            <span className="browser__url-lock">🔒</span>
            app.brickly.pro
          </div>
          <div className="browser__bar-right" />
        </div>

        {/* Screenshot */}
        <div className="browser__viewport" onClick={() => setModalOpen(true)}>
          <img
            src={current.src}
            alt={current.label}
            className="browser__screen"
          />
          <div className="browser__zoom-hint">
            <ZoomIn size={16} /> Kliknij, aby powiększyć
          </div>
        </div>

        {/* Nawigacja i etykieta – tylko gdy >1 screena */}
        {SCREENSHOTS.length > 1 && (
          <div className="browser__footer">
            <button className="browser__nav-btn" onClick={prev} aria-label="Poprzedni">
              <ChevronLeft size={16} />
            </button>
            <span className="browser__label">{current.label}</span>
            <button className="browser__nav-btn" onClick={next} aria-label="Następny">
              <ChevronRight size={16} />
            </button>
          </div>
        )}
      </div>

      {/* ---- Modal lightbox ---- */}
      {modalOpen && (
        <div className="lightbox" onClick={() => setModalOpen(false)}>
          <button className="lightbox__close" aria-label="Zamknij">
            <X size={22} />
          </button>

          {SCREENSHOTS.length > 1 && (
            <button
              className="lightbox__nav lightbox__nav--prev"
              onClick={e => { e.stopPropagation(); prev() }}
              aria-label="Poprzedni"
            >
              <ChevronLeft size={28} />
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
              aria-label="Następny"
            >
              <ChevronRight size={28} />
            </button>
          )}

          <div className="lightbox__label">{current.label}</div>
        </div>
      )}
    </>
  )
}
