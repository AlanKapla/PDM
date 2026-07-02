# Focus prompts — wstrzykiwane do universal_extraction_agent

Format: bloki `## FOCUS:` / `## FOCUS_B:` (wiele nagłówków = ten sam prompt), treść wieloliniowa, separator `---`.

**Wyjście zawsze: JSON typu `FloorPlanDrawing`** (camelCase). Sekcje SCHEMA poniżej wskazują które pola wypełnić per typ rysunku.

| Typ rysunku | Główne sekcje FloorPlanDrawing |
|---|---|
| rzut kondygnacji | `totalAreaM2`, `rooms[]`, `walls[]`, `openings[]`, `columns[]`, `beams[]`, `lintels[]`, `externalDimensions` |
| rzut fundamentów | `foundations{footings[],pads[],concreteClass,steelSpecification,coverageMm,foundationLevelM,foundationWall}` |
| zbrojenie stropu | `floors{bars[],totalMassKg,coverageDescription,slabs[]}` |
| więźba dachowa | `roof{timberGroups[],totalVolumeM3,woodClass,pitchDegrees,coveringType}` |
| przekrój | `section{levels,floorZones[],roofZones[],ringBeam,purlin,wallPlate}` |
| elewacja | `elevation{title,finishes[],levels}` + `openings[]` |
| detale | `details[]` |
| zagospodarowanie | `site{plotAreaM2,buildingFootprintM2,...}` |

---

## FOCUS: rzut_parteru
## FOCUS: rzut_piętra
## FOCUS: rzut_poddasza
## FOCUS: rzut_piwnicy

Analizujesz rzut architektoniczny kondygnacji.
Poziom kondygnacji odczytaj z tytułu rysunku.

KROK 1 — Zestawienie pomieszczeń (tabela po prawej stronie)
KRYTYCZNE — numery pomieszczeń:
Numer odczytaj z tabeli DOKŁADNIE jak wydrukowany na rysunku.
"11" to nie jest "1" ani "01", "111" to nie jest "11".
NIE normalizuj, NIE przeliczaj, NIE zamieniaj na sekwencję 01, 02, 03.

Odczytaj KAŻDY wiersz: numer, nazwa, powierzchnia [m2].
Przypisz kategorię: komunikacja | sanitarne | usługowe | mieszkalne | gospodarcze.
Uwagi przy pomieszczeniu (np. antresola) → pole `notes`.
Podana wartość → przepisz, NIE obliczaj.
Łączna powierzchnia kondygnacji pod tabelą → przepisz.
Uwagi pod tabelą (np. „liczona do 180cm”) → `areaNotes`.

KROK 2 — Elementy konstrukcyjne
Słupy: "Słup S-X" z wymiarami przekroju
Podciągi: "Podciąg P-X" z wymiarami
Nadproża: "Nadproże N-X" przy otworach

KROK 3 — Ściany
Odczytaj grubości z ciągów wymiarowych.
Każdy unikalny typ i grubość = osobny wpis.
Wypełnij `walls.internal` przez typy: wewnętrzna nośna → loadBearing, działowa → partition.

KROK 4 — Wymiary zewnętrzne z ciągów wymiarowych.

KROK 5 — Otwory
Policz i zgrupuj po symbolu (O1, D1...).
Drzwi wewnętrzne: `isInterior: true`, `count` = liczba na rzucie.

KROK 6 — Instalacje z opisów na rysunku
Wentylacja/rekuperacja, wod-kan, ogrzewanie (kotłownia + numer pomieszczenia) → `installations`.

KROK 7 — Stolarka wewnętrzna (szacunek)
`interiorDoors: [{type, floor, countEstimated}]` — policz drzwi wewnętrzne na rzucie.

