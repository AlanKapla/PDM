import { TrendingUp, ClipboardCheck, PenLine, UserCheck } from 'lucide-react'
import './TargetUsers.css'

const USERS = [
  {
    icon: ClipboardCheck,
    title: 'Deweloper',
    description: 'Zarządzanie wieloma inwestycjami jednocześnie wymaga spójnego wglądu w finanse, harmonogramy i dokumentację każdego projektu. Brickly zapewnia centralny punkt kontroli bez konieczności przełączania się między narzędziami.',
    perks: ['Dashboard finansowy wielu projektów', 'Analiza porównawcza inwestycji', 'Zarządzanie zespołami i kontrahentami'],
  },
  {
    icon: UserCheck,
    title: 'Inwestor zastępczy',
    description: 'Działanie w imieniu inwestora wymaga pełnej dokumentacji decyzyjnej i przejrzystości finansowej. Brickly dostarcza kompletny ślad audytowy kosztów, akceptacji i postępu realizacji.',
    perks: ['Pełna dokumentacja decyzyjna z historią zmian', 'Raporty finansowe generowane automatycznie', 'Zestawienie kosztorysu z wydatkami rzeczywistymi'],
  },
  {
    icon: PenLine,
    title: 'Architekt',
    description: 'Prowadzenie nadzoru autorskiego nad wieloma inwestycjami wymaga sprawnego zarządzania dokumentacją techniczną i koordynacji z wykonawcami. Brickly integruje te procesy w jednym środowisku.',
    perks: ['Wersjonowane repozytorium dokumentacji technicznej', 'Nadzór autorski zintegrowany z harmonogramem', 'Komunikacja z uczestnikami projektu w jednym miejscu'],
  },
  {
    icon: TrendingUp,
    title: 'Inwestor prywatny',
    description: 'Niezależny dostęp do aktualnego stanu finansowego i postępu realizacji inwestycji — bez konieczności angażowania zespołu projektowego przy każdym zapytaniu.',
    perks: ['Raport stanu projektu dostępny w każdej chwili', 'Zestawienie kosztorysu z fakturami rzeczywistymi', 'Centralne repozytorium dokumentów i umów'],
  },
]

export default function TargetUsers() {
  return (
    <section id="target" className="section">
      <div className="container">
        <div className="target__header">
          <span className="section-label">Dla kogo</span>
          <h2 className="section-title">
            Platforma dla profesjonalistów<br />zarządzających inwestycjami
          </h2>
          <p className="section-subtitle">
            Brickly zostało zaprojektowane dla uczestników procesu inwestycyjnego,
            którzy potrzebują pełnego wglądu w finanse, harmonogram i dokumentację
            prowadzonych projektów.
          </p>
        </div>

        <div className="target__grid">
          {USERS.map(user => (
            <div key={user.title} className="target-card">
              <div className="target-card__top">
                <div className="target-card__icon" aria-hidden="true">
                  <user.icon size={26} aria-hidden="true" />
                </div>
                <h3 className="target-card__title">{user.title}</h3>
              </div>
              <p className="target-card__desc">{user.description}</p>
              <ul className="target-card__perks">
                {user.perks.map(perk => (
                  <li key={perk}>
                    <span className="target-card__perk-dot" aria-hidden="true" />
                    {perk}
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
