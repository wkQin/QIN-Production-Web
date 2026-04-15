# Wareneingang Technical

## Zweck

Diese Dokumentation beschreibt den aktuellen technischen Stand der Wareneingang-Seite in der Fertigung.

Relevante Dateien:

- [Wareneingang.razor](/Components/Pages/Fertigung/Wareneingang.razor:1)
- [WareneingangService.cs](/Data/WareneingangService.cs:1)
- [EmailHelper.cs](/Helpers/EmailHelper.cs:1)
- [ZebraPrinterHelper.cs](/Helpers/ZebraPrinterHelper.cs:1)
- [ActivityLogService.cs](/Data/ActivityLogService.cs:1)

## Route und Seite

Die Seite ist unter folgender Route erreichbar:

- `/fertigung/wareneingang`

Definiert in:

- [Wareneingang.razor](/Components/Pages/Fertigung/Wareneingang.razor:1)

## Datenmodell

### WareneingangEntry

Definiert in:

- [WareneingangService.cs](/Data/WareneingangService.cs:8)

Wichtige Felder:

- `ID`
- `Lieferant`
- `EBE_NR`
- `LS_Nr`
- `Pos`
- `Menge`
- `Artikel`
- `Bemerkung`
- `Zustand`
- `Chargen`
- `Benutzer`
- `Eingangsdatum`
- `Palettentausch`
- `Gebucht`
- `Dickenmessung`

### ChargenEntry

Definiert in:

- [WareneingangService.cs](/Data/WareneingangService.cs:26)

Wichtige Felder:

- `Charge`
- `Menge`
- `Scanner`
- `IsNew01`

`Scanner` unterscheidet, ob eine Charge ueber `Enter` bzw. Scanner oder manuell ueber den Button angelegt wurde.

`IsNew01` markiert, ob eine Charge beim Speichern neu in die DB geschrieben werden soll.

## Aktueller UI-Aufbau

Die Seite ist in drei Nutzungsbereiche gegliedert:

1. Kopfbereich mit Status, `Leeren` und `Speichern`
2. Erfassung von Stammdaten und Chargen
3. Historie der letzten Buchungen

Die Historientabelle ist ueber `makeTableResizable(...)` mit einer JS-Hilfe erweitert.

## Wichtiger UI-State

Wichtige Variablen in [Wareneingang.razor](/Components/Pages/Fertigung/Wareneingang.razor:1):

- Form-Felder wie `Lieferschein`, `Pos`, `EBE`, `SelectedLieferant`, `Zustand`, `Dickenmessung`, `Bemerkung`, `Palettentausch`
- Eingabefelder fuer neue Chargen: `NewCharge`, `NewMenge`
- berechnete Summe: `GesamtLaufmeter`
- Listen: `Lieferanten`, `Materialien`, `ChargenList`, `WarenList`
- Edit-State: `IsEditMode`, `SelectedEntry`, `SelectedCharge`
- Sortierung: `SortColumn`, `IsSortedAscending`

## Initialisierung

Beim Laden der Seite passiert in `OnInitializedAsync()`:

1. Lieferanten werden ueber `GetLieferantenAsync()` geladen.
2. Die Wareneingangs-Historie wird ueber `LoadWareneingangAsync()` geladen.
3. Die Historie wird direkt lokal sortiert.

In `OnAfterRenderAsync()` wird beim ersten Rendern die Tabellenhilfe initialisiert:

- `JS.InvokeVoidAsync("makeTableResizable", "historieTable")`

## Laden von Auswahldaten und Historie

### Lieferanten

Die Lieferantenliste kommt aus:

- Tabelle `Lieferanten`

Siehe:

- [WareneingangService.cs](/Data/WareneingangService.cs:35)

### Materialien

Nach Auswahl eines Lieferanten werden Materialien geladen ueber:

- `FindAllMaterialsAsync(SelectedLieferant)`

Abfragebasis:

- `Materialliste`
- `Artikelliste`

Siehe:

- UI-Handler: [Wareneingang.razor](/Components/Pages/Fertigung/Wareneingang.razor:1)
- Service: [WareneingangService.cs](/Data/WareneingangService.cs:87)

