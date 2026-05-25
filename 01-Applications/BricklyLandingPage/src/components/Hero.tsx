import { ArrowRight, CheckCircle2 } from 'lucide-react'
import { useScrollTo } from '../hooks/useScrollTo'
import './Hero.css'

const BENEFITS = [
  'Kosztorys zestawiony z wydatkami rzeczywistymi',
  'Dashboard finansowy projektu i organizacji',
  'Harmonogram z alertami o opóźnieniach',
  'Centralne repozytorium dokumentów i faktur',
  'Komunikacja w kontekście projektu',
  'Raporty dla inwestora generowane automatycznie',
]

export default function Hero() {
  const scrollTo = useScrollTo()

  return (
    <section id="about" className="hero">
      <div className="hero__bg">
        <div className="hero__blob hero__blob--1" />
        <div className="hero__blob hero__blob--2" />
        <div className="hero__blob hero__blob--3" />
      </div>

      <div className="container hero__container">
        <h1 className="hero__title">
          Kompleksowe zarządzanie<br />
          <span className="hero__title-accent">każdą inwestycją</span>
        </h1>

        <div className="hero__columns">
        <div className="hero__content">
          <p className="hero__subtitle">
            Brickly to zintegrowana platforma łącząca dokumentację projektową, kontrolę kosztów,
            harmonogramowanie i komunikację zespołową — zapewniająca pełny obraz finansowy
            każdej inwestycji w czasie rzeczywistym, bez konieczności korzystania z wielu niezależnych narzędzi.
          </p>

          <div className="hero__stats">
            <div className="hero__stat">
              <span className="hero__stat-value">8</span>
              <span className="hero__stat-label">zintegrowanych modułów</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">100%</span>
              <span className="hero__stat-label">bezpłatny dostęp</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">1</span>
              <span className="hero__stat-label">platforma dla całego projektu</span>
            </div>
          </div>

          <div className="hero__actions">
            <a
              href="https://app.brickly.pro"
              target="_blank"
              rel="noopener noreferrer"
              className="btn hero__cta"
            >
              Rozpocznij bezpłatnie
              <ArrowRight size={18} />
            </a>
            <a href="#modules" className="btn hero__cta-secondary" onClick={e => { e.preventDefault(); scrollTo('#modules') }}>
              Poznaj możliwości
            </a>
          </div>
        </div>

        <div className="hero__panel">
          <ul className="hero__benefits">
            {BENEFITS.map(benefit => (
              <li key={benefit} className="hero__benefit">
                <CheckCircle2 size={16} className="hero__benefit-icon" />
                <span>{benefit}</span>
              </li>
            ))}
          </ul>
        </div>
        </div>
      </div>
    </section>
  )
}
