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
            <span className="hero__title-accent">Twoja inwestycja?</span>{' '}
            Naprawdę?
          </h1>

          <p className="hero__subtitle">
            Większość inwestorów dowiaduje się o przekroczeniu budżetu
            za późno. Brickly pokazuje Ci stan finansowy każdej inwestycji
            w czasie rzeczywistym — zanim będzie za drogo.
          </p>

          <div className="hero__pain-strip">
            <span className="hero__pain-item">„Ile już wydaliśmy?"</span>
            <span className="hero__pain-arrow">→</span>
            <span className="hero__pain-item">„Kiedy to skończą?"</span>
            <span className="hero__pain-arrow">→</span>
            <span className="hero__pain-item">„Gdzie jest ta umowa?"</span>
          </div>

          <div className="hero__stats">
            <div className="hero__stat">
              <span className="hero__stat-value">Budżet</span>
              <span className="hero__stat-label">vs rzeczywiste wydatki</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">Harmonogram</span>
              <span className="hero__stat-label">opóźnienia widoczne od razu</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">1 miejsce</span>
              <span className="hero__stat-label">dla całego zespołu</span>
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

      <div className="hero__wave">
        <svg viewBox="0 0 1440 40" preserveAspectRatio="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M0,20 C360,40 1080,0 1440,20 L1440,40 L0,40 Z" fill="#FFFFFF" />
        </svg>
      </div>
    </section>
  )
}
