# Sperrlager Backend

Technische Dokumentation für die Sperrlager-Funktion in QIN Production Web.

Stand: Update 3.2.4

## Zweck

Das Sperrlager verwaltet gesperrte Chargen im Produktionslayout. Es verbindet die technische Sperre in `Fertigung.dbo.Chargen` mit einem Lagerplatz aus `dbo.Lagerorte` und einer Historie in `Fertigung.dbo.Sperrlager`.

Die Funktion ist bewusst mit echten Chargen verknüpft. Manuell eingegebene Chargennummern werden nur verarbeitet, wenn sie bereits in `Fertigung.dbo.Chargen` existieren.

## Benutzeroberfläche

Hauptseite:

`Components/Pages/Produktion/Produktionslayout.razor`

Modal:

`Components/Pages/Produktion/RegalModal.razor`

Das Sperrlager wird über den roten Button im Produktionslayout geöffnet.

Der Button ruft im Regalmodal auf:

`ShowAsync("Sperrlager")`

## Wichtige Code-Dateien

- `Components/Pages/Produktion/Produktionslayout.razor`
- `Components/Pages/Produktion/RegalModal.razor`
- `Data/ProduktionslayoutService.cs`
- `Data/WareneingangService.cs`
- `Data/ActivityLogService.cs`
- `Data/SqlManager.cs`

## Datenbanken

Hauptdatenbank:

`qinFSK\table1`

Fertigungsdatenbank:

`Fertigung`

Wichtige Tabellen:

- `qinFSK\table1.dbo.Lagerorte`
- `Fertigung.dbo.Chargen`
- `Fertigung.dbo.Wareneingang`
- `Fertigung.dbo.Sperrlager`

## Lagerorte

Die Sperrlagerplätze liegen in:

`qinFSK\table1.dbo.Lagerorte`

Verwendete Plätze:

- `H2R17P1S`
- `H2R17P2S`
- `H2R17P3S`
- `H2R17P4S`
- `H2R17P5S`
- `H2R17P6S`
- `H2R17P7S`
- `H2R17P8S`
- `H2R17P9S`
- `H2R17P10S`
- `H2R17P11S`
- `H2R17P12S`

Wichtige Felder:

- `QRCode`
- `AktuelleCharge`
- `LetzteNutzung`
- `Art`
- `Halle`
- `Regal`
- `Platz`

Mehrere Chargen werden in `AktuelleCharge` als Textliste gespeichert.

## Sperrlager-Historie

Die Historie liegt in:

`Fertigung.dbo.Sperrlager`

Die Tabelle ist über `Chargen_ID` mit `Fertigung.dbo.Chargen.ID` verbunden.

Wichtige Felder:

- `ID`
- `Chargen_ID`
- `Charge`
- `Aktion`
- `Bereich`
- `Grund`
- `GesperrtVon`
- `GesperrtVonPersonalnummer`
- `GesperrtAm`
- `EntsperrtVon`
- `EntsperrtAm`
- `VermuelltVon`
- `VermuelltAm`
- `LagerortQRCode`
- `Bemerkung`
- `CreatedAt`
- `UpdatedAt`

Verwendete Aktionen:

- `Gesperrt`
- `Entsperrt`
- `Vermüllt`

Verwendete Bereiche:

- `Wareneingang`
- `Sperrlager`

## Datenmodelle

Quelle:

`Data/ProduktionslayoutService.cs`

### PlatzInfo

Beschreibt einen Lagerplatz.

Wichtige Felder:

- `QRCode`
- `Charges`
- `PlatzChargen`
- `SumAktuelleMenge`
- `SumEchteMenge`
- `Wareneingaenge`

### PlatzChargeInfo

Beschreibt eine Charge auf einem Sperrlagerplatz.

Wichtige Felder:

- `Charge`
- `EingelagertAm`

### SperrlagerChargeInfo

Beschreibt eine offene gesperrte Charge ohne Lagerplatz.

Wichtige Felder:

- `ID`
- `Charge`
- `Artikel`
- `AktuelleMenge`
- `EchteMenge`
- `Einheit`
- `Datum`
- `Eingangsdatum`

### ChargeDetailInfo

Beschreibt die Daten für das Detailpopup.

Wichtige Felder:

- `Charge`
- `Artikel`
- `Lieferant`
- `LSNr`
- `EBENr`
- `Zustand`
- `Dickenmessung`
- `AktuelleMenge`
- `EchteMenge`
- `Einheit`
- `Eingangsdatum`
- `Lagerort`
- `EingelagertAm`
- `SperrlagerAktion`
- `SperrlagerBereich`
- `SperrlagerGrund`
- `SperrlagerBenutzer`
- `SperrlagerDatum`

### SperrlagerActionResult

Rückgabeobjekt für Aktionen.

Wichtige Felder:

- `Success`
- `Message`
- `BetroffeneChargen`

### SperrlagerChargeUpdateResult

Internes Ergebnis beim Verarbeiten von Chargen.

Wichtige Felder:

- `Found`
- `Missing`