SCHEMA (FloorPlanDrawing):
{
  "totalAreaM2": null,
  "areaNotes": null,
  "rooms": [{"number": "11", "name": "Wiatrołap", "areaM2": 8.37, "category": "komunikacja", "notes": null}],
  "externalDimensions": {"widthCm": null, "lengthCm": null},
  "walls": [
    {"type": "zewnętrzna", "thicknessCm": 44,
     "layers": [{"material": "beton komórkowy odm.500", "thicknessCm": 24},
                {"material": "styropian EPS", "thicknessCm": 20}]},
    {"type": "wewnętrzna nośna", "thicknessCm": 24},
    {"type": "działowa", "thicknessCm": 12}
  ],
  "columns": [{"symbol": "Słup S-1", "bCm": null, "hCm": null, "reinforcement": null}],
  "beams": [{"symbol": "Podciąg P-1", "bCm": null, "hCm": null}],
  "lintels": [{"symbol": "Nadproże N-1", "bwCm": null, "hCm": null}],
  "openings": [
    {"type": "okno", "symbol": "O1", "widthCm": null, "heightCm": null, "count": null, "location": null},
    {"type": "drzwi", "symbol": "D1", "widthCm": null, "heightCm": null, "count": null, "isInterior": false}
  ],
  "interiorDoors": [{"type": "Drzwi wewnętrzne", "floor": "Parter", "countEstimated": 10}],
  "installations": {
    "ventilation": {"type": "Wentylacja mechaniczna (rekuperacja)", "notes": "wg odrębnego opracowania"},
    "heating": {"type": "Kotłownia własna", "roomNumber": "110", "areaM2": 6.21, "notes": null}
  }
}

---

## FOCUS_B: rzut_parteru
## FOCUS_B: rzut_piętra
## FOCUS_B: rzut_poddasza
## FOCUS_B: rzut_piwnicy

Agent B — niezależna weryfikacja sum tabel i spójności liczb.
NIE czytaj w odwrotnej kolejności — zweryfikuj sumy.

KROK 1 — Zestawienie pomieszczeń
Odczytaj tabelę pomieszczeń i policz sumę powierzchni wierszy.
Porównaj z `totalAreaM2` pod tabelą. Różnica > 1% → sprawdź ponownie.

KROK 2 — Otwory (okna, drzwi, bramy garażowe)
Policz i zgrupuj po symbolach.

KROK 3 — Ściany i grubości.

KROK 4 — Elementy konstrukcyjne (słupy, podciągi, nadproża).

KROK 5 — Wymiary zewnętrzne.

SCHEMA: identyczny jak FOCUS: rzut_parteru

---

## FOCUS: rzut_fundamentow

KROK 1 — Blok tekstowy na dole (POZIOM POSADOWIENIA) — OBOWIĄZKOWE POLA
Zawsze wypełnij: `concreteClass`, `steelSpecification`, `coverageMm`, `foundationLevelM`.
Klasa betonu, gatunek stali, poziom posadowienia, otulina — przepisz dosłownie z rysunku.

KROK 2 — Ławy fundamentowe
Dla każdej ławy odczytaj:
- symbol (Ł-1, Ł-2...)
- szerokość B [m] i wysokość H [m] z etykiety typu "Ława fundamentowa Ł-1 / 0.600 m / 0.400 m"
- długość odcinka [m] z łańcucha wymiarowego przy tym odcinku
- identyfikator odcinka (np. "ściana N", "poprzeczna oś 2")
Jeśli jeden typ ławy ma wiele odcinków → `segments: [{id, lengthM}]`
Suma długości segmentów = całkowita długość ławy tego typu.

KROK 3 — Stopy fundamentowe
Format: "Stopa fund. / B m / L m / H m"

KROK 4 — Słupy (symbol + wymiary).

SCHEMA (FloorPlanDrawing — sekcja foundations):
{
  "foundations": {
    "concreteClass": "C20/25 (B25)",
    "steelSpecification": "BST500s Ø10,12 i St3S Ø6,8",
    "coverageMm": 50,
    "foundationLevelM": -1.0,
    "footings": [
      {"symbol": "Ł-1", "widthM": 0.60, "heightM": 0.40,
       "segments": [
         {"id": "ściana N", "lengthM": 8.62},
         {"id": "ściana S", "lengthM": 8.62}
       ]},
      {"symbol": "Ł-2", "widthM": 0.70, "heightM": 0.40,
       "segments": [
         {"id": "ściana E", "lengthM": 6.10}
       ]}
    ],
    "pads": [
      {"symbol": "Stopa S1", "bM": 1.300, "lM": 1.000, "heightM": 0.450, "count": 2}
    ],
    "foundationWall": {"material": "bloczek betonowy", "thicknessCm": 24}
  },
  "columns": [{"symbol": "Słup S-1", "bCm": 24, "hCm": 24}]
}

---

## FOCUS_B: rzut_fundamentow

Agent B — weryfikacja sum długości segmentów ław i spójności z wymiarami na rysunku.

