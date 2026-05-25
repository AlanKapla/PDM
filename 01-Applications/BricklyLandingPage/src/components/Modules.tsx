import './Modules.css'

const FEATURES = [
  {
    number: '01',
    tag: 'Dokumentacja projektowa',
    title: 'Centralne repozytorium dokumentacji',
    description: 'Wszystkie dokumenty projektu — plany, specyfikacje, notatki — przechowywane w jednym miejscu z pełną historią zmian i kontrolą dostępu.',
    highlights: [
      'Wersjonowanie dokumentów z podglądem historii',
      'Komentarze w kontekście konkretnych dokumentów',
      'Udostępnianie z granularną kontrolą uprawnień',
      'Bezpieczne repozytorium z logiem dostępu',
    ],
    screen: 'doc-versioning.png',
    screenAlt: 'Dokumentacja projektowa — wersjonowanie i komentarze',
  },
  {
    number: '02',
    tag: 'Dokumentacja kosztowa',
    title: 'Rejestracja i akceptacja wydatków',
    description: 'Każdy poniesiony koszt jest ewidencjonowany przez członków projektu i kierowany do akceptacji przez osoby upoważnione — z pełną ścieżką audytu.',
    highlights: [
      'Rejestracja wydatków przez uczestników projektu',
      'Dwustopniowy proces akceptacji kosztów',
      'Przypisanie kosztów do pozycji kosztorysu',
      'Historia operacji z datą i autorem',
    ],
    screen: 'cost-expenses.png',
    screenAlt: 'Dokumentacja kosztowa — wydatki i akceptacja',
  },
  {
    number: '03',
    tag: 'Kosztorysy',
    title: 'Kosztorysy oparte na szablonach',
    description: 'Kosztorysy budowane na bazie spersonalizowanych szablonów. Każda pozycja może posiadać warianty i być konstruowana z komponentów: materiały, robocizna, transport, koszty stałe.',
    highlights: [
      'Szablony kosztorysów dostosowane do organizacji',
      'Warianty pozycji kosztorysowych',
      'Budowanie pozycji z komponentów (materiał, robocizna, transport)',
      'Automatyczne przeliczenia przy zmianie parametrów',
    ],
    screen: 'estimate-templates.png',
    screenAlt: 'Kosztorysy — szablony i warianty',
  },
  {
    number: '04',
    tag: 'Harmonogram',
    title: 'Planowanie i monitorowanie realizacji',
    description: 'Harmonogram z podziałem zakresów prac na okresy realizacji. Możliwość zaznaczania postępu wykonania oraz definiowania zależności między zakresami z różnych etapów.',
    highlights: [
      'Podział zakresów prac na okresy realizacji',
      'Zaznaczanie wykonania okresu lub całego zakresu',
      'Zależności między zakresami prac z różnych etapów',
      'Automatyczne alerty o opóźnieniach',
    ],
    screen: 'schedule-periods.png',
    screenAlt: 'Harmonogram — zakresy prac i zależności',
  },
  {
    number: '05',
    tag: 'Synchronizacja',
    title: 'Integracja kosztorysów z harmonogramem',
    description: 'Automatyczne tworzenie struktury harmonogramu na podstawie danych kosztorysowych — etapy, podetapy i zakresy prac generowane bezpośrednio z pozycji kosztorysu.',
    highlights: [
      'Generowanie etapów harmonogramu z kosztorysu',
      'Tworzenie podetapów i zakresów prac',
      'Synchronizacja zmian między modułami',
      'Spójna struktura kosztowo-czasowa projektu',
    ],
    screen: 'sync-stages.png',
    screenAlt: 'Synchronizacja kosztorysu z harmonogramem',
  },
  {
    number: '06',
    tag: 'Dashboard',
    title: 'Analiza kosztowo-czasowa w czasie rzeczywistym',
    description: 'Centralny punkt zarządzania projektem: rejestracja kosztów dla pozycji kosztorysu i harmonogramu, alerty o przekroczeniach oraz wielowymiarowa analiza realizacji.',
    highlights: [
      'Dodawanie kosztów dla pozycji kosztorysu i harmonogramu',
      'Alerty o przekroczeniu kosztu lub czasu realizacji',
      'Analiza kosztowo-czasowa projektu',
      'Widok porównawczy planu z wykonaniem',
    ],
    screen: 'dashboard-costs.png',
    screenAlt: 'Dashboard — analiza kosztowo-czasowa',
  },
  {
    number: '07',
    tag: 'Komunikacja i zadania',
    title: 'Moduły zespołowe i planowanie prac',
    description: 'Wbudowany moduł komunikacji między członkami projektu oraz moduł planowania prac — zadania przypisywane uczestnikom na podstawie harmonogramu.',
    highlights: [
      'Komunikacja w kontekście projektu z historią wiadomości',
      'Zaplanowane prace dla członków zespołu',
      'Generowanie zadań na podstawie harmonogramu',
      'Powiadomienia o nowych zadaniach i zmianach',
    ],
    screen: 'communication-module.png',
    screenAlt: 'Komunikacja i zaplanowane prace',
  },
  {
    number: '08',
    tag: 'Organizacja',
    title: 'Kontrahenci i parametryzacja projektu',
    description: 'Zarządzanie bazą kontrahentów organizacji oraz pełna parametryzacja projektów — w tym waluta rozliczeń, stawki i inne konfiguracje dostosowane do potrzeb inwestycji.',
    highlights: [
      'Baza kontrahentów przypisanych do organizacji',
      'Parametryzacja projektu (waluta, stawki, konfiguracje)',
      'Możliwość indywidualnych ustawień dla każdej inwestycji',
      'Integracja z modułami kosztorysów i harmonogramów',
    ],
    screen: 'contractors-module.png',
    screenAlt: 'Kontrahenci i parametryzacja projektu',
  },
]

export default function Modules() {
  return (
    <section id="modules" className="section section--alt">
      <div className="container">
        <div className="features__header">
          <span className="section-label">Funkcjonalności</span>
          <h2 className="section-title">
            Zintegrowane moduły<br />dla każdego etapu inwestycji
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
              <div className="feature-row__right">
                <ul className="feature-row__highlights">
                  {feature.highlights.map(h => (
                    <li key={h} className="feature-row__highlight">
                      <span className="feature-row__dot" />
                      {h}
                    </li>
                  ))}
                </ul>
                <div className="feature-row__screen">
                  <img
                    src={`/screenshots/${feature.screen}`}
                    alt={feature.screenAlt}
                    className="feature-row__screen-img"
                  />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
