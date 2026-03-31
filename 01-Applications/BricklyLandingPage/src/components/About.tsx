import { CheckCircle2, Zap, Shield, Users } from 'lucide-react'
import './About.css'

const PILLARS = [
  {
    icon: Zap,
    title: 'Jedno narzędzie zamiast dziesięciu',
    description: 'Koniec z Excel do kosztorysów, WhatsApp do komunikacji i e-mail do dokumentów. Wszystko w jednym miejscu.',
  },
  {
    icon: Users,
    title: 'Każdy uczestnik w jednym systemie',
    description: 'Zaproś wykonawców, podwykonawców, architektów i klientów. Każdy widzi dokładnie to, do czego ma dostęp.',
  },
  {
    icon: Shield,
    title: 'Pełna kontrola i bezpieczeństwo',
    description: 'Izolowane środowiska dla każdej organizacji. Twoje dane są tylko Twoje – z pełną kontrolą uprawnień.',
  },
]

const BENEFITS = [
  'Kosztorysy z szablonami i automatycznymi kalkulacjami',
  'Harmonogramowanie prac z widokiem osi czasu',
  'Wymiana dokumentów i plików projektowych',
  'Komunikator i powiadomienia w czasie rzeczywistym',
  'Wieloorganizacyjna struktura dostępu',
  'Śledzenie postępów i przydzielonych zadań',
]

export default function About() {
  return (
    <section id="about" className="section">
      <div className="container">
        <div className="about__layout">
          <div className="about__left">
            <span className="section-label">O aplikacji</span>
            <h2 className="section-title" style={{ marginBottom: '16px' }}>
              Budujesz na Excelu<br />i WhatsAppie?
            </h2>
            <p className="section-subtitle" style={{ marginBottom: '16px', color: 'var(--cobalt-600)', fontWeight: 600 }}>
              Większość nadzorców i wykonawców traci godziny tygodniowo na szukanie
              pliku, odświeżanie arkusza i odpowiadanie na tę samą wiadomość
              w trzech miejscach naraz.
            </p>
            <p className="section-subtitle" style={{ marginBottom: '32px' }}>
              Brickly rozwiązuje ten problem. Jedna platforma dla nadzorców
              inwestycyjnych, inwestorów zastępczych, architektów i generalnych
              wykonawców – wszystko, czego potrzebujesz do prowadzenia inwestycji.
            </p>

            <ul className="about__benefits">
              {BENEFITS.map(benefit => (
                <li key={benefit} className="about__benefit">
                  <CheckCircle2 size={18} className="about__benefit-icon" />
                  <span>{benefit}</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="about__right">
            {PILLARS.map(pillar => (
              <div key={pillar.title} className="about__pillar">
                <div className="about__pillar-icon">
                  <pillar.icon size={22} />
                </div>
                <div>
                  <h3 className="about__pillar-title">{pillar.title}</h3>
                  <p className="about__pillar-desc">{pillar.description}</p>
                </div>
              </div>
            ))}

            <div className="about__highlight">
              <div className="about__highlight-inner">
                <p className="about__highlight-text">
                  „Ile razy szukałeś kosztorysu, który ktoś wysłał mailem
                  trzy tygodnie temu? W Brickly wszystko jest na swoim miejscu –
                  zawsze i dla właściwych osób."
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