KROK 1 — Ławy fundamentowe
Dla każdej ławy odczytaj segmenty i policz sumę `lengthM`.
Porównaj sumę segmentów z wymiarami na rysunku. Różnica > 1% → sprawdź ponownie.

KROK 2 — Stopy fundamentowe.
KROK 3 — Blok tekstowy (beton, stal, poziom, otulina).
KROK 4 — Słupy.

SCHEMA: identyczny jak FOCUS: rzut_fundamentow

---

## FOCUS: zbrojenie_stropu_dolne

Tabela "Lista prętów - kształty gięcia" = JEDYNE źródło danych.
To jest zbrojenie DOLNE (bottom) — zapisuj w sekcji `floors`.

KROK 1 — Opis pod tytułem
Grubość stropu, klasa betonu, opis siatki (`basicGrid`), informacja jakiego stropu dotyczy.

KROK 2 — Tabela "Lista prętów"
Kolumny: Poz. | Szt. | Ø [mm] | Długość poj. [m] | Kształt | Długość całk. [m] | Masa [kg]
Odczytaj KAŻDY wiersz dokładnie — bez ucięcia listy.

KROK 3 — Masa całkowita z dołu tabeli → `totalMassKg` (wiersz "Masa całkowita").

KROK 4 — Weryfikacja: suma mas ≈ totalMassKg. Różnica > 1% → sprawdź.

SCHEMA (FloorPlanDrawing — sekcja floors):
{
  "floors": {
    "coverageDescription": "Strop nad parterem",
    "basicGrid": null,
    "bars": [
      {"pos": 1, "count": null, "diameterMm": null, "lengthM": null,
       "totalLengthM": null, "massKg": null}
    ],
    "totalMassKg": null,
    "notes": null,
    "slabs": [{"thicknessCm": null, "concreteClass": null}]
  }
}

---

## FOCUS: zbrojenie_stropu_gorne

Tabela "Lista prętów - kształty gięcia" = JEDYNE źródło danych.
To jest zbrojenie GÓRNE (top) — zapisuj w sekcji `floors` (ten sam kształt JSON co dolne, ale dane z arkusza K-03 / górne).

KROK 1 — Opis pod tytułem: grubość, beton, `basicGrid`.
KROK 2 — Tabela prętów: odczytaj KAŻDY wiersz bez ucięcia.
KROK 3 — `totalMassKg` z wiersza "Masa całkowita" na dole tabeli.
KROK 4 — Weryfikacja sumy mas (tolerancja 1%).

SCHEMA: identyczny jak FOCUS: zbrojenie_stropu_dolne (`floors.bars[]`, `totalMassKg`, `basicGrid`).

---

## FOCUS_B: zbrojenie_stropu_dolne

Agent B — weryfikacja sum tabeli prętów.
Policz sumę `massKg` wszystkich wierszy i porównaj z `totalMassKg` z dołu tabeli.
Różnica > 1% → sprawdź ponownie. NIE czytaj wierszy w odwrotnej kolejności.

---

## FOCUS_B: zbrojenie_stropu_gorne

Agent B — weryfikacja sum tabeli prętów.
Policz sumę `massKg` wszystkich wierszy i porównaj z `totalMassKg` z dołu tabeli.
Różnica > 1% → sprawdź ponownie. NIE czytaj wierszy w odwrotnej kolejności.

---

## FOCUS: rzut_wiezby_dachowej

Tabela "Lista drewna" w prawym górnym rogu = JEDYNE źródło danych o drewnie.

UWAGA: Pierwsza grupa w tabeli to zawsze "Krokwie dachu nad garażem" przekrój 8/20 — nie pomijaj jej nawet jeśli jest krótsza od głównej.

KRYTYCZNE — KOMPLETNOŚĆ `rows[]`:
Krokwie dachu głównego (np. 10x20) mają często ponad 20 wierszy.
Musisz przepisać KAŻDY wiersz każdej grupy bez wyjątku.
Jeśli zabraknie miejsca w odpowiedzi — pomiń mniej istotne pola (`notes`, `pitchDegrees`),
ale `timberGroups[].rows[]` musi być kompletne.

STRUKTURA TABELI — KRYTYCZNE:
Jeden typ elementu = WIELE wierszy (jeden na każdą unikalną długość).
Suma mb i Objętość = tylko RAZ, na końcu każdej grupy.

