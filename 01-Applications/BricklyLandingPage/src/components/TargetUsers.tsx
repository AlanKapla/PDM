import { HardHat, ClipboardCheck, PenLine, UserCheck } from 'lucide-react'
import './TargetUsers.css'

const USERS = [
  {
    icon: ClipboardCheck,
    title: 'Nadzorcy inwestycyjni',
    description: 'Kontroluj postępy na wielu budowach równocześnie. Weryfikuj kosztorysy, nadzoruj harmonogramy i komunikuj się z wykonawcami – wszystko w jednym miejscu.',
    perks: ['Wieloprojektowy widok nadzoru', 'Weryfikacja kosztorysów', 'Historia zmian i decyzji'],
  },
  {
    icon: UserCheck,
    title: 'Inwestorzy zastępczy',
    description: 'Działaj w imieniu inwestora z pełnym dostępem do dokumentacji. Organizuj przetargi, akceptuj kosztorysy i koordynuj pracę generalnych wykonawców.',
    perks: ['Zarządzanie wieloma ekipami', 'Akceptacja i kontrola budżetu', 'Pełna dokumentacja decyzyjna'],
  },
  {
    icon: PenLine,
    title: 'Architekci',
    description: 'Prowadź nadzór autorski i koordynuj realizację projektowanego obiektu. Udostępniaj rysunki, reaguj na RFI i miej wgląd w postęp prac na budowie.',
    perks: ['Repozytorium dokumentacji projektowej', 'Nadzór autorski w systemie', 'Komunikacja z kierownikiem budowy'],
  },
  {
    icon: HardHat,
    title: 'Generalni wykonawcy',
    description: 'Zarządzaj podwykonawcami, kosztorysami i harmonogramem z jednego miejsca. Deleguj zadania do ekip i śledź realizację każdego etapu prac.',
    perks: ['Zarządzanie podwykonawcami', 'Kosztorysy i rozliczenia', 'Harmonogram etapów'],
  },

]

export default function TargetUsers() {
  return (
    <section id="target" className="section">
      <div className="container">
        <div className="target__header">
          <span className="section-label">Dla kogo</span>
          <h2 className="section-title">
            Dla tych, którzy spinają<br />wiele ekip naraz
          </h2>
          <p className="section-subtitle">
            Brickly jest przede wszystkim dla nadzorców inwestycyjnych, inwestorów
            zastępczych, architektów i generalnych wykonawców – osób, które stoją
            w centrum procesu budowlanego i koordynują wielu uczestników jednocześnie.
          </p>
        </div>

        <div className="target__grid">
          {USERS.map(user => (
            <div key={user.title} className="target-card">
              <div className="target-card__top">
                <div className="target-card__icon">
                  <user.icon size={26} />
                </div>
                <h3 className="target-card__title">{user.title}</h3>
              </div>
              <p className="target-card__desc">{user.description}</p>
              <ul className="target-card__perks">
                {user.perks.map(perk => (
                  <li key={perk}>
                    <span className="target-card__perk-dot" />
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
