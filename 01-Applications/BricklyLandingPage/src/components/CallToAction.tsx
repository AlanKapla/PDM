import { ExternalLink, Mail, CheckCircle2 } from 'lucide-react'
import './CallToAction.css'

const MICRO_BENEFITS = [
  'Bezpłatny dostęp na start',
  'Gotowy do pracy w 5 minut',
  'Bez karty kredytowej',
]

export default function CallToAction() {
  return (
    <section id="contact" className="cta-section">
      <div className="cta-section__bg">
        <div className="cta-section__blob cta-section__blob--1" />
        <div className="cta-section__blob cta-section__blob--2" />
        <div className="cta-section__grid" />
      </div>

      <div className="container cta-section__content">
        <div className="cta-section__badge">
          <span className="cta-section__badge-dot" />
          Zacznij już dziś
        </div>
        <h2 className="cta-section__title">
          Miej pełną kontrolę<br />
          <span className="cta-section__title-accent">nad każdą inwestycją.</span>
        </h2>
        <p className="cta-section__subtitle">
          Nadzorcy inwestycyjni, inwestorzy zastępczy, architekci i generalni
          wykonawcy zyskują przejrzystość i spokój – dokumenty, kosztorysy
          i komunikacja zawsze pod ręką, w jednym miejscu.
        </p>

        <div className="cta-section__micro-benefits">
          {MICRO_BENEFITS.map(b => (
            <span key={b} className="cta-section__micro-benefit">
              <CheckCircle2 size={15} />
              {b}
            </span>
          ))}
        </div>

        <div className="cta-section__actions">
          <a
            href="https://app.brickly.pro"
            target="_blank"
            rel="noopener noreferrer"
            className="btn cta-section__btn-main"
          >
            <ExternalLink size={18} />
            Wejdź do aplikacji
          </a>
          <a href="mailto:kontakt@brickly.pro" className="btn cta-section__btn-secondary">
            <Mail size={16} />
            Napisz do nas
          </a>
        </div>

        <div className="cta-section__trust">
          <span>🔒 Bezpieczna platforma</span>
          <span className="cta-section__trust-divider" />
          <span>☁️ Dostęp z każdego urządzenia</span>
          <span className="cta-section__trust-divider" />
          <span>🇵🇱 Produkt polski</span>
        </div>
      </div>
    </section>
  )
}
