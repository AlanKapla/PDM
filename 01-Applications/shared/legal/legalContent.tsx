export const CONTACT_EMAIL = "kontakt@brickly.pro";
export const APP_URL = "https://app.brickly.pro";

export function PrivacyPolicyContent() {
  return (
    <div className="legal-content">
      <p className="legal-content__meta">
        Ostatnia aktualizacja: 14 lipca 2026 r. · Platforma dostępna pod adresem{" "}
        <a href={APP_URL} target="_blank" rel="noopener noreferrer">
          {APP_URL}
        </a>
      </p>

      <h3>1. Administrator danych</h3>
      <p>
        Administratorem danych osobowych jest Alan Kapla Usługi Informatyczne, NIP: 762-201-08-39
        (dalej: „Administrator”). Kontakt w sprawach ochrony danych:{" "}
        <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
      </p>

      <h3>2. Zakres dokumentu</h3>
      <p>
        Niniejsza polityka opisuje zasady przetwarzania danych w aplikacji Brickly (Project Data
        Management) — serwisie do zarządzania projektami budowlanymi i remontowymi, w tym
        kosztorysami, harmonogramami, plikami, kosztami i komunikacją zespołową.
      </p>

      <h3>3. Cele i podstawy prawne przetwarzania</h3>
      <ul>
        <li>
          Rejestracja, logowanie i świadczenie usługi (art. 6 ust. 1 lit. b RODO — wykonanie umowy
          lub działania przed jej zawarciem).
        </li>
        <li>
          Zarządzanie organizacjami, projektami, zaproszeniami i uprawnieniami (art. 6 ust. 1 lit. b
          RODO).
        </li>
        <li>Wysyłka powiadomień systemowych i zaproszeń e-mail (art. 6 ust. 1 lit. b RODO).</li>
        <li>
          Funkcje oparte na sztucznej inteligencji — odczyt dokumentów kosztowych, generowanie
          kosztorysów i harmonogramów, asystent AI — wyłącznie po inicjatywie użytkownika (art. 6
          ust. 1 lit. b RODO).
        </li>
        <li>
          Zapewnienie bezpieczeństwa, diagnostyka błędów i ochrona przed nadużyciami (art. 6 ust. 1
          lit. f RODO — prawnie uzasadniony interes Administratora).
        </li>
        <li>
          Wypełnienie obowiązków prawnych, w tym podatkowych i rachunkowych (art. 6 ust. 1 lit. c
          RODO), o ile ma zastosowanie.
        </li>
      </ul>

      <h3>4. Kategorie przetwarzanych danych</h3>
      <ul>
        <li>
          <strong>Dane konta:</strong> imię, nazwisko, adres e-mail, identyfikator konta Microsoft
          Entra External ID (Azure AD B2C Object ID).
        </li>
        <li>
          <strong>Dane profilu (opcjonalne):</strong> numer telefonu, nazwa firmy, NIP, adres.
        </li>
        <li>
          <strong>Dane organizacji i projektów:</strong> nazwy, opisy, role członków, zaproszenia,
          uprawnienia.
        </li>
        <li>
          <strong>Dane merytoryczne wprowadzone przez użytkowników:</strong> kosztorysy, koszty,
          harmonogramy prac, pliki projektowe, komentarze, wiadomości czatu, powiadomienia.
        </li>
        <li>
          <strong>Dokumenty przesyłane do analizy AI:</strong> faktury, rachunki i inne pliki
          graficzne lub dokumentowe zawierające dane kontrahentów (np. NIP, adres, kwoty).
        </li>
        <li>
          <strong>Dane techniczne:</strong> adres IP, identyfikatory sesji, logi żądań API, metadane
          plików, znaczniki czasu operacji, dane niezbędne do działania połączeń WebSocket
          (SignalR).
        </li>
      </ul>

      <h3>5. Źródło danych</h3>
      <p>
        Dane konta (e-mail, imię, nazwisko) pochodzą z procesu rejestracji i logowania przez
        Microsoft Entra External ID, w tym opcjonalnie przez dostawcę tożsamości Google
        skonfigurowany w tym systemie. Pozostałe dane pochodzą bezpośrednio od użytkowników i
        członków ich organizacji.
      </p>

      <h3>6. Odbiorcy danych i podmioty przetwarzające</h3>
      <p>
        Dane przetwarzane są na infrastrukturze Microsoft Azure w regionach europejskich (m.in.
        hosting aplikacji, baza danych, magazyn plików Blob Storage, kolejki zadań, Azure OpenAI).
        Administrator korzysta z:
      </p>
      <ul>
        <li>Microsoft Entra External ID — uwierzytelnianie użytkowników.</li>
        <li>Microsoft Azure — hosting, przechowywanie danych i usługi chmurowe.</li>
        <li>Azure OpenAI — przetwarzanie treści przekazanych do funkcji AI.</li>
        <li>Dostawca poczty SMTP — wysyłka wiadomości e-mail (zaproszenia, powiadomienia).</li>
      </ul>
      <p>
        Podmioty te działają jako podmioty przetwarzające na podstawie umów powierzenia zgodnych z
        art. 28 RODO. Microsoft Azure i Entra External ID przetwarzają dane zgodnie ze standardami
        bezpieczeństwa i umowami Microsoft na warunkach opisanych w dokumentacji Microsoft (w tym
        DPA). Administrator nie sprzedaje danych osobowych podmiotom trzecim.
      </p>

      <h3>7. Przekazywanie danych poza Europejski Obszar Gospodarczy</h3>
      <p>
        Dane są przechowywane i przetwarzane głównie w centrach danych Microsoft Azure w Europie. W
        przypadku, gdy dostawca technologii przetwarza dane poza EOG, odbywa się to wyłącznie na
        podstawie odpowiednich mechanizmów prawnych (np. standardowych klauzul umownych Komisji
        Europejskiej) lub decyzji o adekwatności.
      </p>

      <h3>8. Okres przechowywania danych</h3>
      <ul>
        <li>
          Dane konta i profilu — przez czas korzystania z Platformy oraz do 30 dni po usunięciu
          konta (okres na wykonanie kopii zapasowych i usunięcie z systemów).
        </li>
        <li>
          Dane projektów i organizacji — do momentu usunięcia przez uprawnionego użytkownika lub
          zamknięcia organizacji/projektu.
        </li>
        <li>
          Dokumenty tymczasowo przechowywane w procesie importu kosztów AI — do 30 dni, chyba że
          użytkownik zaakceptuje lub odrzuci import wcześniej.
        </li>
        <li>
          Logi techniczne — przez okres niezbędny do zapewnienia bezpieczeństwa i diagnostyki,
          zazwyczaj do 90 dni.
        </li>
        <li>
          Dane wymagane przepisami prawa (np. podatkowymi) — przez okres wynikający z przepisów, nie
          dłużej niż 5 lat od zakończenia współpracy.
        </li>
      </ul>

      <h3>9. Prawa osób, których dane dotyczą</h3>
      <p>
        Przysługuje prawo: dostępu do danych, sprostowania, usunięcia, ograniczenia przetwarzania,
        przenoszenia danych, sprzeciwu wobec przetwarzania opartego na prawnie uzasadnionym
        interesie oraz wniesienia skargi do Prezesa Urzędu Ochrony Danych Osobowych (ul. Stawki 2,
        00-193 Warszawa).
      </p>
      <p>
        Wnioski należy kierować na adres: <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
        Usunięcie konta można zgłosić e-mailem — Administrator zweryfikuje tożsamość wnioskodawcy
        przed realizacją.
      </p>

      <h3>10. Pliki cookie, localStorage i pamięć sesji</h3>
      <p>
        Strona brickly.pro nie stosuje narzędzi analitycznych ani marketingowych. Aplikacja pod
        adresem app.brickly.pro nie korzysta z Google Analytics ani podobnych narzędzi. W aplikacji
        stosowane są wyłącznie technologie niezbędne do działania serwisu:
      </p>
      <ul>
        <li>
          <strong>localStorage (MSAL):</strong> tokeny uwierzytelniające i stan sesji logowania
          Microsoft Entra External ID.
        </li>
        <li>
          <strong>localStorage (cookieConsent):</strong> zapis informacji o wyborze w banerze
          cookies.
        </li>
        <li>
          <strong>sessionStorage:</strong> preferencje sesji aplikacji (np. tryb demonstracyjny).
        </li>
        <li>
          <strong>Pliki cookie sesyjne MSAL:</strong> obsługa logowania w przeglądarkach z
          ograniczeniami pamięci lokalnej (np. Safari na iOS).
        </li>
        <li>
          <strong>Service Worker (PWA):</strong> buforowanie zasobów aplikacji w celu poprawy
          wydajności — bez śledzenia użytkownika.
        </li>
      </ul>

      <h3>11. Funkcje sztucznej inteligencji</h3>
      <p>
        Po wyraźnej akcji użytkownika (np. przesłanie dokumentu, wygenerowanie kosztorysu) treści
        mogą być przekazywane do modeli Azure OpenAI w celu automatycznego odczytu lub generowania
        treści. Użytkownik ponosi odpowiedzialność za przesyłanie dokumentów, do których ma prawo,
        oraz za dane osobowe osób trzecich zawarte w tych dokumentach. Administrator nie wykorzystuje
        danych projektowych do trenowania własnych modeli AI.
      </p>

      <h3>12. Bezpieczeństwo</h3>
      <p>
        Komunikacja z Platformą odbywa się przez szyfrowane połączenia TLS. Dane w spoczynku są
        chronione mechanizmami szyfrowania stosowanymi przez Microsoft Azure. Dostęp do danych mają
        wyłącznie uprawnieni użytkownicy w ramach przypisanych organizacji i projektów.
      </p>

      <h3>13. Zmiany polityki</h3>
      <p>
        O istotnych zmianach niniejszej polityki użytkownicy zostaną poinformowani drogą e-mailową z
        co najmniej 14-dniowym wyprzedzeniem lub poprzez komunikat w aplikacji.
      </p>
    </div>
  );
}

