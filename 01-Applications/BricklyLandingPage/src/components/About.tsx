import { CheckCircle2 } from 'lucide-react'
import './About.css'

const BENEFITS = [
  'Zestawienie kosztorysu z wydatkami rzeczywistymi — zawsze aktualne',
  'Dashboard finansowy projektu i organizacji',
  'Harmonogram z alertami o opóźnieniach i przekroczeniach budżetu',
  'Centralne repozytorium dokumentów, plików i faktur',
  'Komunikacja w kontekście projektu — z pełną historią',
  'Raporty dla inwestora generowane automatycznie',
]

export default function About() {
  return (
    <section id="about" className="section">
      <div className="container">
        <div className="about__content">
          <span className="section-label">O platformie</span>
          <h2 className="section-title about__title">
            Pełna kontrola finansowa<br />każdej inwestycji
          </h2>
          <p className="section-subtitle about__subtitle--lead">
            Brickly to zintegrowana platforma łącząca zarządzanie dokumentacją,
            kontrolę kosztów, harmonogramowanie i komunikację zespołową.
            Zapewnia dostęp do kompletnego obrazu finansowego projektu
            w czasie rzeczywistym — bez konieczności agregowania danych
            z wielu niezależnych narzędzi.
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
      </div>
    </section>
  )
}
