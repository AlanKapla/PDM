import { CheckCircle2, Zap, Shield, Users, Lightbulb } from 'lucide-react'
import './About.css'

const PILLARS = [
  {
    icon: Zap,
    title: 'Finanse pod kontrolą',
    description: 'Kosztorys to nie tylko wycena — to punkt odniesienia. Brickly zestawia go z rzeczywistymi wydatkami na bieżąco, żebyś wiedział czy projekt jest rentowny.',
  },
  {
    icon: Users,
    title: 'Harmonogram który ma znaczenie',
    description: 'Opóźnienie tygodnia to nie tylko problem czasowy. To dodatkowe koszty, przestoje, nerwowe telefony. Widzisz to zanim się wydarzy.',
  },
  {
    icon: Shield,
    title: 'Każdy wie co ma robić',
    description: 'Wykonawca widzi swoje zadania. Inwestor widzi budżet. Ty widzisz wszystko. Nikt nie pyta o to samo dwa razy.',
  },
]

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
            <p className="section-subtitle about__subtitle--body">
              Jedna platforma dla każdego kto prowadzi inwestycję —
              prywatnego inwestora, nadzorcy, architekta, dewelopera.
              Każdy widzi to co ważne. Ty widzisz wszystko.
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
                <div className="about__highlight-icon">
                  <Lightbulb size={20} />
                </div>
                <p className="about__highlight-text">
                  „Stworzyłem Brickly bo sam byłem tym nadzorcą,
                  który co piątek sklejał raport z trzech Exceli,
                  WhatsAppa i skrzynki mailowej.
                  Teraz robi to system — automatycznie."
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
