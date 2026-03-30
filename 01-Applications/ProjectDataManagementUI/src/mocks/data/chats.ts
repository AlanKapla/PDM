/**
 * Dane demonstracyjne czatów i wiadomości.
 * Rozmowy między członkami projektów ArchPlan Sp. z o.o.
 */

import type {
  ChatWeb,
  MessageWeb,
  ProjectContactsGroupWeb,
} from "../../types/chat.types";
import { DEMO_USERS, DEMO_TENANT_ID } from "./users";
import { DEMO_PROJECT_IDS } from "./projects";

// ===== IDENTYFIKATORY =====

export const DEMO_CHAT_IDS = {
  dmAnnaPiotr:   "chat-dm-anna-piotr",
  dmAnnaMarta:   "chat-dm-anna-marta",
  dmAnnaTomasz:  "chat-dm-anna-tomasz",
  grpBiurowe:    "chat-grp-biurowe",
  grpHotel:      "chat-grp-hotel",
} as const;

// ===== HELPER =====

const msg = (
  id: string,
  chatId: string,
  sender: (typeof DEMO_USERS)[keyof typeof DEMO_USERS],
  content: string,
  sentAt: string,
  replyToMessageId: string | null = null
): MessageWeb => ({
  id,
  chatId,
  senderId: sender.userId,
  senderFirstName: sender.firstName,
  senderLastName: sender.lastName,
  content,
  isDeleted: false,
  isEdited: false,
  sentAt,
  editedAt: null,
  replyToMessageId,
});

// ===== WIADOMOŚCI =====

