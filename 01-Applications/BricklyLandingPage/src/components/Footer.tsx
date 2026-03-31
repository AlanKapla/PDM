import { Mail, Globe, Phone, MapPin, User } from 'lucide-react'
import './Footer.css'

const FOOTER_LINKS = {
  'Platforma': [
    { label: 'O aplikacji', href: '#about' },
    { label: 'Moduły', href: '#modules' },
    { label: 'Dla kogo', href: '#target' },
    { label: 'Kontakt', href: '#contact' },
  ],
  'Moduły': [
    { label: 'Projekty', href: '#modules' },
    { label: 'Kosztorysy', href: '#modules' },
    { label: 'Harmonogram', href: '#modules' },
    { label: 'Pliki', href: '#modules' },
    { label: 'Wiadomości', href: '#modules' },
  ],
}

export default function Footer() {
  const scrollTo = (href: string) => {
    if (href.startsWith('#')) {
      document.querySelector(href)?.scrollIntoView({ behavior: 'smooth' })
    }
  }

  return (
    <footer id="contact" className="footer">
      <div className="container footer__inner">
        <div className="footer__brand">
          <a href="#" className="footer__logo" onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}>
            <span className="footer__logo-text">Brickly</span>
          </a>
          <p className="footer__tagline">
            Nowoczesna platforma do zarządzania projektami budowlanymi i remontowymi.
          </p>
          <div className="footer__contacts">
            <span className="footer__contact-link">
              <Globe size={15} />
              brickly.pro
            </span>
          </div>
        </div>

        {Object.entries(FOOTER_LINKS).map(([section, links]) => (
          <div key={section} className="footer__col">
            <h4 className="footer__col-title">{section}</h4>
            <ul className="footer__col-links">
              {links.map(link => (
                <li key={link.label}>
                  <a
                    href={link.href}
                    className="footer__link"
                    onClick={link.href.startsWith('#') ? e => { e.preventDefault(); scrollTo(link.href) } : undefined}
                  >
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>

      <div className="footer__info-strip">
        <div className="container footer__info-strip-inner">
          <div className="footer__info-block">
            <h4 className="footer__info-title">Dane firmy</h4>
            <div className="footer__info-row"><User size={13} /> BRICKLY</div>
            <div className="footer__info-row"><User size={13} /> Alan Kapla</div>
            <div className="footer__info-row"><MapPin size={13} /> ul. Klonowa 27, 07-200 Rybienko Nowe</div>
          </div>
          <div className="footer__info-divider" />
          <div className="footer__info-block">
            <h4 className="footer__info-title">Kontakt</h4>
            <a href="tel:+48798517893" className="footer__info-row footer__info-link">
              <Phone size={13} /> 798 517 893
            </a>
            <a href="mailto:kontakt@brickly.pro" className="footer__info-row footer__info-link">
              <Mail size={13} /> kontakt@brickly.pro
            </a>
          </div>
        </div>
      </div>

      <div className="footer__bottom">
        <div className="container footer__bottom-inner">
          <span>© {new Date().getFullYear()} Brickly. Wszelkie prawa zastrzeżone.</span>
          <span className="footer__made">Produkt polski 🇵🇱</span>
        </div>
      </div>
    </footer>
  )
}