Hinweis:

- Die Materialliste wird aktuell im State geladen, auf der sichtbaren Seite aber nicht als Auswahlfeld angezeigt.

### Historie

Die Historie kommt aus `Wareneingang` in der Fertigungsdatenbank:

- nur Datensaetze mit `Gebucht = 0`
- optional gefiltert nach Lieferant
- sortiert per SQL zuerst nach `ID DESC`

Pro Datensatz wird die Chargenanzahl ueber eine Subquery gegen `Chargen` mitgeladen.

Siehe:

- [WareneingangService.cs](/Data/WareneingangService.cs:55)

## Chargen-Erfassung im UI

Die Chargenerfassung laeuft ueber zwei Wege:

1. Scanner bzw. `Enter` in `HandleKeyUp(...)`
2. Klick auf `Hinzufuegen`

Beide Wege landen in:

- `AddCharge(bool isEnterKey)`

Dabei passiert:

- leere Charge wird verworfen
- leere Menge wird als `0` behandelt
- `Scanner = 1` bei `Enter`, sonst `0`
- neue Eintraege erhalten `IsNew01 = 1`
- `GesamtLaufmeter` wird lokal neu berechnet
- der Fokus springt zurueck auf das Charge-Feld

## Validierung vor dem Speichern

Das Speichern wird ueber `SaveEintrag()` ausgeloest.

Vor dem eigentlichen DB-Zugriff werden aktuell geprueft:

- `Lieferschein` darf nicht leer sein
- `SelectedLieferant` darf nicht leer sein
- `Bemerkung` ist Pflicht bei `Palettentausch = true`
- `Bemerkung` ist Pflicht bei `Zustand = Schlecht`
- `Dickenmessung` muss numerisch gueltig sein
- `Dickenmessung` muss zwischen `0.23` und `1.2` liegen

Die Dickenmessung wird in `TryNormalizeDickenmessung(...)` normalisiert:

- `mm` wird entfernt
- Leerzeichen werden entfernt
- Komma wird intern zu Punkt
- gespeichert wird anschliessend wieder im `de-DE`-Format

## Benutzerkontext

Vor dem Speichern wird der angemeldete Benutzer ueber `AuthenticationStateProvider` gelesen.

Genutzt werden:

- `user.Identity.Name`
- Claim `UserId`

Falls der Claim fehlt, wird als Fallback `100` verwendet.

Die Daten werden in `UserSession` uebergeben.

## Speichern neuer Eintraege und Updates

Das UI ruft auf:

- `WareneingangService.InsertWareneingangAsync(...)`

Je nach `IsEditMode` wird intern entweder ein `INSERT` oder ein `UPDATE` auf `Wareneingang` ausgefuehrt.

Gespeichert werden im Wareneingang unter anderem:

- `Lieferant`
- `LS_Nr`
- `Pos`
- `Zustand`
- `Palettentausch`
- `Artikel`
- `Eingangsdatum`
- `Benutzer`
- `Bemerkung`
- `EBE_Nr`
- `Dickenmessung`

Danach:

- werden neue Chargen ueber `InsertChargenAsync(...)` geschrieben
- wird ein Aktivitaetslog ueber `ActivityLogService.InsertLogAsync(...)` erzeugt

Nach erfolgreichem Speichern im UI:

- erscheint eine Meldung
- wird die Historie neu geladen
- wird das Formular ueber `RefreshForm()` geleert
- startet eine Fire-and-forget-QS-Mail an `qsintern@qin-form.de`

## Bearbeiten bestehender Eintraege

Beim Klick auf eine Historienzeile passiert in `EditSelected(...)`:

1. `SelectedEntry` wird gesetzt.
2. `IsEditMode` wird auf `true` gesetzt.
3. Stammdaten wie `Lieferschein`, `Pos`, `EBE`, `Lieferant`, `Zustand` und `Dickenmessung` werden in die UI kopiert.
4. Materialien werden fuer den Lieferanten geladen.
5. Chargen werden ueber `FindChargenAsync(ID)` geladen.
6. Die Gesamtmenge wird lokal neu berechnet.