export const DEMO_MESSAGES: Record<string, MessageWeb[]> = {

  // ── Anna ↔ Piotr ──────────────────────────────────────────────────────
  [DEMO_CHAT_IDS.dmAnnaPiotr]: [
    msg("msg-ap-001", DEMO_CHAT_IDS.dmAnnaPiotr, DEMO_USERS.anna,  "Cześć Piotr, sprawdziłeś już ostatnie kosztorysy dla Centrum Biurowego?", "2024-06-10T08:12:00Z"),
    msg("msg-ap-002", DEMO_CHAT_IDS.dmAnnaPiotr, DEMO_USERS.piotr, "Tak, właśnie przeglądałem. Mam kilka uwag do pozycji 3.2 – elewacja. Wartości wydają się zawyżone o ok. 12%.", "2024-06-10T08:15:00Z"),
    msg("msg-ap-003", DEMO_CHAT_IDS.dmAnnaPiotr, DEMO_USERS.anna,  "Widziałam to. Dostawca materiałów zmienił cennik w marcu. Zaktualizuję tabelę dziś po południu.", "2024-06-10T08:17:00Z"),
    msg("msg-ap-004", DEMO_CHAT_IDS.dmAnnaPiotr, DEMO_USERS.piotr, "Okej, dzięki. Czy masz już harmonogram spotkania z inwestorem?", "2024-06-10T08:20:00Z"),
    msg("msg-ap-005", DEMO_CHAT_IDS.dmAnnaPiotr, DEMO_USERS.anna,  "Planujemy na 18 czerwca, godz. 11:00 w biurze przy Marszałkowskiej.", "2024-06-10T08:22:00Z"),
    msg("msg-ap-006", DEMO_CHAT_IDS.dmAnnaPiotr, DEMO_USERS.piotr, "Zarezerwuję termin. Przygotuję prezentację etapu II na to spotkanie.", "2024-06-10T08:24:00Z"),
    msg("msg-ap-007", DEMO_CHAT_IDS.dmAnnaPiotr, DEMO_USERS.anna,  "Super, daj mi wersję roboczą do 15-go – przejrzę przed wysłaniem inwestorowi.", "2024-06-10T08:26:00Z"),
  ],

  // ── Anna ↔ Marta ──────────────────────────────────────────────────────
  [DEMO_CHAT_IDS.dmAnnaMarta]: [
    msg("msg-am-001", DEMO_CHAT_IDS.dmAnnaMarta, DEMO_USERS.marta, "Aniu, wrzuciłam nowe wizualizacje do paczki 'Wizualizacje 3D'. Możesz rzucić okiem?", "2024-05-22T13:05:00Z"),
    msg("msg-am-002", DEMO_CHAT_IDS.dmAnnaMarta, DEMO_USERS.anna,  "Już patrzę – elewacja północna wygląda świetnie! Tylko kolorystyka wejścia głównego chyba trochę ciemna?", "2024-05-22T13:18:00Z"),
    msg("msg-am-003", DEMO_CHAT_IDS.dmAnnaMarta, DEMO_USERS.marta, "Masz rację, poprawię jasność i wyślę nową wersję. Jasny granit zamiast ciemnego?", "2024-05-22T13:22:00Z"),
    msg("msg-am-004", DEMO_CHAT_IDS.dmAnnaMarta, DEMO_USERS.anna,  "Dokładnie – taki jak w projekcie A4 z Krakowa. Będzie spójne z identyfikacją inwestora.", "2024-05-22T13:25:00Z"),
    msg("msg-am-005", DEMO_CHAT_IDS.dmAnnaMarta, DEMO_USERS.marta, "Rozumiem. Wprowadzę zmiany do jutra rano.", "2024-05-22T13:27:00Z"),
    msg("msg-am-006", DEMO_CHAT_IDS.dmAnnaMarta, DEMO_USERS.anna,  "Dziękuję, Marta 🙂 Bardzo dobra robota z atrium – to zdjęcie będzie na pierwszy slajd prezentacji.", "2024-05-22T13:30:00Z"),
  ],

  // ── Anna ↔ Tomasz ─────────────────────────────────────────────────────
  [DEMO_CHAT_IDS.dmAnnaTomasz]: [
    msg("msg-at-001", DEMO_CHAT_IDS.dmAnnaTomasz, DEMO_USERS.tomasz, "Ania, mam pytanie co do wytyczenia geodezyjnego – czy mam zaczekać na aktualizację mapy BDOT?", "2024-01-15T09:30:00Z"),
    msg("msg-at-002", DEMO_CHAT_IDS.dmAnnaTomasz, DEMO_USERS.anna,   "Nie, możesz startować z aktualną. BDOT aktualizujemy dopiero w etapie II.", "2024-01-15T09:35:00Z"),
    msg("msg-at-003", DEMO_CHAT_IDS.dmAnnaTomasz, DEMO_USERS.tomasz, "OK, dziękuję. Geodeta będzie na miejscu w piątek rano.", "2024-01-15T09:37:00Z"),
    msg("msg-at-004", DEMO_CHAT_IDS.dmAnnaTomasz, DEMO_USERS.anna,   "Dobrze. Po wytyczeniu wrzuć protokół do paczki 'Dokumentacja etap I'.", "2024-01-15T09:40:00Z"),
  ],

  // ── Grupa: Centrum Biurowe ────────────────────────────────────────────
  [DEMO_CHAT_IDS.grpBiurowe]: [
    msg("msg-gb-001", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.anna,   "Witajcie w kanale projektu Centrum Biurowe Nowa Brama 👋 Tu będę koordynować całą komunikację.", "2023-02-01T09:05:00Z"),
    msg("msg-gb-002", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.piotr,  "Dzięki za zaproszenie. Zapoznałem się z dokumentacją etapu I – pytanie: kiedy rusza harmonogram?", "2023-02-01T09:10:00Z"),
    msg("msg-gb-003", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.anna,   "Harmonogram uruchomię dziś do południa. Proszę wszystkich o weryfikację przypisanych zadań.", "2023-02-01T09:13:00Z"),
    msg("msg-gb-004", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.marta,  "Rozumiem. Mam pytanie o wizualizacje – czy zakres obejmuje też wnętrza holu głównego?", "2023-02-01T09:20:00Z"),
    msg("msg-gb-005", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.anna,   "Tak, hol główny i atrium wewnętrzne. Marta, wyślę Ci brief z inwestora do końca dnia.", "2023-02-01T09:22:00Z"),
    msg("msg-gb-006", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.tomasz, "Też jestem. Kiedy planujemy pierwsze wejście geodety?", "2023-02-02T08:00:00Z"),
    msg("msg-gb-007", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.anna,   "Tomasz – wytyczenie planujemy na 8 stycznia. Szczegóły w harmonogramie.", "2023-02-02T08:05:00Z"),
    msg("msg-gb-008", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.piotr,  "Zaktualizowałem tabelę kosztorysową – sekcja 'Roboty ziemne'. Możecie sprawdzić przed poniedziałkiem?", "2024-01-20T16:30:00Z"),
    msg("msg-gb-009", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.marta,  "Widzę. Pozycja 2.4 (odwodnienie) – czy uwzględniamy pompowanie czy grawitację?", "2024-01-20T16:45:00Z"),
    msg("msg-gb-010", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.piotr,  "Pompowanie – grunt nienośny na 3.2m. Tomasz potwierdził w raporcie geodezyjnym.", "2024-01-20T16:50:00Z"),
    msg("msg-gb-011", DEMO_CHAT_IDS.grpBiurowe, DEMO_USERS.anna,   "Wszystko się zgadza. Kosztorys zatwierdzam – możemy wysłać inwestorowi w poniedziałek.", "2024-01-21T07:55:00Z"),
  ],

  // ── Grupa: Hotel Panorama ────────────────────────────────────────────
  [DEMO_CHAT_IDS.grpHotel]: [
    msg("msg-gh-001", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.piotr,    "Witam wszystkich w projekcie Hotel Panorama – Rozbudowa Skrzydła B. Zapraszam do aktywnej komunikacji 🏗", "2023-08-20T11:10:00Z"),
    msg("msg-gh-002", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.anna,     "Dziękuję za zaproszenie. Przejrzałam projekt wstępny – mam pytanie o powiązanie ze skrzydłem A.", "2023-08-20T11:15:00Z"),
    msg("msg-gh-003", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.piotr,    "Łącznik na poziomie -1 i 0. Szczegóły w projekcie architektonicznym v1.2 – już wrzuciłem do plików.", "2023-08-20T11:18:00Z"),
    msg("msg-gh-004", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.marta,    "Zacznę od wizualizacji łącznika. Kiedy mam deadline na pierwszą wersję?", "2023-08-20T11:22:00Z"),
    msg("msg-gh-005", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.piotr,    "15 września będzie OK.", "2023-08-20T11:24:00Z"),
    msg("msg-gh-006", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.tomasz,   "Pytanie o fundament – roboty ziemne zaplanowane na 2 października. Czy skrzynia szalunkowa jest już zamówiona?", "2023-09-25T14:00:00Z"),
    msg("msg-gh-007", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.piotr,    "Tak, zamówiono 18 września. Dostawa 28-go.", "2023-09-25T14:05:00Z"),
    msg("msg-gh-008", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.katarzyna,"Oglądałam dokumentację projektu – wszystko czytelne. Mam dostęp do pliku z pozwoleniem?", "2023-10-02T10:30:00Z"),
    msg("msg-gh-009", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.anna,     "Katarzyna – udostępniłam Ci paczkę 'Dokumentacja projektowa'. Pozwolenie jest w środku.", "2023-10-02T10:35:00Z"),
    msg("msg-gh-010", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.katarzyna,"Widzę, dziękuję 😊", "2023-10-02T10:37:00Z"),
    msg("msg-gh-011", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.piotr,    "UWAGA: dostawca stali opóźnia dostawę o 4 tygodnie. Perforacja harmonogramu – proszę Tomka i Martę o rewizję etapu II.", "2024-02-14T09:05:00Z"),
    msg("msg-gh-012", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.marta,    "Rozumiem. Przestawiam swoje zadania – stropy i klatki przesuniemy o miesiąc.", "2024-02-14T09:12:00Z"),
    msg("msg-gh-013", DEMO_CHAT_IDS.grpHotel, DEMO_USERS.tomasz,   "Już aktualizuję harmonogram. Nowy termin szkieletu: do 10 czerwca.", "2024-02-14T09:15:00Z"),
  ],
};

