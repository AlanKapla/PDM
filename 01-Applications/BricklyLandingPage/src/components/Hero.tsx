import { ArrowRight } from 'lucide-react'
import BrowserMockup from './BrowserMockup'
import './Hero.css'

export default function Hero() {
  const scrollTo = (href: string) => {
    document.querySelector(href)?.scrollIntoView({ behavior: 'smooth' })
  }

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
            Platforma dla branży budowlanej
          </div>

          <h1 className="hero__title">
            Jeden system<br />
            <span className="hero__title-accent">dla całego procesu</span>{' '}
            inwestycyjnego.
          </h1>

          <p className="hero__subtitle">
            Kosztorysy, harmonogramy, dokumenty i komunikacja
            z wykonawcami – wszystko w jednym miejscu,
            dostępnym z każdego urządzenia.
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
              <span className="hero__stat-value">10+</span>
              <span className="hero__stat-label">modułów</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">100%</span>
              <span className="hero__stat-label">online</span>
            </div>
            <div className="hero__stat-divider" />
            <div className="hero__stat">
              <span className="hero__stat-value">0</span>
              <span className="hero__stat-label">papierów</span>
            </div>
          </div>

          <div className="hero__actions">
            <a
              href="https://app.brickly.com.pl"
              target="_blank"
              rel="noopener noreferrer"
              className="btn hero__cta"
            >
              Wypróbuj za darmo
              <ArrowRight size={18} />
            </a>
            <button className="btn hero__cta-secondary" onClick={() => scrollTo('#about')}>
              Zobacz jak działa
            </button>
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
