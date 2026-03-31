import { useState, useEffect, useCallback } from 'react'
import { X, ChevronLeft, ChevronRight, ZoomIn, Upload } from 'lucide-react'
import './Gallery.css'

export interface GalleryImage {
  src: string
  thumb?: string
  caption: string
  module: string
}

// Domyślne zdjęcia – zastąp je właściwymi screenami aplikacji.
// Umieść pliki w folderze /public/screenshots/ i zaktualizuj ścieżki poniżej.
const PLACEHOLDER_IMAGES: GalleryImage[] = [
  { src: '/screenshots/dashboard.png',        caption: 'Panel główny',                    module: 'Dashboard' },
  { src: '/screenshots/projects.png',         caption: 'Lista projektów',                  module: 'Projekty' },
  { src: '/screenshots/project-details.png',  caption: 'Szczegóły projektu',               module: 'Projekty' },
  { src: '/screenshots/cost-estimate.png',    caption: 'Kosztorys projektu',               module: 'Kosztorysy' },
  { src: '/screenshots/cost-template.png',    caption: 'Szablony kosztorysów',             module: 'Kosztorysy' },
  { src: '/screenshots/schedule.png',         caption: 'Harmonogram prac',                 module: 'Harmonogram' },
  { src: '/screenshots/files.png',            caption: 'Repozytorium plików',              module: 'Pliki' },
  { src: '/screenshots/chat.png',             caption: 'Komunikator',                      module: 'Wiadomości' },
  { src: '/screenshots/members.png',          caption: 'Zarządzanie członkami projektu',   module: 'Zespół' },
  { src: '/screenshots/tenants.png',          caption: 'Zarządzanie organizacjami',        module: 'Organizacje' },
]

const ALL_MODULES = ['Wszystkie', ...Array.from(new Set(PLACEHOLDER_IMAGES.map(i => i.module)))]

export default function Gallery() {
  const [images, setImages] = useState<GalleryImage[]>(PLACEHOLDER_IMAGES)
  const [activeModule, setActiveModule] = useState('Wszystkie')
  const [lightbox, setLightbox] = useState<number | null>(null)
  const [dragging, setDragging] = useState(false)

  const filtered = activeModule === 'Wszystkie'
    ? images
    : images.filter(img => img.module === activeModule)

  const openLightbox = (index: number) => setLightbox(index)
  const closeLightbox = () => setLightbox(null)

  const prevImage = useCallback(() => {
    setLightbox(i => i === null ? null : (i - 1 + filtered.length) % filtered.length)
  }, [filtered.length])

  const nextImage = useCallback(() => {
    setLightbox(i => i === null ? null : (i + 1) % filtered.length)
  }, [filtered.length])

  useEffect(() => {
    if (lightbox === null) return
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape')     closeLightbox()
      if (e.key === 'ArrowLeft')  prevImage()
      if (e.key === 'ArrowRight') nextImage()
    }
    document.addEventListener('keydown', handleKey)
    return () => document.removeEventListener('keydown', handleKey)
  }, [lightbox, prevImage, nextImage])

  // Drag & drop upload
  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragging(false)
    const files = Array.from(e.dataTransfer.files).filter(f => f.type.startsWith('image/'))
    files.forEach(file => {
      const url = URL.createObjectURL(file)
      const newImg: GalleryImage = {
        src: url,
        caption: file.name.replace(/\.[^.]+$/, '').replace(/[-_]/g, ' '),
        module: 'Własne',
      }
      setImages(prev => [...prev, newImg])
    })
  }

  return (
    <section id="gallery" className="section section--alt">
      <div className="container">
        <div className="gallery__header">
          <span className="section-label">Galeria</span>
          <h2 className="section-title">
            Brickly w akcji
          </h2>
          <p className="section-subtitle" style={{ marginTop: '16px', margin: '16px auto 0' }}>
            Zobacz jak wygląda platforma – od pulpitu po szczegółowe widoki modułów.
          </p>
        </div>

        {/* Filter tabs */}
        <div className="gallery__filters">
          {ALL_MODULES.map(mod => (
            <button
              key={mod}
              className={`gallery__filter${activeModule === mod ? ' gallery__filter--active' : ''}`}
              onClick={() => setActiveModule(mod)}
            >
              {mod}
            </button>
          ))}
        </div>

        {/* Drop zone info */}
        <div
          className={`gallery__dropzone${dragging ? ' gallery__dropzone--active' : ''}`}
          onDragOver={e => { e.preventDefault(); setDragging(true) }}
          onDragLeave={() => setDragging(false)}
          onDrop={handleDrop}
        >
          <Upload size={18} />
          <span>Przeciągnij i upuść screeny aplikacji, aby dodać je do galerii</span>
        </div>

        {/* Grid */}
        <div className="gallery__grid">
          {filtered.map((img, idx) => (
            <button
              key={`${img.src}-${idx}`}
              className="gallery__item"
              onClick={() => openLightbox(idx)}
              title={img.caption}
            >
              <div className="gallery__item-img-wrap">
                <img
                  src={img.src}
                  alt={img.caption}
                  className="gallery__item-img"
                  onError={e => {
                    const el = e.currentTarget
                    el.style.display = 'none'
                    const wrap = el.parentElement
                    if (wrap && !wrap.querySelector('.gallery__placeholder')) {
                      const ph = document.createElement('div')
                      ph.className = 'gallery__placeholder'
                      ph.innerHTML = `<span>📸</span><small>${img.caption}</small>`
                      wrap.appendChild(ph)
                    }
                  }}
                />
                <div className="gallery__item-overlay">
                  <ZoomIn size={24} />
                </div>
              </div>
              <div className="gallery__item-meta">
                <span className="gallery__item-module">{img.module}</span>
                <span className="gallery__item-caption">{img.caption}</span>
              </div>
            </button>
          ))}
        </div>

        {filtered.length === 0 && (
          <div className="gallery__empty">
            <p>Brak zdjęć dla tego modułu</p>
          </div>
        )}
      </div>

      {/* Lightbox */}
      {lightbox !== null && (
        <div className="lightbox" onClick={closeLightbox} role="dialog" aria-modal="true">
          <button className="lightbox__close" onClick={closeLightbox} aria-label="Zamknij">
            <X size={22} />
          </button>
          <button
            className="lightbox__nav lightbox__nav--prev"
            onClick={e => { e.stopPropagation(); prevImage() }}
            aria-label="Poprzednie"
          >
            <ChevronLeft size={28} />
          </button>
          <div className="lightbox__content" onClick={e => e.stopPropagation()}>
            <img
              src={filtered[lightbox].src}
              alt={filtered[lightbox].caption}
              className="lightbox__img"
            />
            <div className="lightbox__info">
              <span className="lightbox__module">{filtered[lightbox].module}</span>
              <span className="lightbox__caption">{filtered[lightbox].caption}</span>
              <span className="lightbox__counter">{lightbox + 1} / {filtered.length}</span>
            </div>
          </div>
          <button
            className="lightbox__nav lightbox__nav--next"
            onClick={e => { e.stopPropagation(); nextImage() }}
            aria-label="Następne"
          >
            <ChevronRight size={28} />
          </button>
        </div>
      )}
    </section>
  )
}
