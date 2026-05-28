import { useState } from 'react'
import { ChevronDown } from 'lucide-react'
import './FAQ.css'

const FAQ_ITEMS = [
  {
    question: 'Czy korzystanie z platformy Brickly jest bezpłatne?',
    answer:
      'Tak. Platforma Brickly jest dostępna bezpłatnie. Nie ma ukrytych opłat ani limitów projektów w planie podstawowym.',
  },
  {
    question: 'Jakie rodzaje projektów można prowadzić w Brickly?',
    answer:
      'Platforma obsługuje projekty budowlane i remontowe dowolnej skali — od remontów mieszkań po wieloetapowe inwestycje deweloperskie.',
  },
  {
    question: 'Czy możliwe jest połączenie z zewnętrznymi systemami, takimi jak ERP lub programy księgowe?',
    answer:
      'Brickly jest otwarte na integracje z zewnętrznymi systemami ERP, platformami zakupowymi i oprogramowaniem księgowym. Szczegółowe informacje dostępne po kontakcie z zespołem.',
  },
  {
    question: 'Kto ma dostęp do danych projektu?',
    answer:
      'Dostęp do projektu posiadają wyłącznie zaproszeni członkowie. Właściciel projektu zarządza uprawnieniami każdego uczestnika.',
  },
  {
    question: 'Czy inwestor może śledzić postęp projektu bez angażowania kierownika budowy?',
    answer:
      'Tak. Inwestor otrzymuje dedykowany widok z bieżącym stanem finansowym, harmonogramem i dokumentami — bez konieczności kontaktowania się z zespołem.',
  },
  {
    question: 'W jaki sposób Brickly chroni dane projektów?',
    answer:
      'Dane są przechowywane w infrastrukturze chmurowej z szyfrowaniem w transmisji i spoczynku. Platforma działa zgodnie z wymaganiami RODO.',
  },
  {
    question: 'Czy możliwe jest przypisanie zadań poszczególnym członkom projektu?',
    answer:
      'Tak. Moduł zadań umożliwia przypisanie zaplanowanych prac do konkretnych uczestników projektu wraz z terminem realizacji.',
  },
  {
    question: 'Czy platforma umożliwia porównanie kosztorysu z rzeczywistymi wydatkami?',
    answer:
      'Tak. Moduł kontroli kosztów zestawia pozycje kosztorysu z faktycznie poniesionymi wydatkami i generuje alerty o przekroczeniach budżetu.',
  },
]

interface FAQItemProps {
  question: string
  answer: string
}

function FAQItem({ question, answer }: FAQItemProps) {
  const [open, setOpen] = useState(false)

  return (
    <div className={`faq__item${open ? ' faq__item--open' : ''}`}>
      <button
        type="button"
        className="faq__question"
        onClick={() => setOpen(o => !o)}
        aria-expanded={open}
      >
        <span>{question}</span>
        <ChevronDown size={18} className="faq__icon" aria-hidden="true" />
      </button>
      <div className="faq__answer" hidden={!open}>
        <p>{answer}</p>
      </div>
    </div>
  )
}

export default function FAQ() {
  return (
    <section id="faq" className="section">
      <div className="container">
        <span className="section-label">Pytania i odpowiedzi</span>
        <h2 className="section-title faq__title">Najczęstsze pytania</h2>
        <div className="faq__list">
          {FAQ_ITEMS.map((item, i) => (
            <FAQItem key={i} question={item.question} answer={item.answer} />
          ))}
        </div>
      </div>
    </section>
  )
}