// ===== CZATY =====

export const DEMO_CHATS: ChatWeb[] = [
  {
    id: DEMO_CHAT_IDS.grpBiurowe,
    name: "Centrum Biurowe Nowa Brama",
    isGroupChat: true,
    projectId: DEMO_PROJECT_IDS.biurowe,
    tenantId: DEMO_TENANT_ID,
    createdAt: "2023-02-01T09:05:00Z",
    createdByUserId: DEMO_USERS.anna.userId,
    unreadCount: 0,
    lastMessage: DEMO_MESSAGES[DEMO_CHAT_IDS.grpBiurowe].at(-1) ?? null,
    members: [
      { userId: DEMO_USERS.anna.userId,      firstName: "Anna",      lastName: "Kowalska",  joinedAt: "2023-02-01T09:05:00Z", isAdmin: true,  lastReadAt: "2024-01-21T08:00:00Z" },
      { userId: DEMO_USERS.piotr.userId,     firstName: "Piotr",     lastName: "Wiśniewski",joinedAt: "2023-02-01T09:05:00Z", isAdmin: false, lastReadAt: "2024-01-21T07:00:00Z" },
      { userId: DEMO_USERS.marta.userId,     firstName: "Marta",     lastName: "Nowak",     joinedAt: "2023-02-01T09:05:00Z", isAdmin: false, lastReadAt: "2024-01-21T06:00:00Z" },
      { userId: DEMO_USERS.tomasz.userId,    firstName: "Tomasz",    lastName: "Zając",     joinedAt: "2023-02-01T09:05:00Z", isAdmin: false, lastReadAt: "2024-01-20T17:00:00Z" },
    ],
  },
  {
    id: DEMO_CHAT_IDS.grpHotel,
    name: "Hotel Panorama – Skrzydło B",
    isGroupChat: true,
    projectId: DEMO_PROJECT_IDS.hotel,
    tenantId: DEMO_TENANT_ID,
    createdAt: "2023-08-20T11:10:00Z",
    createdByUserId: DEMO_USERS.piotr.userId,
    unreadCount: 2,
    lastMessage: DEMO_MESSAGES[DEMO_CHAT_IDS.grpHotel].at(-1) ?? null,
    members: [
      { userId: DEMO_USERS.piotr.userId,     firstName: "Piotr",     lastName: "Wiśniewski", joinedAt: "2023-08-20T11:10:00Z", isAdmin: true,  lastReadAt: "2024-02-14T09:20:00Z" },
      { userId: DEMO_USERS.anna.userId,      firstName: "Anna",      lastName: "Kowalska",   joinedAt: "2023-08-20T11:10:00Z", isAdmin: false, lastReadAt: "2024-02-14T09:10:00Z" },
      { userId: DEMO_USERS.marta.userId,     firstName: "Marta",     lastName: "Nowak",      joinedAt: "2023-09-01T10:00:00Z", isAdmin: false, lastReadAt: "2024-02-14T09:15:00Z" },
      { userId: DEMO_USERS.tomasz.userId,    firstName: "Tomasz",    lastName: "Zając",      joinedAt: "2023-09-05T09:00:00Z", isAdmin: false, lastReadAt: "2024-02-14T09:16:00Z" },
      { userId: DEMO_USERS.katarzyna.userId, firstName: "Katarzyna", lastName: "Wójcik",     joinedAt: "2023-10-01T08:00:00Z", isAdmin: false, lastReadAt: "2023-10-02T10:38:00Z" },
    ],
  },
  {
    id: DEMO_CHAT_IDS.dmAnnaPiotr,
    name: "",
    isGroupChat: false,
    projectId: null,
    tenantId: DEMO_TENANT_ID,
    createdAt: "2024-06-10T08:12:00Z",
    createdByUserId: DEMO_USERS.anna.userId,
    unreadCount: 1,
    lastMessage: DEMO_MESSAGES[DEMO_CHAT_IDS.dmAnnaPiotr].at(-1) ?? null,
    members: [
      { userId: DEMO_USERS.anna.userId,  firstName: "Anna",  lastName: "Kowalska",   joinedAt: "2024-06-10T08:12:00Z", isAdmin: false, lastReadAt: "2024-06-10T08:26:00Z" },
      { userId: DEMO_USERS.piotr.userId, firstName: "Piotr", lastName: "Wiśniewski", joinedAt: "2024-06-10T08:12:00Z", isAdmin: false, lastReadAt: "2024-06-10T08:26:00Z" },
    ],
  },
  {
    id: DEMO_CHAT_IDS.dmAnnaMarta,
    name: "",
    isGroupChat: false,
    projectId: null,
    tenantId: DEMO_TENANT_ID,
    createdAt: "2024-05-22T13:05:00Z",
    createdByUserId: DEMO_USERS.marta.userId,
    unreadCount: 0,
    lastMessage: DEMO_MESSAGES[DEMO_CHAT_IDS.dmAnnaMarta].at(-1) ?? null,
    members: [
      { userId: DEMO_USERS.anna.userId,  firstName: "Anna",  lastName: "Kowalska", joinedAt: "2024-05-22T13:05:00Z", isAdmin: false, lastReadAt: "2024-05-22T13:31:00Z" },
      { userId: DEMO_USERS.marta.userId, firstName: "Marta", lastName: "Nowak",    joinedAt: "2024-05-22T13:05:00Z", isAdmin: false, lastReadAt: "2024-05-22T13:28:00Z" },
    ],
  },
  {
    id: DEMO_CHAT_IDS.dmAnnaTomasz,
    name: "",
    isGroupChat: false,
    projectId: null,
    tenantId: DEMO_TENANT_ID,
    createdAt: "2024-01-15T09:30:00Z",
    createdByUserId: DEMO_USERS.anna.userId,
    unreadCount: 0,
    lastMessage: DEMO_MESSAGES[DEMO_CHAT_IDS.dmAnnaTomasz].at(-1) ?? null,
    members: [
      { userId: DEMO_USERS.anna.userId,   firstName: "Anna",   lastName: "Kowalska", joinedAt: "2024-01-15T09:30:00Z", isAdmin: false, lastReadAt: "2024-01-15T09:41:00Z" },
      { userId: DEMO_USERS.tomasz.userId, firstName: "Tomasz", lastName: "Zając",    joinedAt: "2024-01-15T09:30:00Z", isAdmin: false, lastReadAt: "2024-01-15T09:38:00Z" },
    ],
  },
];

