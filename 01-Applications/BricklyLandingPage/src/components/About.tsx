import { CheckCircle2 } from 'lucide-react'
import './About.css'

const BENEFITS = [
  'Kosztorys vs wydatki rzeczywiste — zawsze aktualny',
  'Dashboard finansowy projektu i organizacji',
  'Harmonogram z alertami o opóźnieniach',
  'Dokumenty, pliki i faktury w jednym miejscu',
  'Komunikacja w kontekście projektu — nie na WhatsAppie',
  'Raporty dla inwestora gotowe w minutę',
]

export default function About() {
  return (
    <section id="about" className="section">
      <div className="container">
        <div className="about__layout">
          <div className="about__left">
            <span className="section-label">O aplikacji</span>
            <h2 className="section-title about__title">
              Budujesz. A kto<br />pilnuje kasy?
            </h2>
            <p className="section-subtitle about__subtitle--lead">
              Każda inwestycja ma swój moment — zwykle w środku nocy —
              kiedy zastanawiasz się czy budżet jeszcze się zgadza.
              Brickly eliminuje tę niepewność. Masz pełny obraz finansowy
              projektu w każdej chwili, bez dzwonienia do kierownika.
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
            <blockquote className="about__quote">
              <p className="about__quote-text">
                „Stworzyłem Brickly bo sam byłem tym nadzorcą,
                który co piątek sklejał raport z trzech Exceli,
                WhatsAppa i skrzynki mailowej.
                Teraz robi to system — automatycznie."
              </p>
              <footer className="about__quote-author">— Założyciel, Brickly</footer>
            </blockquote>

            <div className="about__pillars-new">
              <div className="about__pillar-new">
                <span className="about__pillar-new-title">Finanse pod kontrolą</span>
                <p className="about__pillar-new-desc">Kosztorys zestawiony z wydatkami — zawsze aktualny, bez ręcznej pracy.</p>
              </div>
              <div className="about__pillar-new">
                <span className="about__pillar-new-title">Harmonogram który ostrzega</span>
                <p className="about__pillar-new-desc">Opóźnienie widoczne natychmiast — razem z wpływem na budżet.</p>
              </div>
              <div className="about__pillar-new">
                <span className="about__pillar-new-title">Jeden ekran zamiast pięciu</span>
                <p className="about__pillar-new-desc">Wykonawca widzi zadania. Inwestor widzi budżet. Ty widzisz wszystko.</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
