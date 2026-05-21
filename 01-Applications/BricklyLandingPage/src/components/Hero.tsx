import { ArrowRight } from 'lucide-react'
import BrowserMockup from './BrowserMockup'
import { useScrollTo } from '../hooks/useScrollTo'
import './Hero.css'

export default function Hero() {
  const scrollTo = useScrollTo()

  return (
    <section className="hero">
      {/* Animated cobalt background */}
      <div className="hero__bg">
        <div className="hero__blob hero__blob--1" />
        <div className="hero__blob hero__blob--2" />
        <div className="hero__blob hero__blob--3" />
        <div className="hero__grid" />
      </div>

      <div className="container hero__container">
        <div className="hero__content">
          <div className="hero__badge">
            <span className="hero__badge-dot" />
            Koniec z chaosem na budowie. Zacznij widzieć liczby.
          </div>

          <h1 className="hero__title">
            Wiesz ile kosztuje<br />
            <span className="hero__title-accent">ta budowa?</span>
          </h1>

          <p className="hero__subtitle">
            Brickly zestawia kosztorys z rzeczywistymi wydatkami na bieżąco.
            Widzisz odchylenia zanim przekroczysz budżet — nie tydzień później.
          </p>

          <div className="hero__stats">
            <div className="hero__stat">
              <span className="hero__stat-value">87%</span>
              <span className="hero__stat-label">budów przekracza budżet</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">3×</span>
              <span className="hero__stat-label">mniej czasu na raporty</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">1</span>
              <span className="hero__stat-label">miejsce dla całego projektu</span>
            </div>
          </div>

          <div className="hero__actions">
            <a
              href="https://app.brickly.pro"
              target="_blank"
              rel="noopener noreferrer"
              className="btn hero__cta"
            >
              Wypróbuj za darmo
              <ArrowRight size={18} />
            </a>
            <a href="#about" className="btn hero__cta-secondary" onClick={e => { e.preventDefault(); scrollTo('#about') }}>
              Zobacz jak działa
            </a>
          </div>
        </div>

        <div className="hero__visual">
          <BrowserMockup />
        </div>
      </div>

    </section>
  )
}