// ===== KONTAKTY (grupowane wg projektów) =====

export const DEMO_CHAT_CONTACTS: ProjectContactsGroupWeb[] = [
  {
    projectId: DEMO_PROJECT_IDS.biurowe,
    projectName: "Centrum Biurowe Nowa Brama",
    tenantId: DEMO_TENANT_ID,
    tenantName: "ArchPlan Sp. z o.o.",
    members: [
      { userId: DEMO_USERS.piotr.userId,  firstName: "Piotr",  lastName: "Wiśniewski" },
      { userId: DEMO_USERS.marta.userId,  firstName: "Marta",  lastName: "Nowak" },
      { userId: DEMO_USERS.tomasz.userId, firstName: "Tomasz", lastName: "Zając" },
    ],
  },
  {
    projectId: DEMO_PROJECT_IDS.drogowa,
    projectName: "Modernizacja Drogi Ekspresowej DK7",
    tenantId: DEMO_TENANT_ID,
    tenantName: "ArchPlan Sp. z o.o.",
    members: [
      { userId: DEMO_USERS.piotr.userId,  firstName: "Piotr",  lastName: "Wiśniewski" },
      { userId: DEMO_USERS.tomasz.userId, firstName: "Tomasz", lastName: "Zając" },
    ],
  },
  {
    projectId: DEMO_PROJECT_IDS.hotel,
    projectName: "Hotel Panorama – Rozbudowa Skrzydła B",
    tenantId: DEMO_TENANT_ID,
    tenantName: "ArchPlan Sp. z o.o.",
    members: [
      { userId: DEMO_USERS.piotr.userId,     firstName: "Piotr",     lastName: "Wiśniewski" },
      { userId: DEMO_USERS.marta.userId,     firstName: "Marta",     lastName: "Nowak" },
      { userId: DEMO_USERS.tomasz.userId,    firstName: "Tomasz",    lastName: "Zając" },
      { userId: DEMO_USERS.katarzyna.userId, firstName: "Katarzyna", lastName: "Wójcik" },
    ],
  },
];
