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
            Excel, WhatsApp, e-mail — koniec z tym.
          </div>

          <h1 className="hero__title">
            Jeden system<br />
            <span className="hero__title-accent">dla całego procesu</span>{' '}
            inwestycyjnego.
          </h1>

          <p className="hero__subtitle">
            Koniec z Excelem do kosztorysów, WhatsAppem do komunikacji
            i mailem do dokumentów. Brickly spina cały proces inwestycyjny
            w jednym miejscu.
          </p>

          <div className="hero__pain-strip">
            <span className="hero__pain-item">Rozproszona dokumentacja</span>
            <span className="hero__pain-arrow">→</span>
            <span className="hero__pain-item">Nieaktualne kosztorysy</span>
            <span className="hero__pain-arrow">→</span>
            <span className="hero__pain-item">Brak kontroli nad projektem</span>
          </div>

          <div className="hero__stats">
            <div className="hero__stat">
              <span className="hero__stat-value">5 min</span>
              <span className="hero__stat-label">konfiguracja</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">1 miejsce</span>
              <span className="hero__stat-label">zamiast 5 narzędzi</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">100%</span>
              <span className="hero__stat-label">online</span>
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
