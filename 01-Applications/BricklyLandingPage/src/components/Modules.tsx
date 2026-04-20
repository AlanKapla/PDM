import {
  FolderKanban, FileText, CalendarDays, FolderOpen,
  MessageSquare, Building2, Mail, Briefcase, Calculator
} from 'lucide-react'
import './Modules.css'

const MODULES = [
  {
    icon: FolderKanban,
    title: 'Projekty',
    description: 'Centralny rejestr wszystkich projektów. Twórz, przeglądaj i zarządzaj projektami budowlanymi oraz remontowymi w jednym miejscu.',
    color: '#1A5CD8',
    bg: '#EEF3FF',
    tag: 'Podstawowy',
  },
  {
    icon: Calculator,
    title: 'Kosztorysy',
    description: 'Twórz szczegółowe kosztorysy z szablonów. Automatyczne kalkulacje, waluty, stawki VAT i udostępnianie klientom.',
    color: '#0047AB',
    bg: '#EEF3FF',
    tag: 'Finansowy',
  },
  {
    icon: CalendarDays,
    title: 'Harmonogram',
    description: 'Planuj prace w osi czasu. Przydzielaj zadania do wykonawców, śledź postępy i terminy realizacji.',
    color: '#00A8E8',
    bg: '#E0F7FA',
    tag: 'Planowanie',
  },
  {
    icon: FolderOpen,
    title: 'Pliki projektowe',
    description: 'Bezpieczne repozytorium dokumentów. Plany, umowy, faktury – z kontrolą wersji i dzieleniem z zewnętrznymi stronami.',
    color: '#0047AB',
    bg: '#EEF3FF',
    tag: 'Dokumenty',
  },
  {
    icon: MessageSquare,
    title: 'Wiadomości',
    description: 'Wbudowany komunikator do rozmów z członkami zespołu i organizacji. Komunikacja w kontekście projektu.',
    color: '#1A5CD8',
    bg: '#EEF3FF',
    tag: 'Komunikacja',
  },
  {
    icon: Building2,
    title: 'Organizacje',
    description: 'Zarządzaj wieloma firmami lub oddziałami. Pełna izolacja danych i zindywidualizowane uprawnienia dostępu.',
    color: '#003A8C',
    bg: '#EEF3FF',
    tag: 'Zarządzanie',
  },
  {
    icon: Briefcase,
    title: 'Zaplanowane prace',
    description: 'Dedykowany widok przydzielonych zadań. Każdy wykonawca widzi swoje prace, terminy i priorytety.',
    color: '#00B8D9',
    bg: '#E0F7FA',
    tag: 'Zadania',
  },
  {
    icon: FileText,
    title: 'Szablony kosztorysów',
    description: 'Twórz i zarządzaj biblioteką szablonów. Standaryzuj wyceny i przyspieszaj tworzenie nowych kosztorysów.',
    color: '#0047AB',
    bg: '#EEF3FF',
    tag: 'Szablony',
  },
  {
    icon: Mail,
    title: 'Zaproszenia',
    description: 'Zapraszaj wykonawców i klientów do projektów lub organizacji. Kontroluj kto ma dostęp i do czego.',
    color: '#1A5CD8',
    bg: '#EEF3FF',
    tag: 'Dostęp',
  },
]

export default function Modules() {
  return (
    <section id="modules" className="section section--alt">
      <div className="container">
        <div className="modules__header">
          <span className="section-label">Moduły</span>
          <h2 className="section-title">
            Wszystko czego potrzebujesz<br />w jednej platformie
          </h2>
          <p className="section-subtitle">
            Kompletny zestaw narzędzi dla nadzorców, inwestorów zastępczych
            i architektów – wszystko, czego potrzebujesz, żeby spinać wiele ekip
            bez chaosu.
          </p>
        </div>

        <div className="modules__grid">
          {MODULES.map(mod => (
            <div key={mod.title} className="module-card">
              <div className="module-card__icon" style={{ background: mod.bg, color: mod.color }}>
                <mod.icon size={24} />
              </div>
              <span className="module-card__tag" style={{ color: mod.color, background: mod.bg }}>
                {mod.tag}
              </span>
              <h3 className="module-card__title">{mod.title}</h3>
              <p className="module-card__desc">{mod.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
