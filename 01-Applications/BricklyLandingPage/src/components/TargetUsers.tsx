import { TrendingUp, ClipboardCheck, PenLine, UserCheck } from 'lucide-react'
import './TargetUsers.css'

const USERS = [
  {
    icon: ClipboardCheck,
    title: 'Nadzorca inwestycyjny',
    description: 'Prowadzisz kilka budów naraz. Pamiętasz wszystko — ale nie powinieneś musieć. Miej pełny obraz finansowy każdego projektu bez ciągłego sprawdzania.',
    perks: ['Dashboard finansowy wielu projektów', 'Alerty gdy etap przekracza budżet', 'Historia zmian i decyzji'],
  },
  {
    icon: UserCheck,
    title: 'Inwestor zastępczy',
    description: 'Działasz w imieniu inwestora. On musi Ci ufać. Brickly sprawia że masz czym udowodnić że to zaufanie jest zasłużone.',
    perks: ['Raporty jednym kliknięciem', 'Pełna dokumentacja decyzyjna', 'Kosztorys vs wydatki w czasie rzeczywistym'],
  },
  {
    icon: PenLine,
    title: 'Architekt',
    description: 'Twoja praca to nie tylko projekt — to też koordynacja ludzi, terminów i dokumentów. Miej wszystko w jednym miejscu zamiast w trzech skrzynkach mailowych.',
    perks: ['Wersjonowane repozytorium dokumentacji', 'Nadzór autorski zintegrowany z harmonogramem', 'Bezpośredni wgląd w postęp prac'],
  },
  {
    icon: TrendingUp,
    title: 'Deweloper i inwestor prywatny',
    description: 'Wkładasz pieniądze i chcesz wiedzieć na co idą. Bez cotygodniowych telefonów do wykonawcy. Brickly daje Ci wgląd bez angażowania całego zespołu.',
    perks: ['Raport stanu projektu zawsze pod ręką', 'Kosztorys vs faktury rzeczywiste', 'Dokumenty i umowy w jednym miejscu'],
  },

]

export default function TargetUsers() {
  return (
    <section id="target" className="section">
      <div className="container">
        <div className="target__header">
          <span className="section-label">Dla kogo</span>
          <h2 className="section-title">
            Dla każdego kto odpowiada<br />za pieniądze i terminy
          </h2>
          <p className="section-subtitle">
            Nieważne jak się nazywa Twoja rola na budowie.
            Jeśli ktoś pyta Cię „jak idzie?" i „ile zostało w budżecie?" —
            Brickly jest dla Ciebie.
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
