import {
  FolderKanban, FileText, CalendarDays, FolderOpen,
  MessageSquare, Building2, Mail, Briefcase, Calculator
} from 'lucide-react'
import './Modules.css'

const MODULES = [
  {
    icon: FolderKanban,
    title: 'Projekty',
    description: 'Centralny rejestr projektów z podsumowaniem budżetu i postępu. Jednym rzutem oka widzisz stan finansowy każdej inwestycji.',
    color: 'var(--cobalt-500)',
    bg: 'var(--cobalt-50)',
    tag: 'Podstawowy',
  },
  {
    icon: Calculator,
    title: 'Kosztorysy',
    description: 'Szablonowe kosztorysy z automatycznymi kalkulacjami VAT i walutami. Porównuj planowane koszty z rzeczywistymi wydatkami w czasie rzeczywistym.',
    color: 'var(--cobalt-600)',
    bg: 'var(--cobalt-50)',
    tag: 'Finansowy',
  },
  {
    icon: CalendarDays,
    title: 'Harmonogram',
    description: 'Oś czasu z przypisaniem wykonawców i śledzeniem opóźnień. Każde opóźnienie etapu widoczne natychmiast — razem z wpływem na budżet.',
    color: 'var(--accent)',
    bg: 'var(--accent-light)',
    tag: 'Planowanie',
  },
  {
    icon: FolderOpen,
    title: 'Pliki projektowe',
    description: 'Bezpieczne repozytorium dokumentów z kontrolą wersji. Plany, umowy i faktury zawsze pod ręką — dla właściwych osób.',
    color: 'var(--cobalt-600)',
    bg: 'var(--cobalt-50)',
    tag: 'Dokumenty',
  },
  {
    icon: MessageSquare,
    title: 'Wiadomości',
    description: 'Wbudowany komunikator w kontekście projektu. Koniec z WhatsAppem i mailem — wszystkie ustalenia zostają w systemie.',
    color: 'var(--cobalt-500)',
    bg: 'var(--cobalt-50)',
    tag: 'Komunikacja',
  },
  {
    icon: Building2,
    title: 'Organizacje',
    description: 'Wielofirmowa struktura z pełną izolacją danych. Każda organizacja widzi tylko swoje projekty i finanse.',
    color: 'var(--cobalt-700)',
    bg: 'var(--cobalt-50)',
    tag: 'Zarządzanie',
  },
  {
    icon: Briefcase,
    title: 'Zaplanowane prace',
    description: 'Dedykowany widok zadań dla każdego wykonawcy. Terminy, priorytety i postępy — bez zbędnych telefonów.',
    color: 'var(--accent)',
    bg: 'var(--accent-light)',
    tag: 'Zadania',
  },
  {
    icon: FileText,
    title: 'Szablony kosztorysów',
    description: 'Biblioteka szablonów wycen dla Twojej branży. Standaryzuj kosztorysy i twórz nowe w minuty zamiast godzin.',
    color: 'var(--cobalt-600)',
    bg: 'var(--cobalt-50)',
    tag: 'Szablony',
  },
  {
    icon: Mail,
    title: 'Zaproszenia',
    description: 'Zapraszaj wykonawców, podwykonawców i klientów. Każdy widzi dokładnie to co powinien — nic więcej, nic mniej.',
    color: 'var(--cobalt-500)',
    bg: 'var(--cobalt-50)',
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
            Wszystko czego potrzebujesz.<br />Nic czego nie potrzebujesz.
          </h2>
          <p className="section-subtitle">
            Każdy moduł robi jedną rzecz dobrze — i łączy się z pozostałymi.
            Kosztorys zna harmonogram. Harmonogram zna wydatki.
            Ty znasz sytuację.
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
