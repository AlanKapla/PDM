import { useState, useEffect, useRef } from 'react'
import { Menu, X } from 'lucide-react'
import { useScrollTo } from '../hooks/useScrollTo'
import './Navbar.css'

const NAV_LINKS = [
  { label: 'O aplikacji', href: '#about' },
  { label: 'Moduły', href: '#modules' },
  { label: 'Dla kogo', href: '#target' },
  { label: 'FAQ', href: '#faq' },
  { label: 'Kontakt', href: '#footer-contact' },
]

export default function Navbar() {
  const [menuOpen, setMenuOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)
  const scrollTo = useScrollTo()
  const hamburgerRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 24)
    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  useEffect(() => {
    if (!menuOpen) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setMenuOpen(false)
        hamburgerRef.current?.focus()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [menuOpen])

  const handleNavClick = (href: string) => {
    setMenuOpen(false)
    scrollTo(href)
  }

  return (
    <header className={`navbar${scrolled ? ' navbar--scrolled' : ''}`}>
      <div className="container navbar__inner">
        <a className="navbar__logo" href="#" onClick={() => scrollTo('#')} aria-label="Brickly – strona główna">
          <img src="/logo.png" alt="" className="navbar__logo-img" aria-hidden="true" />
          <span className="sr-only">Brickly</span>
        </a>

        <nav className="navbar__links" aria-label="Nawigacja główna">
          {NAV_LINKS.map(link => (
            <a
              key={link.href}
              className="navbar__link"
              href={link.href}
              onClick={e => { e.preventDefault(); handleNavClick(link.href) }}
            >
              {link.label}
            </a>
          ))}
        </nav>

        <div className="navbar__actions">
          <a
            href="https://app.brickly.pro"
            target="_blank"
            rel="noopener noreferrer"
            className="btn btn-primary navbar__cta"
          >
            Wejdź do aplikacji
          </a>
        </div>

        <button
          ref={hamburgerRef}
          type="button"
          className="navbar__hamburger"
          onClick={() => setMenuOpen(o => !o)}
          aria-label={menuOpen ? 'Zamknij menu' : 'Otwórz menu'}
          aria-expanded={menuOpen}
          aria-controls="mobile-menu"
        >
          {menuOpen ? <X size={22} aria-hidden="true" /> : <Menu size={22} aria-hidden="true" />}
        </button>
      </div>

      {menuOpen && (
        <div id="mobile-menu" className="navbar__mobile-menu" role="dialog" aria-label="Menu nawigacyjne">
          {NAV_LINKS.map(link => (
            <a
              key={link.href}
              className="navbar__mobile-link"
              href={link.href}
              onClick={e => { e.preventDefault(); handleNavClick(link.href) }}
            >
              {link.label}
            </a>
          ))}
          <a
            href="#cta"
            className="btn btn-primary navbar__mobile-cta"
            onClick={e => { e.preventDefault(); handleNavClick('#cta') }}
          >
            Wypróbuj za darmo
          </a>
        </div>
      )}
    </header>
  )
}
