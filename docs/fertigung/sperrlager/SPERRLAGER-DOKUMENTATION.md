# Sperrlager Dokumentation

Allgemeine Dokumentation für das Sperrlager in QIN Production Web.

Stand: Update 3.2.4

## Zweck

Das Sperrlager dient dazu, gesperrte Chargen sichtbar, nachvollziehbar und kontrolliert zu verwalten.

Gesperrte Chargen sollen nicht unbeabsichtigt in weiteren Produktionsprozessen verwendet werden. Deshalb werden sie technisch gesperrt, einem Sperrlagerplatz zugeordnet und mit einem Sperrgrund dokumentiert.

## Zielgruppe

Diese Dokumentation richtet sich an:

- Fertigung
- Schichtleitung
- QS
- Verwaltung
- IT und Systembetreuung

Für die technische Umsetzung gibt es ein eigenes Dokument:

`SPERRLAGER-BACKEND.md`

## Funktionsumfang

Das Sperrlager umfasst:

- Anzeige von 12 definierten Sperrlagerplätzen
- Zwei sichtbare Sperrregale mit jeweils 6 Plätzen
- Anzeige gesperrter Chargen pro Platz
- Anzeige des Einlagerungszeitpunkts je Charge
- Detailpopup für Chargen
- Sideview für offene gesperrte Chargen ohne Lagerplatz
- Manuelles Sperren und Einlagern von Chargen
- Entsperren von Chargen
- Vermüllen von Chargen
- Bulk-Aktionen für mehrere Chargen
- Protokollierung aller Sperrlager-Aktionen

## Einstieg

Das Sperrlager wird im Produktionslayout geöffnet.

Im Produktionslayout befindet sich ein roter Button `Sperrlager`. Der Button öffnet eine eigene Sperrlager-Ansicht.

Die Ansicht zeigt:

- Sperrregal A
- Sperrregal B
- 12 Sperrlagerplätze
- Aktionsbereich für Charge und Aktionen
- Button `Offene gesperrte Chargen`

## Sperrlagerplätze

Die Sperrlagerplätze sind fest definiert.

Verwendete QR-Codes:

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

Im UI werden die Plätze als `Platz 1`, `Platz 2` und so weiter angezeigt.

## Gesperrte Chargen anzeigen

Wenn eine Charge auf einem Sperrlagerplatz liegt, wird auf dem Platz angezeigt:

- Chargennummer
- Einlagerungszeitpunkt
- Kopier-Button

Der Platz zeigt keine Artikel- oder Mengenübersicht, damit mehrere Chargen auf einem Platz gut lesbar bleiben.

## Offene gesperrte Chargen

Der Button `Offene gesperrte Chargen` öffnet eine seitliche Liste.

Diese Liste zeigt gesperrte Chargen, die noch keinem Lagerplatz zugeordnet sind.

Eine Charge erscheint dort, wenn:

- `Fertigung.dbo.Chargen.Gesperrt = 1` ist.
- Die Charge nicht in `dbo.Lagerorte.AktuelleCharge` steht.
- Die Charge nicht als vermüllt markiert ist.

Vermüllte Chargen werden nicht mehr in dieser Liste angezeigt.

## Charge einlagern

Eine Charge kann über das Sperrlager eingelagert werden.

Ablauf:

1. Sperrlager öffnen.
2. Einen Sperrlagerplatz auswählen.
3. Charge in das Eingabefeld schreiben.
4. Auf `Sperren` klicken.

Danach:

- Die Charge wird in `Fertigung.dbo.Chargen` auf `Gesperrt = 1` gesetzt.
- Die Charge wird beim ausgewählten Lagerplatz eingetragen.
- Ein Sperrlager-Protokolleintrag wird geschrieben.
- Die Charge verschwindet aus der Liste `Offene gesperrte Chargen`.

Wichtig:

Die Charge muss bereits in `Fertigung.dbo.Chargen` vorhanden sein. Unbekannte Chargen werden nicht neu angelegt.

Wenn eine Charge nicht gefunden wird, zeigt das System eine Meldung mit der fehlenden Chargennummer.

## Mehrere Chargen bearbeiten

Im Eingabefeld können mehrere Chargen eingetragen werden.

Erlaubte Trennzeichen:

- Komma
- Semikolon
- Leerzeichen
- Zeilenumbruch

Beispiel:

`10239021, 1283219`

Wenn ein belegter Platz angeklickt wird, werden alle Chargen dieses Platzes automatisch in das Eingabefeld übernommen. Dadurch können mehrere Chargen auf einmal entsperrt oder vermüllt werden.

## Kopier-Button

Neben jeder Charge auf einem Sperrlagerplatz gibt es einen Kopier-Button.

