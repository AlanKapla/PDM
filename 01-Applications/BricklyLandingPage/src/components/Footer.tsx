import { useState, useRef } from 'react'
import { Mail, Globe, Phone, Building, Receipt, Facebook, Instagram } from 'lucide-react'
import { useScrollTo } from '../hooks/useScrollTo'
import LegalModal from './LegalModal'
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
        <h3>1. Administrator danych</h3>
        <p>
          Administratorem danych osobowych jest Alan Kapla Usługi Informatyczne,
          NIP: 762-201-08-39. Kontakt w sprawach danych osobowych:{' '}
          <a href="mailto:kontakt@brickly.pro">kontakt@brickly.pro</a>.
        </p>

        <h3>2. Cel i podstawa przetwarzania</h3>
        <ul>
          <li>Świadczenie usług platformy (art. 6 ust. 1 lit. b RODO — wykonanie umowy).</li>
          <li>Komunikacja i obsługa konta (art. 6 ust. 1 lit. b RODO).</li>
          <li>Wymagania prawne i podatkowe (art. 6 ust. 1 lit. c RODO).</li>
        </ul>

        <h3>3. Zakres przetwarzanych danych</h3>
        <ul>
          <li>Dane rejestracyjne: imię, nazwisko, adres e-mail.</li>
          <li>Dane projektów: dokumenty, kosztorysy, harmonogramy i pliki wprowadzone przez użytkowników.</li>
          <li>Dane techniczne: logi dostępu, adresy IP.</li>
        </ul>

        <h3>4. Okres przechowywania</h3>
        <p>
          Dane konta przechowywane są przez czas trwania umowy oraz przez 5 lat po jej zakończeniu
          (wymogi podatkowe). Dane projektów przechowywane są do momentu usunięcia przez
          użytkownika lub zamknięcia konta.
        </p>

        <h3>5. Prawa użytkowników</h3>
        <p>
          Każdemu użytkownikowi przysługuje prawo dostępu do danych, ich sprostowania,
          usunięcia, ograniczenia przetwarzania, przenoszenia oraz wniesienia sprzeciwu.
          Kontakt: <a href="mailto:kontakt@brickly.pro">kontakt@brickly.pro</a>.
        </p>

        <h3>6. Pliki cookie</h3>
        <p>
          Platforma używa wyłącznie cookie sesyjnych niezbędnych do prawidłowego działania serwisu.
          Żadne cookie analityczne ani marketingowe nie są stosowane bez wyraźnej zgody.
        </p>

        <h3>7. Bezpieczeństwo</h3>
        <p>
          Dane przechowywane są w infrastrukturze chmurowej z szyfrowaniem TLS podczas
          transmisji oraz szyfrowaniem danych w spoczynku.
        </p>

        <h3>8. Zmiany polityki</h3>
        <p>
          O wszelkich zmianach niniejszej polityki użytkownicy zostaną poinformowani
          drogą e-mailową z co najmniej 14-dniowym wyprzedzeniem.
        </p>
      </LegalModal>

      <LegalModal
        isOpen={termsOpen}
        onClose={() => setTermsOpen(false)}
        title="Regulamin"
        returnFocusRef={termsBtnRef}
      >
        <h3>1. Definicje</h3>
        <ul>
          <li><strong>Platforma</strong> — serwis Brickly dostępny pod adresem app.brickly.pro.</li>
          <li><strong>Użytkownik</strong> — osoba fizyczna lub prawna korzystająca z Platformy.</li>
          <li><strong>Organizacja</strong> — podmiot, w imieniu którego Użytkownik korzysta z Platformy.</li>
          <li><strong>Operator</strong> — Alan Kapla Usługi Informatyczne, NIP: 762-201-08-39.</li>
        </ul>

        <h3>2. Warunki korzystania</h3>
        <ul>
          <li>Korzystanie z Platformy wymaga rejestracji z podaniem prawdziwych danych.</li>
          <li>Użytkownik ponosi odpowiedzialność za bezpieczeństwo danych logowania.</li>
          <li>Udostępnianie konta osobom trzecim jest zabronione.</li>
        </ul>

        <h3>3. Licencja i własność intelektualna</h3>
        <ul>
          <li>Platforma udostępniana jest na zasadzie licencji niewyłącznej, niezbywalnej.</li>
          <li>Użytkownik zachowuje pełne prawa do własnych danych projektowych.</li>
          <li>Kod źródłowy i interfejs Platformy stanowią własność Operatora.</li>
        </ul>

        <h3>4. Odpowiedzialność</h3>
        <ul>
          <li>Operator nie ponosi odpowiedzialności za błędy w danych wprowadzonych przez Użytkownika.</li>
          <li>
            Operator dołoży starań, aby Platforma była dostępna całą dobę, jednak nie gwarantuje
            poziomu dostępności (SLA) w planie bezpłatnym.
          </li>
          <li>
            Maksymalna odpowiedzialność Operatora ograniczona jest do wartości 3-miesięcznej opłaty
            abonamentowej.
          </li>
        </ul>

        <h3>5. Dane i prywatność</h3>
        <p>
          Zasady przetwarzania danych osobowych opisuje Polityka prywatności dostępna w stopce serwisu.
        </p>

        <h3>6. Rozwiązanie umowy</h3>
        <ul>
          <li>Użytkownik może w każdej chwili usunąć konto z poziomu ustawień Platformy.</li>
          <li>Operator może zawiesić konto w przypadku naruszenia Regulaminu.</li>
          <li>Dane usuwane są w ciągu 30 dni od zamknięcia konta.</li>
        </ul>

        <h3>7. Zmiany Regulaminu</h3>
        <p>
          O wszelkich zmianach Regulaminu Użytkownicy zostaną poinformowani drogą e-mailową
          z co najmniej 14-dniowym wyprzedzeniem.
        </p>

        <h3>8. Prawo właściwe</h3>
        <p>
          Regulamin podlega prawu polskiemu. Wszelkie spory rozstrzygane są przed sądem
          właściwym dla siedziby Operatora.
        </p>
      </LegalModal>
    </footer>
  )
}
