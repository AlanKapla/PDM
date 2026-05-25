import { ExternalLink, Mail, CheckCircle2, ShieldCheck, Plug2, MapPin } from 'lucide-react'
import './CallToAction.css'

const MICRO_BENEFITS = [
  'Bezpłatny dostęp',
  'Uruchomienie w ciągu kilku minut',
  'Bez karty kredytowej',
]

export default function CallToAction() {
  return (
    <section id="cta" className="cta-section">
      <div className="cta-section__bg">
        <div className="cta-section__blob cta-section__blob--1" />
        <div className="cta-section__blob cta-section__blob--2" />
        <div className="cta-section__grid" />
      </div>

      <div className="container cta-section__content">
        <div className="cta-section__badge">
          <span className="cta-section__badge-dot" />
          Platforma dostępna bezpłatnie
        </div>
        <h2 className="cta-section__title">
          Rozpocznij zarządzanie<br />
          <span className="cta-section__title-accent">inwestycjami profesjonalnie.</span>
        </h2>
        <p className="cta-section__subtitle">
          Bezpłatny dostęp do pełnej funkcjonalności platformy.
          Konfiguracja projektu zajmuje kilka minut.
        </p>

        <div className="cta-section__micro-benefits">
          {MICRO_BENEFITS.map(b => (
            <span key={b} className="cta-section__micro-benefit">
              <CheckCircle2 size={15} />
              {b}
            </span>
          ))}
        </div>

        <div className="cta-section__integration">
          <p className="cta-section__integration-text">
            Platforma jest otwarta na integracje z systemami zewnętrznymi —
            ERP, oprogramowanie księgowe, platformy zakupowe.
            Istnieje możliwość wdrożenia spersonalizowanych modułów
            oraz funkcji opartych na AI, dostosowanych do potrzeb organizacji.
          </p>
        </div>

        <div className="cta-section__actions">
          <a
            href="https://app.brickly.pro"
            target="_blank"
            rel="noopener noreferrer"
            className="btn cta-section__btn-main"
          >
            <ExternalLink size={18} />
            Przejdź do platformy
          </a>
          <a href="mailto:kontakt@brickly.pro" className="btn cta-section__btn-secondary">
            <Mail size={16} />
            Skontaktuj się z nami
          </a>
        </div>

        <div className="cta-section__trust">
          <span><ShieldCheck size={15} /> Bezpieczna platforma</span>
          <span className="cta-section__trust-divider" />
          <span><Plug2 size={15} /> Otwarte na integracje</span>
          <span className="cta-section__trust-divider" />
          <span><MapPin size={15} /> Produkt polski</span>
        </div>
      </div>
    </section>
  )
}
