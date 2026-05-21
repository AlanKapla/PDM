import './Modules.css'

const FEATURES = [
  {
    number: '01',
    tag: 'Finanse',
    title: 'Kosztorys kontra rzeczywistość',
    description: 'Każda faktura zestawiana z planem na bieżąco. Widzisz odchylenie od budżetu zanim przekroczysz limit — nie na koniec miesiąca.',
    highlights: [
      'Budżet vs wydatki w czasie rzeczywistym',
      'Szablonowe kosztorysy z automatycznym VAT',
      'Dashboard finansowy projektu i organizacji',
      'Eksport raportów jednym kliknięciem',
    ],
  },
  {
    number: '02',
    tag: 'Harmonogram',
    title: 'Opóźnienia widoczne zanim staną się kosztowne',
    description: 'Harmonogram połączony z budżetem. Każde przesunięcie etapu przelicza skutki finansowe automatycznie.',
    highlights: [
      'Oś czasu z etapami i wykonawcami',
      'Alerty o opóźnieniach i przekroczeniach budżetu',
      'Dedykowany widok zadań dla wykonawcy',
      'Wpływ opóźnień na koszty widoczny od razu',
    ],
  },
  {
    number: '03',
    tag: 'Dokumentacja',
    title: 'Jeden adres dla każdego dokumentu',
    description: 'Umowy, faktury, plany, zdjęcia — wszystko w projekcie, nie w skrzynce mailowej. Każdy widzi dokładnie to co powinien.',
    highlights: [
      'Bezpieczne repozytorium z kontrolą dostępu',
      'Wersjonowanie dokumentacji i plików',
      'Komunikator w kontekście projektu',
      'Zaproszenia dla wykonawców z ograniczonym dostępem',
    ],
  },
]

export default function Modules() {
  return (
    <section id="modules" className="section section--alt">
      <div className="container">
        <div className="features__header">
          <span className="section-label">Jak to działa</span>
          <h2 className="section-title">
            Trzy problemy.<br />Jedno narzędzie.
          </h2>
        </div>

        <div className="features__list">
          {FEATURES.map((feature) => (
            <div key={feature.number} className="feature-row">
              <div className="feature-row__number">{feature.number}</div>
              <div className="feature-row__left">
                <span className="feature-row__tag">{feature.tag}</span>
                <h3 className="feature-row__title">{feature.title}</h3>
                <p className="feature-row__desc">{feature.description}</p>
              </div>
              <ul className="feature-row__highlights">
                {feature.highlights.map(h => (
                  <li key={h} className="feature-row__highlight">
                    <span className="feature-row__dot" />
                    {h}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
