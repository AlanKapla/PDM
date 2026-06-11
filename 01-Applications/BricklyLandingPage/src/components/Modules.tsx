import { useState } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import './Modules.css'

/* ------------------------------------------------------------------ */
/*  ScreenshotGallery — karuzela zrzutów ekranu dla pojedynczego     */
/*  modułu. Przyjmuje tablicę nazw plików (bez rozszerzenia) i       */
/*  ścieżkę bazową. Zdjęcia ładowane są jako module/N.png.           */
/* ------------------------------------------------------------------ */

interface ScreenshotGalleryProps {
  /** Nazwy plików bez rozszerzenia, np. ['1', '2', '3'] */
  screens: string[]
  /** Ścieżka bazowa, np. /screenshots/module-01 */
  basePath: string
  /** Tekst alternatywny – zostanie uzupełniony o numer widoku */
  alt: string
}

function ScreenshotGallery({ screens, basePath, alt }: ScreenshotGalleryProps) {
  const [index, setIndex] = useState(0)
  const total = screens.length

  function handleGoPrev() {
    setIndex((prev) => (prev === 0 ? total - 1 : prev - 1))
  }

  function handleGoNext() {
    setIndex((prev) => (prev === total - 1 ? 0 : prev + 1))
  }

  function handleGoTo(i: number) {
    setIndex(i)
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'ArrowLeft') {
      e.preventDefault()
      handleGoPrev()
    } else if (e.key === 'ArrowRight') {
      e.preventDefault()
      handleGoNext()
    }
  }

  return (
    <div
      className="gallery"
      tabIndex={0}
      onKeyDown={handleKeyDown}
      role="region"
      aria-label={`Galeria: ${alt}`}
    >
      <div className="gallery__viewport">
        <img
          src={`${basePath}/${screens[index]}.png`}
          alt={`${alt} — widok ${index + 1} z ${total}`}
          className="gallery__img"
          loading="lazy"
        />
      </div>

      {total > 1 && (
        <>
          <button
            type="button"
            className="gallery__btn gallery__btn--prev"
            onClick={handleGoPrev}
            aria-label="Poprzedni zrzut ekranu"
          >
            <ChevronLeft size={20} aria-hidden="true" />
          </button>
          <button
            type="button"
            className="gallery__btn gallery__btn--next"
            onClick={handleGoNext}
            aria-label="Następny zrzut ekranu"
          >
            <ChevronRight size={20} aria-hidden="true" />
          </button>
          <div className="gallery__dots" role="tablist" aria-label="Wybór zrzutu ekranu">
            {screens.map((_, i) => (
              <button
                key={i}
                type="button"
                className={`gallery__dot${i === index ? ' gallery__dot--active' : ''}`}
                onClick={() => handleGoTo(i)}
                role="tab"
                aria-selected={i === index}
                aria-label={`Zrzut ekranu ${i + 1} z ${total}`}
              />
            ))}
          </div>
        </>
      )}
    </div>
  )
}

/* ------------------------------------------------------------------ */
/*  Konfiguracja modułów                                              */
/*                                                                     */
/*  screens: tablica nazw plików (bez .png) w katalogu                */
/*           public/screenshots/module-{number}/                       */
/* ------------------------------------------------------------------ */

interface Feature {
  number: string
  tag: string
  title: string
  description: string
  highlights: string[]
  screens: string[]
}

const FEATURES: Feature[] = [
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
    screens: ['1', '2', '3'],
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
    screens: ['1'],
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
    screens: ['1'],
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
    screens: ['1'],
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
    screens: ['1'],
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
    screens: ['1'],
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
    screens: ['1'],
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
    screens: ['1'],
  },
  {
    number: '09',
    tag: 'AI — Import dokumentów',
    title: 'Automatyczne rozpoznawanie faktur i paragonów',
    description: 'Zdjęcie faktury lub paragonu przesłane do platformy jest automatycznie analizowane przez AI. System rozpoznaje nazwę kosztu, kwoty netto i brutto, numer dokumentu, datę oraz dane kontrahenta. Odczytane informacje są prezentowane do weryfikacji przed zapisaniem.',
    highlights: [
      'Rozpoznawanie nazwy kosztu, kwot netto i brutto',
      'Automatyczne odczytywanie numeru faktury i daty wystawienia',
      'Wyodrębnianie danych kontrahenta: nazwa, NIP, adres',
      'Weryfikacja odczytanych danych przed zapisem',
    ],
    screens: ['1'],
  },
  {
    number: '10',
    tag: 'AI — kosztorys z opisu',
    title: 'Generowanie kosztorysu na podstawie opisu inwestycji',
    description: 'Opis inwestycji w języku naturalnym — rodzaj obiektu, standard wykończenia, budżet, metraż, lokalizacja — jest analizowany przez AI na podstawie wybranego szablonu organizacji. System generuje pełną strukturę kosztorysu z grupami, pozycjami, ilościami i cenami. Uwzględniane są mnożniki lokalizacyjne kosztów robocizny oraz prognozowana inflacja na rok zakończenia inwestycji.',
    highlights: [
      'Opis inwestycji w języku naturalnym jako dane wejściowe',
      'Automatyczny dobór mnożników lokalizacyjnych i inflacji',
      'Generowanie pełnej struktury: grupy, pozycje, komponenty',
      'Podgląd i edycja kosztorysu przed zatwierdzeniem',
    ],
    screens: ['1'],
  },
]

/* ------------------------------------------------------------------ */
/*  Modules — główny komponent sekcji                                 */
/* ------------------------------------------------------------------ */

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
                  {feature.highlights.map((h) => (
                    <li key={h} className="feature-row__highlight">
                      <span className="feature-row__dot" aria-hidden="true" />
                      {h}
                    </li>
                  ))}
                </ul>
              </div>
              <div className="feature-row__gallery">
                <ScreenshotGallery
                  screens={feature.screens}
                  basePath={`/screenshots/module-${feature.number}`}
                  alt={`${feature.tag}: ${feature.title}`}
                />
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