Der Button kopiert die einzelne Charge direkt in das Eingabefeld.

Das ist sinnvoll, wenn nur eine Charge von einem Platz bearbeitet werden soll.

## Detailpopup

Beim Klick auf eine Charge öffnet sich ein Detailpopup.

Das Detailpopup zeigt:

- Artikel
- Lieferant
- Lagerort
- Eingelagert am
- Menge
- LS-Nummer
- EBE-Nummer
- Zustand
- Dickenmessung
- Wareneingangsdatum
- Sperrlager-Aktion
- Bereich
- Benutzer oder System
- Vorgangszeit
- Sperrgrund

Das Detailpopup zeigt keine doppelte Wareneingangs-Bemerkung und kein Chargendatum, weil diese Informationen für die Sperrlagerentscheidung aktuell nicht wichtig sind.

## Sperren

Beim Sperren wird eine Charge technisch gesperrt und einem Sperrlagerplatz zugeordnet.

Das System setzt:

- `Fertigung.dbo.Chargen.Gesperrt = 1`
- Lagerort in `dbo.Lagerorte.AktuelleCharge`
- Eintrag in `Fertigung.dbo.Sperrlager`

Wenn mehrere Chargen eingegeben werden, werden alle gefundenen Chargen verarbeitet.

Nicht gefundene Chargen werden in der Meldung angezeigt.

## Entsperren

Beim Entsperren wird eine Charge wieder freigegeben.

Das System setzt:

- `Fertigung.dbo.Chargen.Gesperrt = 0`
- entfernt die Charge aus allen Lagerorten
- schreibt einen Eintrag in `Fertigung.dbo.Sperrlager`

Danach kann die Charge wieder in normalen Prozessen auftauchen.

## Vermüllen

Beim Vermüllen wird eine Charge nicht entsperrt.

Das System setzt:

- `Fertigung.dbo.Chargen.Gesperrt = 1`
- `Fertigung.dbo.Chargen.Status_ID = 3`
- `Fertigung.dbo.Chargen.Zustand = Vermüllt`
- `Fertigung.dbo.Chargen.Aktuelle_Menge = 0`

Zusätzlich wird die Charge aus allen Lagerorten entfernt und ein Eintrag in `Fertigung.dbo.Sperrlager` geschrieben.

Die Charge verschwindet dadurch aus der Liste `Offene gesperrte Chargen`, bleibt aber fachlich gesperrt und soll nicht in weiteren Prozessen verwendet werden.

## Automatische Sperren aus dem Wareneingang

Der Wareneingang kann Chargen automatisch sperren.

Das passiert, wenn die Dickenmessung wiederholt außerhalb der erlaubten Toleranz liegt und zur Bestellung Chargen vorhanden sind.

Bei einer automatischen Sperre:

- Die betroffenen Chargen werden gesperrt.
- Ein Sperrgrund wird erzeugt.
- QSIntern erhält eine E-Mail.
- Ein Eintrag in `Fertigung.dbo.Sperrlager` wird geschrieben.

Diese Chargen erscheinen anschließend in `Offene gesperrte Chargen`, solange sie noch keinem Sperrlagerplatz zugeordnet wurden.

## Verantwortlichkeiten

Fertigung:

- Gesperrte Chargen physisch ins Sperrlager bringen.
- Den passenden Lagerplatz im System auswählen.
- Chargen korrekt einlagern.

QS:

- Sperrgrund prüfen.
- Entscheidung treffen, ob eine Charge entsperrt, vermüllt oder weiter untersucht wird.

Schichtleitung:

- Bei Unklarheiten unterstützen.
- Sicherstellen, dass gesperrte Chargen nicht weiterverarbeitet werden.

IT und Systembetreuung:

- Datenbanktabellen und Sperrlagerlogik warten.
- Fehler in Lagerplatz- oder Chargenzuordnung prüfen.

## Mindestprüfung vor einer Aktion

Vor einer Sperrlager-Aktion prüfen:

1. Ist die richtige Charge eingetragen?
2. Ist beim Sperren ein Sperrlagerplatz ausgewählt?
3. Existiert die Charge in `Fertigung.dbo.Chargen`?
4. Stimmen die Chargen im Eingabefeld bei Bulk-Aktionen?
5. Soll die Charge wirklich entsperrt oder vermüllt werden?

## Ergebnis

Nach korrekter Nutzung des Sperrlagers:

- Gesperrte Chargen sind sichtbar einem Platz zugeordnet.
- QS und Fertigung sehen den Sperrgrund.
- Aktionen sind in der Sperrlager-Historie nachvollziehbar.
- Entsperrte Chargen werden aus dem Sperrlager entfernt.
- Vermüllte Chargen bleiben gesperrt und verschwinden aus der offenen Liste.