KROK 1 — Przekroje
Ukośnik "/" = separator: 8/20 → "8x20", 14/14 → "14x14".
BEZWZGLĘDNY ZAKAZ wpisywania z pamięci (5x15, 15x15, 8x16 = ZABRONIONE).

KROK 2 — Dla każdej grupy odczytaj każdy wiersz
Przepisz: count, lengthM, rowSumMb.
Przepisz groupSumMb i groupVolumeM3 z końca grupy.

KROK 3 — Suma całkowita "Sumo:" z dołu tabeli → przepisz.

KROK 4 — Weryfikacja: suma objętości grup ≈ totalVolumeM3.

SCHEMA (FloorPlanDrawing — sekcja roof):
{
  "roof": {
    "woodClass": "C24",
    "pitchDegrees": 35,
    "coveringType": "dachówka",
    "notes": "DODAĆ ZAPAS DO KAŻDEGO ELEMENTU OK. 15-25CM",
    "totalVolumeM3": 16.42,
    "timberGroups": [
      {
        "name": "Krokwie dachu nad garażem",
        "section": "8x20",
        "rows": [
          {"count": 2, "lengthM": 1.27, "rowSumMb": 2.54}
        ],
        "groupSumMb": 68.81,
        "groupVolumeM3": 1.101
      }
    ]
  }
}

---

## FOCUS_B: rzut_wiezby_dachowej

Agent B — weryfikacja sum objętości drewna.
Policz sumę `groupVolumeM3` wszystkich grup i porównaj z `totalVolumeM3` z dołu tabeli.
Różnica > 1% → sprawdź ponownie. NIE czytaj grup w odwrotnej kolejności.

---

## FOCUS: przekroj

KROK 1 — Rzędne poziomów (±0.00, poziomy stropów, kalenicy, posadowienia)

KROK 2 — Warstwy podłogi (legendy A, B...)
Każda litera = strefa z listą warstw i grubościami.

KROK 3 — Warstwy dachu (legendy C, D, E, F...)

KROK 4 — Wieniec, murłata, płatew, ściana kolankowa z wymiarami.
KROK 5 — Wieńce żelbetowe (nad parterem, działowe, szczytowe) → `ringBeams[]`.
KROK 6 — Izolacja termiczna z legend warstw → `thermalInsulation[]`.

Grubość styropianu: odczytaj z legendy (np. "STYROPIAN EPS 100 10cm").
NIE obliczaj jako: grubość całkowita ściany − grubość muru.

SCHEMA (FloorPlanDrawing — sekcja section):
{
  "section": {
    "levels": {
      "foundationBottomM": -1.32,
      "groundFloorM": 0.00,
      "ceilingM": 2.88,
      "ridgeM": 8.62
    },
    "floorZones": [
      {"zone": "A — salon, pokoje", "sourceDrawing": "A-05", "layers": [
        {"material": "gres/klepka", "thicknessCm": 2},
        {"material": "szlichta betonowa", "thicknessCm": 5}
      ]}
    ],
    "roofZones": [
      {"zone": "D — dach od wewnątrz", "layers": [
        {"material": "dachówka", "thicknessCm": null},
        {"material": "krokwie 10x20cm", "thicknessCm": 20}
      ]}
    ],
    "collarWall": {
      "thicknessCm": 24, "heightCm": 115,
      "timber": {"section": "14x14", "material": "drewno C24"},
      "ringBeam": {"widthCm": 20, "heightCm": 25, "reinforcement": "wieniec żelbetowy stężający"}
    },
    "ringBeams": [
      {"location": "nad parterem", "widthCm": 24, "heightCm": 25, "reinforcement": "4#12 + strzemiona"}
    ],
    "thermalInsulation": [
      {"element": "Ściany zewnętrzne", "material": "Styropian EPS 100", "thicknessCm": 10, "system": "ETICS"},
      {"element": "Dach — między krokwiami", "material": "Wełna mineralna", "thicknessCm": 18, "notes": "min. 18cm"}
    ],
    "ringBeam": {"widthCm": 24, "heightCm": 25},
    "purlin": {"widthCm": 14, "heightCm": 18},
    "wallPlate": {"widthCm": 14, "heightCm": 14}
  }
}

---

## FOCUS: elewacja

