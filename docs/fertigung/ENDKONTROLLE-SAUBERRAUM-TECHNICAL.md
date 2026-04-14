# Endkontrolle Sauberraum Technical

## Zweck

Diese Dokumentation beschreibt den aktuellen technischen Stand der Endkontrolle im Sauberraum.

Relevante Dateien:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)
- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:1)

## Route und Seite

Die Seite ist unter folgender Route erreichbar:

- `/fertigung/endkontrolle`

Definiert in:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)

## Datenmodell

Das zentrale Modell ist `EndkontrolleEintrag`.

Definiert in:

- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:14)

Wichtige Felder:

- `ID`
- `Charge`
- `FANr`
- `Kunde`
- `Projekt`
- `Artikel`
- `Dekor`
- `Datum`
- `Gutteile`
- Fehlerfelder wie `Fusseln`, `Nadelstiche`, `Pickel`, `Dekorfehler`, `Farbfehler`
- interne Fehler wie `Oelflecken`, `Tiefziehfehler`, `Fraesfehler`, `Knicke`, `Kratzer`
- `Bemerkung`
- `Personalnummer`

## Aktueller UI-Aufbau

Die Seite ist jetzt in vier klar sichtbare Bereiche gegliedert:

1. Seitenkopf mit Status
2. Hinweisbereich `So funktioniert es`
3. Formular mit `Auftragsdaten` und `Fehlersammelkarte`
4. Historie `Letzte Eintraege`

Neu gegenueber der alten Version:

- deutlicher sichtbarer Bearbeitungsmodus
- klare Bedienhinweise direkt auf der Seite
- Bearbeiten ueber Button statt Inline-Zellenbearbeitung
- Drag-Scroll fuer die Historientabelle

## Wichtiger UI-State

Wichtige State-Variablen in [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1):

- `Kunden`
- `Projekte`
- `ArtikelListe`
- `DekorListe`
- `RecentEintraege`
- `UserPersonalnummer`
- `ActualUserName`
- `NeuerEintrag`
- `IsEditMode`
- `SelectedEintragId`

## Initialisierung

Beim Laden der Seite passiert in `OnInitializedAsync()`:

1. Auth-State wird geladen.
2. Benutzername wird aus `user.Identity.Name` gelesen.
3. Die Personalnummer wird aus dem Claim `UserId` gelesen.
4. Falls der Claim fehlt, wird `UserService.Personalnummer` als Fallback verwendet.
5. Das Formular wird per `ResetForm()` vorbereitet.
6. Kundenliste wird geladen.
7. Die letzten Eintraege werden geladen.

Danach initialisiert `OnAfterRenderAsync()` die Drag-Scroll-Hilfe fuer die Seite und fuer den Tabellenbereich.

## Laden der Auswahldaten

### Kunden

Die Kundenliste kommt aus `dbo.Kunden`.

Siehe:

- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:47)

### Projekte

Nach Kundenauswahl werden die Projekte des Kunden geladen.

Siehe:

- UI-Handler: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)
- Service: [EndkontrolleService.cs](/Data/EndkontrolleService.cs:69)

### Artikel und Dekore

Nach Projektauswahl werden Artikel und Dekore geladen.

Siehe:

- UI-Handler: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)
- Service: [EndkontrolleService.cs](/Data/EndkontrolleService.cs:88)

## Speichern neuer Eintraege

Das Speichern wird in `SaveEintrag()` ausgeloest.

Vor dem Speichern werden diese Felder geprueft:

- `Charge`
- `FANr`
- `Artikel`
- `Dekor`

Wenn `IsEditMode == false`, wird aufgerufen:

- `EndkontrolleService.InsertEintragAsync(...)`

DB-Zieltabelle:

- `dbo.Table1`

## Bearbeiten bestehender Eintraege

Die alte Inline-Bearbeitung pro Tabellenzelle wurde ersetzt.

Neuer Ablauf:

1. Unten wird auf `Bearbeiten` geklickt.
2. `EditEintrag(...)` kopiert den Datensatz in `NeuerEintrag`.
3. `IsEditMode` wird auf `true` gesetzt.
4. `SelectedEintragId` markiert die gewaehlte Zeile.
5. Das Formular oben zeigt den Datensatz im Bearbeitungsmodus.
6. `SaveEintrag()` ruft beim Speichern `UpdateEintragAsync(...)` auf.

Damit wird nur genau ein Datensatz aktualisiert:

- `UPDATE dbo.Table1 ... WHERE ID = @ID`

Siehe:

- UI: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)
- Service: [EndkontrolleService.cs](/Data/EndkontrolleService.cs:237)

## Verhalten nach erfolgreichem Update

Wenn ein Update erfolgreich ist:

- erscheint eine Erfolgsmeldung
- die bearbeitete Zeile wird in `RecentEintraege` lokal ersetzt
- das Formular wird per `ResetForm()` wieder auf Neueingabe gesetzt
- der Bearbeitungsmodus wird beendet

Vorteil:

- kein doppelter Eintrag
- kein Voll-Reload der kompletten Liste noetig

## Historie und Loeschen

Die letzten Eintraege werden weiterhin personalnummerbezogen geladen:

- `SELECT TOP 10 ... WHERE Personalnummer = @Personalnummer ORDER BY ID DESC`

Loeschen erfolgt weiter ueber:

- `DeleteEintragAsync(id, userName)`

## Drag-Scroll

Fuer die Bedienung auf Notebooks wurde eine Drag-Scroll-Hilfe eingebaut.

Technisch umgesetzt in:

- `OnAfterRenderAsync()`
- per `JS.InvokeVoidAsync("eval", ...)`

Zwei Ebenen werden beruecksichtigt:

1. Die Historientabelle selbst
2. Die uebergeordnete Seite innerhalb des Layout-Scrollcontainers `.content-scrollable`

Ziel:

- Scrollen ohne exaktes Treffen des Scrollbalkens
- Ziehen mit gedrueckter Maus statt nur Scrollrad
- keine Aktivierung auf echten Eingabeelementen wie `input`, `select`, `textarea`, `button`

## Design-Anpassungen

Die aktuelle Version enthaelt bewusst mehr Bedienhilfe und Abstand:

- groessere Innenabstaende fuer Eingabefelder
- Kartenkopf mit eigenem Padding
- klarere Trennung zwischen Titel und Inhalt
- besser sichtbarer Bearbeitungsstatus
- deutlicherer `Bearbeiten`- und `Aktualisieren`-Ablauf

## Historische Hinweise

Die Datenbank verwendet weiterhin einige historische Feldnamen:

- `Color` statt `Farbfehler`
- `Bemerkungen` statt `Bemerkung`
- `[FA-Nr]` statt `FANr`
- `FSKdate` statt `Datum`

## Relevante technische Besonderheiten

- Die Seite scrollt im Layout nicht ueber `body`, sondern ueber `.content-scrollable` in [MainLayout.razor](/Components/Layout/MainLayout.razor:1).
- Deshalb muss auch die Drag-Scroll-Logik an den Layout-Scrollcontainer gebunden werden.
- Die Tabellenzeile bleibt lokal markiert ueber `SelectedEintragId`.
- Das Formular arbeitet im Edit-Fall mit einer geklonten Instanz ueber `CloneEintrag(...)`.
