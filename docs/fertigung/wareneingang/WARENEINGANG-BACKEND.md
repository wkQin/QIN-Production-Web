# Wareneingang Backend

Technische Dokumentation für die Wareneingang-Funktion in QIN Production Web.

Stand: Update 3.2.4

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
3. Offene Wareneingänge

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
- `qinFSK\table1.dbo.Materialliste`
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
2. Die offenen Wareneingänge werden geladen.
3. Die Tabelle wird lokal sortiert.
4. Die Tabellenhilfe für Spaltenbreiten wird per JavaScript aktiviert.

## Stammdaten

Erfasst werden:

- Lieferant
- Lieferschein
- Position
- EBE-Nummer
- Zustand der Ware
- Palettentausch
- Mustermaterial
- Dickenmessung
- Bemerkung

Die Dickenmessung wird nur angezeigt, wenn der Lieferant als Automotive-relevant erkannt wird und `Mustermaterial` nicht aktiv ist.

Bei `Mustermaterial`:

- Materialname wird als freier Text erfasst.
- Dickenmessung wird ausgeblendet.
- Pflichtfeldprüfungen werden übersprungen.

## Validierung

Vor dem Speichern prüft die Oberfläche:

- Lieferant muss ausgewählt sein.
- Lieferschein darf nicht leer sein.
- Zustand muss ausgewählt sein.
- Bei Zustand `Schlecht` ist eine Bemerkung Pflicht.
- Dickenmessung muss numerisch sein.
- Ohne Materialtreffer muss die Dickenmessung zwischen `0,23 mm` und `1,2 mm` liegen.
- Mit Materialtreffer muss die Dickenmessung innerhalb der Toleranz zum Sollwert liegen.

Die Dickenmessung wird vor dem Speichern normalisiert:

- `mm` wird entfernt.
- Leerzeichen werden entfernt.
- Punkt und Komma werden akzeptiert.
- Gespeichert wird im deutschen Zahlenformat.

### Dickenmessung nach Materialliste

Service-Methode:

`WareneingangService.FindMaterialDickenmessungAsync(...)`

Quelle:

`qinFSK\table1.dbo.Materialliste`

Verwendete Felder:

- `Suchbegriff`
- `Beschreibung`
- `Beschreibung2`
- `Dickenmessung`

Die Suche bewertet direkte Treffer und Teiltreffer. Damit werden einfache Treffer wie `Kurz: KUGA Carbon Black Weave` und Materialtexte mit Maßangaben wie `675 x 355 x 0,25mm` unterstützt.

Wenn ein Sollwert gefunden wird:

- Sollwert wird im UI grün angezeigt.
- Erlaubte Toleranz wird angezeigt.
- Toleranz beträgt 10 Prozent.

Beispiel:

- Sollwert `0,5 mm`
- Erlaubt `0,45 mm` bis `0,55 mm`

Wenn kein Sollwert gefunden wird, gilt der Standardbereich `0,23 mm` bis `1,2 mm`.

### Sperre bei falscher Dickenmessung

Beim ersten falschen Speicher-Versuch zeigt die Oberfläche ein großes Warnfenster.

Beim nächsten falschen Speicher-Versuch:

1. Es wird geprüft, ob Chargen vorhanden sind.
2. Der Wareneingang wird gespeichert.
3. Die zugehörigen Chargen werden über `dbo.Chargen.Gesperrt = 1` gesperrt.
4. QSIntern erhält eine E-Mail.
5. Die Sperr-Bemerkung wird in `Wareneingang.Bemerkung` gespeichert.

Wenn bereits eine Werker-Bemerkung vorhanden ist, wird sie nicht überschrieben. Die automatische Sperr-Bemerkung wird darunter ergänzt.

Service-Methode:

`WareneingangService.InsertWareneingangAndSperreChargenAsync(...)`

Interne Sperrmethode:

`SperreChargenFuerWareneingangAsync(...)`

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

Beim Klick auf eine Zeile in `Offene Wareneingänge`:

1. Der Eintrag wird ausgewählt.
2. Die Maske wechselt in den Bearbeitungsmodus.
3. Stammdaten werden in die Felder geladen.
4. Chargen werden aus der Datenbank geladen.
5. Die Gesamtmenge wird neu berechnet.

## Offene Wareneingänge

Der Bereich `Offene Wareneingänge` zeigt offene Wareneingänge.

SQL-Basis:

- Tabelle `Wareneingang`
- Filter `Gebucht = 0`
- Chargenanzahl über Unterabfrage aus `Chargen`

Die Sortierung der Tabelle erfolgt in der Oberfläche.

Die Bemerkung wird in der Tabelle angezeigt. Lange Texte werden gekürzt dargestellt und vollständig als Tooltip angeboten.

## Etikettendruck

Methode im UI:

`PrintSelectedCharge()`

Zusätzliche Methode im UI:

`PrintAllCharges()`

Helper:

`WareneingangPrintHelper.BuildSingleChargeZpl(...)`

`WareneingangPrintHelper.BuildChargeZplBatch(...)`

Das Label enthält:

- Charge
- Menge
- Material
- Eingangsdatum

Format:

55 x 28 mm

Der Druck wird testweise über Zebra Browser Print lokal am Client ausgelöst.

JavaScript-Funktion:

`qinZebraBrowserPrint.printZpl(...)`

Ablauf:

1. Die Blazor-Seite erzeugt ZPL für eine oder mehrere Chargen.
2. Der Browser ruft `qinZebraBrowserPrint.printZpl(...)` auf.
3. Die JavaScript-Funktion sucht den Zebra-Standarddrucker über Browser Print.
4. Der ZPL-Druckauftrag wird an den lokalen Browser-Print-Dienst gesendet.

Voraussetzungen am Arbeitsplatz-PC:

- Zebra Browser Print ist installiert und läuft.
- Der Zebra-Drucker ist als Default Device eingetragen.
- Der aufgerufene Webhost ist in den Accepted Hosts freigegeben.

Der alte HTML-Druckdialog bleibt als Hilfsfunktion `printHtmlDocument(...)` vorhanden, wird im Wareneingang aber nicht mehr für den normalen Etikettendruck verwendet.

## QS-Mail

Nach erfolgreichem Speichern wird eine Mail an QS vorbereitet.

Empfänger:

`qsintern@qin-form.de`

Die Mail wird nach dem Speichern im Hintergrund ausgelöst.

Bei automatischer Chargensperre wird zusätzlich eine QS-Mail mit Sperrgrund versendet.

## Aktivitätslog

Nach dem Speichern wird ein Aktivitätslog geschrieben.

Beispiele:

- Neuer Wareneingang erstellt
- Wareneingang aktualisiert

## Bekannte technische Hinweise

- Die Materialliste wird für die Soll-Dickenmessung durchsucht.
- Beim Bearbeiten werden vorhandene Chargen angezeigt. Gespeichert werden nur neu hinzugefügte Chargen.
- Die Gesamtmenge wird in der Oberfläche berechnet.
- Wenn kein Datenbankmechanismus für `Wareneingang.Menge` existiert, muss geprüft werden, ob diese Spalte immer aktuell ist.

## Prüfpunkte nach Änderungen

Nach technischen Änderungen am Wareneingang prüfen:

1. Seite `/fertigung/wareneingang` öffnet ohne Fehler.
2. Lieferanten werden geladen.
3. Chargen können gescannt und manuell hinzugefügt werden.
4. Dickenmessung wird korrekt validiert.
5. Falsche Dickenmessung zeigt zuerst das Warnfenster.
6. Zweiter falscher Versuch sperrt vorhandene Chargen.
7. Speichern erzeugt Wareneingang und Chargen.
8. Offene Wareneingänge aktualisieren sich.
9. Etikettendruck öffnet die Druckansicht.
10. Aktivitätslog wird geschrieben.