export function TermsOfServiceContent() {
  return (
    <div className="legal-content">
      <p className="legal-content__meta">
        Ostatnia aktualizacja: 14 lipca 2026 r. · Platforma dostępna pod adresem{" "}
        <a href={APP_URL} target="_blank" rel="noopener noreferrer">
          {APP_URL}
        </a>
      </p>

      <h3>1. Definicje</h3>
      <ul>
        <li>
          <strong>Platforma</strong> — aplikacja webowa Brickly (Project Data Management) dostępna
          pod adresem app.brickly.pro.
        </li>
        <li>
          <strong>Użytkownik</strong> — osoba fizyczna lub prawna korzystająca z Platformy po
          rejestracji.
        </li>
        <li>
          <strong>Organizacja</strong> — podmiot (tenant) tworzony w Platformie, w ramach którego
          zarządzane są projekty i członkowie zespołu.
        </li>
        <li>
          <strong>Operator</strong> — Alan Kapla Usługi Informatyczne, NIP: 762-201-08-39.
        </li>
      </ul>

      <h3>2. Postanowienia ogólne</h3>
      <p>
        Regulamin określa zasady korzystania z Platformy. Korzystanie z Platformy oznacza
        akceptację Regulaminu oraz Polityki prywatności. Platforma jest obecnie udostępniana
        nieodpłatnie w ramach wczesnego dostępu (beta). Operator zastrzega sobie prawo do
        wprowadzenia płatnych planów w przyszłości — o czym Użytkownicy zostaną poinformowani z
        odpowiednim wyprzedzeniem.
      </p>

      <h3>3. Rejestracja i konto</h3>
      <ul>
        <li>
          Rejestracja i logowanie odbywają się przez Microsoft Entra External ID (w tym opcjonalnie
          konto Google).
        </li>
        <li>Użytkownik zobowiązuje się podawać prawdziwe dane i chronić dostęp do swojego konta.</li>
        <li>Udostępnianie konta osobom nieuprawnionym jest zabronione.</li>
        <li>
          Użytkownik może edytować dane profilu w ustawieniach aplikacji. Adres e-mail jest
          powiązany z kontem tożsamości i nie podlega edycji w Platformie.
        </li>
      </ul>

      <h3>4. Organizacje, projekty i zaproszenia</h3>
      <ul>
        <li>
          Użytkownik może tworzyć organizacje, projekty oraz zapraszać innych użytkowników e-mailem.
        </li>
        <li>Administrator organizacji odpowiada za nadawanie uprawnień członkom zespołu.</li>
        <li>
          Dane wprowadzane do projektu mogą być widoczne dla innych członków projektu zgodnie z
          przypisanymi rolami.
        </li>
      </ul>

      <h3>5. Zakres usługi i dostępność</h3>
      <ul>
        <li>
          Platforma umożliwia m.in.: zarządzanie kosztorysami, kosztami, harmonogramami prac, plikami
          projektowymi, komunikacją zespołową (czat), powiadomieniami oraz funkcjami AI wspomagającymi
          pracę.
        </li>
        <li>
          W planie bezpłatnym Operator nie gwarantuje określonego poziomu dostępności (SLA).
          Platforma może być czasowo niedostępna z powodu konserwacji, aktualizacji lub awarii
          infrastruktury.
        </li>
        <li>Operator może wprowadzać zmiany funkcjonalne, w tym dodawać lub modyfikować moduły.</li>
      </ul>

      <h3>6. Licencja i własność intelektualna</h3>
      <ul>
        <li>
          Operator udziela Użytkownikowi niewyłącznej, niezbywalnej licencji na korzystanie z
          Platformy na czas trwania umowy.
        </li>
        <li>
          Użytkownik zachowuje pełne prawa do danych i treści wprowadzonych do Platformy (w tym
          kosztorysów, plików, harmonogramów).
        </li>
        <li>
          Kod źródłowy, interfejs, znaki towarowe i elementy graficzne Platformy stanowią własność
          Operatora.
        </li>
      </ul>

      <h3>7. Funkcje AI</h3>
      <ul>
        <li>
          Wyniki generowane przez AI (odczyt faktur, propozycje kosztorysów, harmonogramów,
          odpowiedzi asystenta) mają charakter pomocniczy i wymagają weryfikacji przez Użytkownika.
        </li>
        <li>
          Operator nie ponosi odpowiedzialności za decyzje biznesowe lub finansowe podjęte na
          podstawie wyników AI bez weryfikacji.
        </li>
        <li>
          Użytkownik oświadcza, że posiada prawo do przesyłania dokumentów i danych do analizy AI.
        </li>
      </ul>

      <h3>8. Zabronione działania</h3>
      <ul>
        <li>Przesyłanie treści bezprawnych, wirusów lub złośliwego oprogramowania.</li>
        <li>Próby nieautoryzowanego dostępu do systemu lub danych innych użytkowników.</li>
        <li>Automatyczne scrapowanie lub nadmierne obciążanie infrastruktury.</li>
        <li>Korzystanie z Platformy w sposób naruszający prawa osób trzecich.</li>
      </ul>

      <h3>9. Odpowiedzialność</h3>
      <ul>
        <li>
          Operator nie ponosi odpowiedzialności za treści wprowadzone przez Użytkowników ani za
          błędy wynikające z nieprawidłowych danych źródłowych.
        </li>
        <li>
          W planie bezpłatnym odpowiedzialność Operatora z tytułu szkód jest ograniczona do
          wysokości faktycznie poniesionych przez Użytkownika opłat za usługę w okresie 3 miesięcy
          poprzedzających zdarzenie — w przypadku braku opłat odpowiedzialność jest wyłączona w
          najszerszym dopuszczalnym przez prawo zakresie, z wyjątkiem szkód wyrządzonych umyślnie.
        </li>
        <li>
          Użytkownik odpowiada za działania członków swojej organizacji, którym nadał uprawnienia.
        </li>
      </ul>

      <h3>10. Dane i prywatność</h3>
      <p>
        Zasady przetwarzania danych osobowych opisuje Polityka prywatności dostępna w stopce serwisu
        oraz w aplikacji. W przypadku gdy Użytkownik wprowadza do Platformy dane osobowe swoich
        pracowników, kontrahentów lub klientów, może pełnić rolę administratora tych danych —
        wówczas ponosi odpowiedzialność za legalność ich przetwarzania i ewentualne powierzenie
        przetwarzania Operatorowi.
      </p>

      <h3>11. Rozwiązanie umowy</h3>
      <ul>
        <li>
          Użytkownik może w każdej chwili zaprzestać korzystania z Platformy i zgłosić usunięcie
          konta na adres <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
        </li>
        <li>
          Operator może zawiesić lub usunąć konto w przypadku naruszenia Regulaminu lub zagrożenia
          bezpieczeństwa Platformy.
        </li>
        <li>
          Po usunięciu konta dane są usuwane w ciągu 30 dni, z zastrzeżeniem obowiązków prawnych
          przechowywania określonych informacji.
        </li>
      </ul>

      <h3>12. Zmiany Regulaminu</h3>
      <p>
        O istotnych zmianach Regulaminu Użytkownicy zostaną poinformowani drogą e-mailową z co
        najmniej 14-dniowym wyprzedzeniem lub poprzez komunikat w aplikacji. Dalsze korzystanie z
        Platformy po wejściu zmian w życie oznacza ich akceptację.
      </p>

      <h3>13. Prawo właściwe i spory</h3>
      <p>
        Regulamin podlega prawu polskiemu. Spory będą rozstrzygane przez sąd właściwy dla siedziby
        Operatora, z zastrzeżeniem bezwzględnie obowiązujących przepisów o właściwości sądów w
        stosunku do konsumentów.
      </p>
    </div>
  );
}
