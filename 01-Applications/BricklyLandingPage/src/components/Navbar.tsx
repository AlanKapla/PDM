import { useState, useEffect } from 'react'
import { Menu, X } from 'lucide-react'
import './Navbar.css'

const NAV_LINKS = [
  { label: 'O aplikacji', href: '#about' },
  { label: 'Moduły', href: '#modules' },
  { label: 'Dla kogo', href: '#target' },
]

export default function Navbar() {
  const [menuOpen, setMenuOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 24)
    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const handleNavClick = (href: string) => {
    setMenuOpen(false)
    const target = document.querySelector(href)
    target?.scrollIntoView({ behavior: 'smooth' })
  }

  return (
    <header className={`navbar${scrolled ? ' navbar--scrolled' : ''}`}>
      <div className="container navbar__inner">
        <a className="navbar__logo" href="#" onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}>
          <span className="navbar__logo-text">Brickly</span>
        </a>

        <nav className="navbar__links">
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
          className="navbar__hamburger"
          onClick={() => setMenuOpen(o => !o)}
          aria-label="Otwórz menu"
        >
          {menuOpen ? <X size={22} /> : <Menu size={22} />}
        </button>
      </div>

      {menuOpen && (
        <div className="navbar__mobile-menu">
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
            href="#contact"
            className="btn btn-primary"
            style={{ marginTop: '8px', justifyContent: 'center' }}
            onClick={e => { e.preventDefault(); handleNavClick('#contact') }}
          >
            Skontaktuj się
          </a>
        </div>
      )}
    </header>
  )
}
