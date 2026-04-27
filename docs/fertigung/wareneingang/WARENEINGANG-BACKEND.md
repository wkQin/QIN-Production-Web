# Wareneingang Backend

Technische Dokumentation für die Wareneingang-Funktion in QIN Production Web.

Stand: Update 3.2.2

## Zweck

Der Wareneingang erfasst angeliefertes Produktionsmaterial, Chargen, Mengen, Zustand der Ware, Dickenmessung und Zusatzinformationen wie Palettentausch. Die Funktion speichert die Daten in der Fertigungsdatenbank und stellt Chargen für weitere Fertigungsprozesse bereit.

## Benutzeroberfläche

Route:

`/fertigung/wareneingang`

Hauptdatei:

`Components/Pages/Fertigung/Wareneingang.razor`

Die Seite besteht aus drei Bereichen:

1. Stammdaten
2. Chargenerfassung
3. Historie der letzten Wareneingänge

## Wichtige Code-Dateien

- `Components/Pages/Fertigung/Wareneingang.razor`
- `Data/WareneingangService.cs`
- `Data/ActivityLogService.cs`
- `Helpers/EmailHelper.cs`
- `Helpers/ZebraPrinterHelper.cs`
- `Data/SqlManager.cs`

## Datenbanken

Hauptdatenbank:

`qinFSK\table1`

Fertigungsdatenbank:

`Fertigung`

Wichtige Tabellen:

- `Lieferanten`
- `Materialliste`
- `Artikelliste`
- `Fertigung.dbo.Wareneingang`
- `Fertigung.dbo.Chargen`
- Aktivitätslog über `ActivityLogService`

## Datenmodelle

### WareneingangEntry

Quelle:

`Data/WareneingangService.cs`

Wichtige Felder:

- `ID`
- `Lieferant`
- `LS_Nr`
- `Pos`
- `EBE_NR`
- `Menge`
- `Artikel`
- `Zustand`
- `Bemerkung`
- `Dickenmessung`
- `Chargen`
- `Benutzer`
- `Eingangsdatum`
- `Palettentausch`
- `Gebucht`

### ChargenEntry

Wichtige Felder:

- `Charge`
- `Menge`
- `Scanner`
- `IsNew01`

`Scanner` zeigt, ob eine Charge über Scanner oder Tastatur erfasst wurde.

`IsNew01` zeigt, ob die Charge neu gespeichert werden muss.

## Ablauf beim Laden

Beim Öffnen der Seite passiert Folgendes:

1. Lieferanten werden geladen.
2. Die offene Wareneingangs-Historie wird geladen.
3. Die Historie wird lokal sortiert.
4. Die Tabellenhilfe für Spaltenbreiten wird per JavaScript aktiviert.

## Stammdaten

Erfasst werden:

- Lieferant
- Lieferschein
- Position
- EBE-Nummer
- Zustand der Ware
- Palettentausch
- Dickenmessung
- Bemerkung

Die Dickenmessung wird nur angezeigt, wenn der Lieferant als Automotive-relevant erkannt wird.

## Validierung

Vor dem Speichern prüft die Oberfläche:

- Lieferant muss ausgewählt sein.
- Lieferschein darf nicht leer sein.
- Zustand muss ausgewählt sein.
- Bei Palettentausch ist eine Bemerkung Pflicht.
- Bei Zustand `Schlecht` ist eine Bemerkung Pflicht.
- Dickenmessung muss numerisch sein.
- Dickenmessung muss zwischen `0,23 mm` und `1,2 mm` liegen.

Die Dickenmessung wird vor dem Speichern normalisiert:

- `mm` wird entfernt.
- Leerzeichen werden entfernt.
- Punkt und Komma werden akzeptiert.
- Gespeichert wird im deutschen Zahlenformat.

## Chargenerfassung

Chargen können auf zwei Arten erfasst werden:

1. Scan oder Eingabe mit Enter
2. Klick auf `Hinzufügen`

Beim Hinzufügen einer Charge:

- Leere Chargennummern werden ignoriert.
- Leere Menge wird als `0` behandelt.
- Die Menge wird in der Gesamtsumme berücksichtigt.
- Neue Chargen erhalten `IsNew01 = 1`.
- Der Fokus springt zurück in das Chargenfeld.

## Speichern

Methode im UI:

`SaveEintrag()`

Service-Methode:

`WareneingangService.InsertWareneingangAsync(...)`

Je nach Bearbeitungsstatus wird ausgeführt:

- neuer Eintrag: `INSERT INTO Wareneingang`
- bestehender Eintrag: `UPDATE Wareneingang`

Gespeichert werden unter anderem:

- Lieferant
- Lieferschein
- Position
- Zustand
- Palettentausch
- Artikel
- Eingangsdatum
- Benutzer
- Bemerkung
- EBE-Nummer
- Dickenmessung

Danach werden neue Chargen gespeichert.

## Chargen speichern

Methode:

`InsertChargenAsync(...)`

Es werden nur Chargen gespeichert, bei denen `IsNew01 = 1` ist.

Gesetzte Werte:

- `Wareneingang_ID`
- `Charge`
- `Aktuelle_Menge`
- `Kontrolle`
- `Einheit = LM`
- `Echte_Menge`
- `Liefermenge`
- `Status_ID = 2`

## Bearbeiten bestehender Einträge

Beim Klick auf eine Historienzeile:

1. Der Eintrag wird ausgewählt.
2. Die Maske wechselt in den Bearbeitungsmodus.
3. Stammdaten werden in die Felder geladen.
4. Chargen werden aus der Datenbank geladen.
5. Die Gesamtmenge wird neu berechnet.

## Historie

Die Historie zeigt offene Wareneingänge.

SQL-Basis:

- Tabelle `Wareneingang`
- Filter `Gebucht = 0`
- Chargenanzahl über Unterabfrage aus `Chargen`

Die Sortierung der Historie erfolgt in der Oberfläche.

## Etikettendruck

Methode im UI:

`PrintSelectedCharge()`

Helper:

`ZebraPrinterHelper.PrintSingleChargeQr(...)`

Das Label enthält:

- Charge
- Menge
- Material
- Eingangsdatum

Format:

55 x 28 mm

## QS-Mail

Nach erfolgreichem Speichern wird eine Mail an QS vorbereitet.

Empfänger:

`qsintern@qin-form.de`

Die Mail wird nach dem Speichern im Hintergrund ausgelöst.

## Aktivitätslog

Nach dem Speichern wird ein Aktivitätslog geschrieben.

Beispiele:

- Neuer Wareneingang erstellt
- Wareneingang aktualisiert

## Bekannte technische Hinweise

- Die Materialliste wird geladen, ist in der aktuellen Oberfläche aber nicht als sichtbares Auswahlfeld eingebunden.
- Beim Bearbeiten werden vorhandene Chargen angezeigt. Gespeichert werden nur neu hinzugefügte Chargen.
- Die Gesamtmenge wird in der Oberfläche berechnet.
- Wenn kein Datenbankmechanismus für `Wareneingang.Menge` existiert, muss geprüft werden, ob diese Spalte immer aktuell ist.

## Prüfpunkte nach Änderungen

Nach technischen Änderungen am Wareneingang prüfen:

1. Seite `/fertigung/wareneingang` öffnet ohne Fehler.
2. Lieferanten werden geladen.
3. Chargen können gescannt und manuell hinzugefügt werden.
4. Dickenmessung wird korrekt validiert.
5. Speichern erzeugt Wareneingang und Chargen.
6. Historie aktualisiert sich.
7. Etikettendruck öffnet die Druckansicht.
8. Aktivitätslog wird geschrieben.
