// Konfiguracja screenów w sekcji Hero (i na stronie ogólnie).
// Aby dodać nowy screen:
//   1. Wrzuć plik do folderu /public/screenshots/
//   2. Dodaj wpis poniżej z nazwą pliku i podpisem

export interface Screenshot {
  src: string
  label: string
}

export const SCREENSHOTS: Screenshot[] = [
  { src: '/screenshots/1.png',       label: 'Panel główny' },
]