`Missing` wird verwendet, um dem Nutzer konkret zu melden, welche eingegebenen Chargen nicht in `dbo.Chargen` gefunden wurden.

## Laden der Sperrlageransicht

Methode im UI:

`ShowAsync("Sperrlager")`

Ablauf:

1. QR-Codes `H2R17P1S` bis `H2R17P12S` werden erzeugt.
2. `ProduktionslayoutService.GetRegalInfosAsync(...)` lädt die belegten Plätze.
3. `ProduktionslayoutService.GetGesperrteNichtEingelagerteChargenAsync()` lädt offene gesperrte Chargen ohne Lagerplatz.
4. Die Ansicht zeigt zwei Sperrregale mit jeweils sechs Plätzen.

## Laden der Lagerplätze

Service-Methode:

`GetRegalInfosAsync(IEnumerable<string> qrCodes)`

Datenquellen:

- `qinFSK\table1.dbo.Lagerorte`
- `Fertigung.dbo.Chargen`
- `Fertigung.dbo.Wareneingang`

Ablauf:

1. Die angefragten Lagerorte werden aus `dbo.Lagerorte` geladen.
2. `AktuelleCharge` wird in einzelne Chargen zerlegt.
3. Pro Charge wird `PlatzChargeInfo` aufgebaut.
4. In der Fertigungsdatenbank werden Menge und Wareneingangsdaten ergänzt.

Bei Sperrlagerplätzen zeigt die UI nur Chargen und Einlagerungszeit. Artikel und Menge werden bewusst nicht direkt auf dem Platz angezeigt.

## Offene gesperrte Chargen

Service-Methode:

`GetGesperrteNichtEingelagerteChargenAsync()`

Datenquellen:

- `qinFSK\table1.dbo.Lagerorte`
- `Fertigung.dbo.Chargen`
- `Fertigung.dbo.Wareneingang`

Filter:

- `c.Gesperrt = 1`
- Charge ist nicht leer.
- `Status_ID` ist nicht `3`.
- `Zustand` ist nicht `Vermüllt`.
- Charge steht nicht in `dbo.Lagerorte.AktuelleCharge`.

Damit zeigt der Sideview nur gesperrte Chargen, die noch keinen Lagerplatz haben und nicht vermüllt sind.

## Detailpopup

Service-Methode:

`GetChargeDetailsAsync(string charge)`

Ablauf:

1. Die Methode sucht zuerst in `dbo.Lagerorte`, ob die Charge auf einem Lagerplatz liegt.
2. Danach werden Chargen-, Wareneingangs- und Sperrlagerdaten aus `Fertigung` geladen.
3. Der letzte Sperrlager-Eintrag wird über `OUTER APPLY` geholt.

Die letzte Aktion wird aus `Fertigung.dbo.Sperrlager` geladen.

Das Popup zeigt keine Wareneingangs-Bemerkung und kein Chargendatum, weil der Sperrgrund für das Sperrlager die relevante Information ist.

## Eingabe mehrerer Chargen

Hilfsmethode:

`ParseChargeText(string chargeText)`

Erlaubte Trennzeichen:

- Komma
- Semikolon
- Leerzeichen
- Tab
- Zeilenumbruch

Doppelte Chargen werden ignoriert.

## Sperren und Einlagern

Service-Methode:

`SperreChargenImSperrlagerAsync(string chargeText, string lagerortQRCode, string benutzer, string personalnummer)`

Ablauf:

1. Eingabetext wird in einzelne Chargen zerlegt.
2. Es wird geprüft, ob ein Lagerort ausgewählt ist.
3. Jede Charge wird in `Fertigung.dbo.Chargen` gesucht.
4. Gefundene Chargen werden auf `Gesperrt = 1` gesetzt.
5. Pro gefundener Charge wird ein Eintrag in `Fertigung.dbo.Sperrlager` geschrieben.
6. Die Charge wird aus allen anderen Lagerorten entfernt.
7. Die Charge wird beim ausgewählten Lagerort eingetragen.
8. `LetzteNutzung` des Lagerorts wird aktualisiert.
9. Ein Aktivitätslog wird geschrieben.

Wenn eine Charge nicht gefunden wird, wird sie in der Rückmeldung angezeigt.

## Entsperren

Service-Methode:

`EntsperreChargenAsync(string chargeText, string benutzer, string personalnummer)`

Ablauf:

1. Eingabetext wird in einzelne Chargen zerlegt.
2. Gefundene Chargen werden auf `Gesperrt = 0` gesetzt.
3. Pro Charge wird ein Sperrlager-Eintrag mit Aktion `Entsperrt` geschrieben.
4. Die Charge wird aus allen Lagerorten entfernt.
5. Ein Aktivitätslog wird geschrieben.

Danach kann die Charge wieder in normalen Prozessen erscheinen.

## Vermüllen

Service-Methode:

`VermuelleChargenAsync(string chargeText, string benutzer, string personalnummer)`

Ablauf:

1. Eingabetext wird in einzelne Chargen zerlegt.
2. Gefundene Chargen bleiben gesperrt.
3. `Aktuelle_Menge` wird auf `0` gesetzt.
4. `Status_ID` wird auf `3` gesetzt.
5. `Zustand` wird auf `Vermüllt` gesetzt.
6. Pro Charge wird ein Sperrlager-Eintrag mit Aktion `Vermüllt` geschrieben.
7. Die Charge wird aus allen Lagerorten entfernt.
8. Ein Aktivitätslog wird geschrieben.

Wichtig:

Beim Vermüllen wird `Gesperrt` nicht auf `0` gesetzt. Die Charge bleibt gesperrt, damit sie nicht in weiteren Prozessen auftaucht.

## Lagerort aktualisieren

Service-Methode:

`MoveChargesToLagerortAsync(List<string> charges, string lagerortQRCode)`

Ablauf:

1. Die Charge wird zuerst aus allen Lagerorten entfernt.
2. Der Ziel-Lagerort wird geladen.
3. Vorhandene und neue Chargen werden zusammengeführt.
4. Doppelte Chargen werden entfernt.
5. `AktuelleCharge` und `LetzteNutzung` werden aktualisiert.

## Charge aus Lagerorten entfernen

Service-Methode:

`RemoveChargesFromAllLagerorteAsync(List<string> charges)`

Ablauf:

1. Alle Lagerorte mit `AktuelleCharge` werden geladen.
2. Die zu entfernenden Chargen werden aus der Textliste entfernt.
3. Wenn keine Charge mehr übrig ist, wird `AktuelleCharge` leer geschrieben.
4. Wenn der Platz leer wird, wird `LetzteNutzung` auf `NULL` gesetzt.

## Automatische Sperre aus dem Wareneingang

Quelle:

`Data/WareneingangService.cs`

Relevante Methode:

`SperreChargenFuerWareneingangAsync(...)`

Bei wiederholt falscher Dickenmessung sperrt der Wareneingang vorhandene Chargen einer Bestellung.

Zusätzlich wird aufgerufen:

`InsertSperrlagerLogsForWareneingangAsync(...)`

Dadurch erhält jede automatisch gesperrte Charge einen Eintrag in `Fertigung.dbo.Sperrlager`.

Verwendete Werte:

- `Aktion = Gesperrt`
- `Bereich = Wareneingang`
- `Grund = Sperrgrund aus der Dickenmessung`
- Benutzer und Personalnummer aus der Session

Diese Chargen erscheinen danach in der offenen Sperrlagerliste, solange sie noch keinem Lagerort zugeordnet wurden.

## Benutzerinformationen

Im UI werden Benutzerinformationen über `AuthenticationStateProvider` gelesen.

Verwendete Werte:

- `user.Identity.Name`
- Claim `UserId`

Wenn keine Daten vorhanden sind, wird `System` verwendet.

## Aktivitätslog

Bei erfolgreichen Aktionen wird ein Aktivitätslog geschrieben.

Beispiele:

- `[Sperrlager] Manuelle Sperre über Sperrlager: 12345`
- `[Sperrlager] Entsperrt über Sperrlager: 12345`
- `[Sperrlager] Vermüllt über Sperrlager: 12345`

## Bekannte technische Hinweise

- `Fertigung.dbo.Sperrlager.Chargen_ID` ist per Fremdschlüssel mit `Fertigung.dbo.Chargen.ID` verbunden.
- Unbekannte Chargen werden nicht automatisch in `dbo.Chargen` angelegt.
- Mehrere Chargen in `dbo.Lagerorte.AktuelleCharge` werden als Textliste gespeichert.
- Die offene gesperrte Liste blendet vermüllte Chargen aus.
- Ein Platzklick auf einen belegten Platz übernimmt alle Chargen des Platzes in das Eingabefeld.
- Ein Platzklick auf einen leeren Platz löscht eine manuell eingetragene Charge nicht.

## Prüfpunkte nach Änderungen

Nach technischen Änderungen am Sperrlager prüfen:

1. Produktionslayout öffnet ohne Fehler.
2. Der rote Sperrlager-Button öffnet die Sperrlageransicht.
3. Alle 12 Plätze werden angezeigt.
4. Platznamen werden als `Platz 1`, `Platz 2` und so weiter angezeigt.
5. Gesperrte eingelagerte Chargen stehen auf dem richtigen Platz.
6. Offene gesperrte Chargen erscheinen im Sideview.
7. Klick auf eine offene Charge öffnet das Detailpopup.
8. Klick auf eine Platz-Charge öffnet das Detailpopup.
9. Kopier-Button schreibt die Charge ins Eingabefeld.
10. Klick auf belegten Platz übernimmt alle Chargen für Bulk-Aktionen.
11. Sperren setzt `Gesperrt = 1` und schreibt Lagerort und Historie.
12. Entsperren setzt `Gesperrt = 0` und entfernt die Charge aus Lagerorten.
13. Vermüllen lässt `Gesperrt = 1`, setzt `Status_ID = 3`, setzt `Zustand = Vermüllt` und entfernt die Charge aus Lagerorten.
14. Nicht gefundene Chargen werden verständlich gemeldet.
15. Automatische Sperren aus dem Wareneingang schreiben Sperrlager-Historie.