## Drucken von Chargen-Etiketten

Das Drucken laeuft ueber `PrintSelectedCharge()`.

Voraussetzungen:

- `SelectedCharge` ist gesetzt
- `SelectedLieferant` ist nicht leer

Ablauf:

1. Eingangsdatum der Charge wird ueber `GetEingangsDatumForChargeAsync(...)` geladen.
2. `ZebraPrinterHelper.PrintSingleChargeQr(...)` erzeugt ZPL fuer ein 55 x 28 mm Label.
3. Das QR-Label enthaelt `Charge|Menge|Material|Eingangsdatum`.

## Sortierung der Historie

Die Historie wird im UI ueber `SortTable(...)` und `SortWarenList()` sortiert.

Sortierbare Spalten:

- `ID`
- `Lieferschein`
- `Pos`
- `EBE`
- `Lieferant`
- `Material`
- `Menge`
- `Chargen`

Die Sortierung erfolgt rein lokal auf `WarenList`.

## Datenzugriffe im Service

### GetLieferantenAsync

- Verbindung: `SqlManager.connectionString`
- Quelle: `Lieferanten`

### LoadWareneingangAsync

- Verbindung: `SqlManager.FertigungConnectionString`
- Quelle: `Wareneingang`
- Zusatz: Chargenanzahl via Subquery aus `Chargen`

### FindAllMaterialsAsync

- Verbindung: `SqlManager.connectionString`
- Quelle: `Artikelliste` und `Materialliste`

### FindChargenAsync

- Verbindung: `SqlManager.FertigungConnectionString`
- Quelle: `Chargen`

Bestehende Chargen werden mit `IsNew01 = 0` in den UI-State geladen.

### InsertWareneingangAsync

- Verbindung: `SqlManager.FertigungConnectionString`
- Ziel: `Wareneingang`
- Nebeneffekt: Aktivitaetslog in Hauptdatenbank

### InsertChargenAsync

- nutzt die bereits offene Fertigungs-Connection
- schreibt nur Chargen mit `IsNew01 == 1`
- setzt `Einheit = LM`
- setzt `Status_ID = 2`

### GetEingangsDatumForChargeAsync

- liest das Eingangsdatum ueber Join `Wareneingang` zu `Chargen`

## Wichtige technische Besonderheiten

Aus dem aktuellen Quellcode ergeben sich folgende Punkte:

- `Materialien` werden geladen, aber auf der sichtbaren Seite nicht ausgewaehlt. Neue Eintraege koennen dadurch mit leerem `Artikel` gespeichert werden.
- Beim Bearbeiten werden `Bemerkung` und `Palettentausch` nicht aus der Historie in die UI zurueckgeladen.
- Bestehende Chargen werden im Edit-Fall zwar angezeigt, aber `InsertChargenAsync(...)` speichert nur neue Chargen mit `IsNew01 == 1`.
- `GesamtLaufmeter` wird im UI berechnet. Im gezeigten Service-Code wird `Wareneingang.Menge` selbst jedoch nicht direkt per `INSERT` oder `UPDATE` geschrieben. Wenn kein DB-seitiger Mechanismus existiert, ist diese Spalte potentiell leer oder nicht aktuell.

Diese Punkte sind keine Vermutung ueber die Datenbank selbst, sondern eine direkte Ableitung aus dem derzeit sichtbaren Code.

## Relevante technische Stellen

- UI und Validierung: [Wareneingang.razor](/Components/Pages/Fertigung/Wareneingang.razor:1)
- Datenmodelle und SQL: [WareneingangService.cs](/Data/WareneingangService.cs:1)
- Benutzerkontext: [LoginService.cs](/Data/LoginService.cs:1)
- QS-Mail: [EmailHelper.cs](/Helpers/EmailHelper.cs:1)
- Etikettendruck: [ZebraPrinterHelper.cs](/Helpers/ZebraPrinterHelper.cs:1)
- Aktivitaetslog: [ActivityLogService.cs](/Data/ActivityLogService.cs:1)