KROK 1 — Nazwa elewacji z tytułu.
KROK 2 — Wykończenia z legendy (prostokątne próbki z opisami).
Deduplikuj: ten sam material+kolor na wielu elewacjach = jeden wpis (scalenie w raporcie końcowym).
KROK 3 — Rzędne poziomów.
KROK 4 — Otwory (policz, wymiary tylko jeśli podane, `location` = nazwa elewacji).

SCHEMA (FloorPlanDrawing — sekcja elevation + openings):
{
  "elevation": {
    "title": "Elewacja frontowa (NE)",
    "finishes": [
      {"zone": "ściany", "material": "tynk elewacyjny", "color": "biały"},
      {"zone": "cokół", "material": "tynk mozaikowy", "color": "szary"}
    ],
    "levels": {"groundFloor": 0.00, "windowTop": 2.35, "ridge": 8.62}
  },
  "openings": [
    {"type": "okno", "count": null, "location": "elewacja NE"},
    {"type": "brama garażowa", "count": 1, "location": "elewacja NE"}
  ]
}

---

## FOCUS: detale_konstrukcyjne

Zidentyfikuj każdy detal, odczytaj nazwę i zbrojenie.

SCHEMA (FloorPlanDrawing — sekcja details):
{
  "details": [
    {
      "title": "Zbrojenie słupów żelbetowych w ścianie kolankowej",
      "reinforcement": "4Ø12 pręty podłużne, zakład dl=30 średnic, pręt startowy 4Ø12"
    },
    {"title": "Połączenie ław wewnętrznych", "reinforcement": "Pręt łącznikowy naroża Ø12, zakład 45cm"},
    {"title": "Połączenie naroża ław i wieńcy", "reinforcement": "Pręt łącznikowy naroża Ø12, zakład 45cm"}
  ]
}

---

## FOCUS: zagospodarowanie_terenu

Odczytaj zestawienie powierzchni z prawej strony rysunku.
Odczytaj przyłącza z legendy/opisu (wod-kan, elektro, szambo) → sekcja `installations` jest OBOWIĄZKOWA gdy dane są na rysunku.

SCHEMA (FloorPlanDrawing — sekcja site):
{
  "site": {
    "plotAreaM2": 720,
    "buildingFootprintM2": 194.55,
    "pavedAreaM2": 93.5,
    "greenAreaM2": 431.95,
    "buildingVolumeM3": 1350,
    "buildingCoverageRatio": 0.27
  },
  "installations": {
    "plumbing": {
      "sewage": {"type": "Szambo jednokomorowe"},
      "waterSupply": {"type": "Przyłącze wodociągowe W", "notes": "wg odrębnego opracowania"}
    },
    "electrical": {"type": "Przyłącze elektroenergetyczne E", "notes": "wg odrębnego opracowania"}
  }
}

---

## FOCUS: aksonometria_wiezby

Rysunek 3D poglądowy — brak tabel z danymi.

SCHEMA (FloorPlanDrawing):
{
  "deferredDetails": [
    {"topic": "aksonometria", "notes": "Rysunek poglądowy 3D — dane liczbowe odczytywać z rzutu więźby"}
  ]
}

---

## FOCUS: rzut_dachu

KROK 1 — Połacie dachu (wymiary, kąty nachylenia).
KROK 2 — Okna połaciowe — policz i zgrupuj po symbolach; wypełnij `widthCm`, `heightCm`.
KROK 3 — Obróbki i pokrycie dachu z legendy lub opisu.
KROK 4 — Kominek wentylacyjny, rynny → `roof.ventilation`, `roof.drainage` (OBOWIĄZKOWE gdy widoczne).

SCHEMA (FloorPlanDrawing — sekcja roof + openings):
{
  "roof": {
    "pitchDegrees": null,
    "coveringType": null,
    "areaM2": null,
    "ventilation": {"type": "Kominek wentylacyjny", "count": 1},
    "drainage": {"downpipeDiameterMm": 100, "minSlopePct": 0.5, "notes": "Rynny mocować ze spadkiem min. 0.5%"}
  },
  "openings": [
    {"type": "okno połaciowe", "symbol": null, "widthCm": null, "heightCm": null, "count": null, "location": "dach główny"}
  ]
}

---

## FOCUS: default

Odczytaj wszystkie widoczne wymiary, materiały i tabele z obrazu i classificationContext.
Zwróć JSON zgodny ze schematem dla rozpoznanego typu rysunku.

## FOCUS_B: default

Zweryfikuj tabele i opisy z geometrią rysunku. Priorytet: spójność symboli i liczb.
