import { useState, useRef } from 'react'
import { Mail, Globe, Phone, Building, Receipt, Facebook, Instagram } from 'lucide-react'
import { useScrollTo } from '../hooks/useScrollTo'
import LegalModal from './LegalModal'
import { PrivacyPolicyContent, TermsOfServiceContent } from '@pdm-shared/legal/legalContent'
import '@pdm-shared/legal/legalContent.css'
import './Footer.css'

const FOOTER_LINKS = {
  'Platforma': [
    { label: 'O aplikacji', href: '#about' },
    { label: 'Moduły', href: '#modules' },
    { label: 'Dla kogo', href: '#target' },
    { label: 'Wypróbuj za darmo', href: '#cta' },
  ],
  'Moduły': [
    { label: 'Projekty', href: '#modules' },
    { label: 'Kosztorysy', href: '#modules' },
    { label: 'Harmonogram', href: '#modules' },
    { label: 'Pliki', href: '#modules' },
    { label: 'Wiadomości', href: '#modules' },
  ],
}

const SOCIAL_LINKS = [
  {
    label: 'Facebook',
    href: 'https://www.facebook.com/brickly.pro',
    icon: Facebook,
  },
  {
    label: 'Instagram',
    href: 'https://www.instagram.com/brickly.pro',
    icon: Instagram,
  },
]

export default function Footer() {
  const scrollTo = useScrollTo()
  const [privacyOpen, setPrivacyOpen] = useState(false)
  const [termsOpen, setTermsOpen] = useState(false)
  const privacyBtnRef = useRef<HTMLButtonElement>(null)
  const termsBtnRef = useRef<HTMLButtonElement>(null)

  return (
    <footer className="footer">
      <div className="container footer__inner">
        <div className="footer__brand">
          <a href="#" className="footer__logo" onClick={() => scrollTo('#')}>
            <img src="/logo.png" alt="Brickly" className="footer__logo-img" />
          </a>
          <p className="footer__tagline">
            Nowoczesna platforma do zarządzania projektami budowlanymi i remontowymi.
          </p>
          <div className="footer__contacts">
            <a
              href="https://brickly.pro"
              target="_blank"
              rel="noopener noreferrer"
              className="footer__contact-link"
            >
              <Globe size={15} aria-hidden="true" />
              brickly.pro
            </a>
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

      <div id="footer-contact" className="footer__info-strip">
        <div className="container footer__info-strip-inner">

          <div className="footer__info-block">
            <h4 className="footer__info-title">Dane firmy</h4>
            <div className="footer__info-row"><Building size={13} aria-hidden="true" /> Alan Kapla Usługi Informatyczne</div>
            <div className="footer__info-row"><Receipt size={13} aria-hidden="true" /> NIP: 762-201-08-39</div>
          </div>

          <div className="footer__info-divider" />

          <div className="footer__info-block">
            <h4 className="footer__info-title">Kontakt</h4>
            <a href="tel:+48798517893" className="footer__info-row footer__info-link">
              <Phone size={13} aria-hidden="true" /> 798 517 893
            </a>
            <a href="mailto:kontakt@brickly.pro" className="footer__info-row footer__info-link">
              <Mail size={13} aria-hidden="true" /> kontakt@brickly.pro
            </a>
          </div>

        </div>
      </div>

      <div className="footer__bottom">
        <div className="container footer__bottom-inner">
          <span>© {new Date().getFullYear()} Brickly. Wszelkie prawa zastrzeżone.</span>
          <div className="footer__legal-links">
            <button
              ref={privacyBtnRef}
              type="button"
              className="footer__legal-link"
              onClick={() => setPrivacyOpen(true)}
            >
              Polityka prywatności
            </button>
            <span className="footer__legal-sep" aria-hidden="true">·</span>
            <button
              ref={termsBtnRef}
              type="button"
              className="footer__legal-link"
              onClick={() => setTermsOpen(true)}
            >
              Regulamin
            </button>
          </div>
          <div className="footer__social" aria-label="Media społecznościowe">
            {SOCIAL_LINKS.map(social => (
              <a
                key={social.label}
                href={social.href}
                target="_blank"
                rel="noopener noreferrer"
                className="footer__social-link"
                aria-label={`Brickly na ${social.label}`}
              >
                <social.icon size={18} aria-hidden="true" />
                <span>{social.label}</span>
              </a>
            ))}
          </div>
          <span className="footer__made">Produkt polski 🇵🇱</span>
        </div>
      </div>

      <LegalModal
        isOpen={privacyOpen}
        onClose={() => setPrivacyOpen(false)}
        title="Polityka prywatności"
        returnFocusRef={privacyBtnRef}
      >
        <PrivacyPolicyContent />
      </LegalModal>

      <LegalModal
        isOpen={termsOpen}
        onClose={() => setTermsOpen(false)}
        title="Regulamin"
        returnFocusRef={termsBtnRef}
      >
        <TermsOfServiceContent />
      </LegalModal>
    </footer>
  )
}
