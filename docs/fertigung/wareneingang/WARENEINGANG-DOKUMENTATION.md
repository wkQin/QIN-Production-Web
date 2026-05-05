# Wareneingang Dokumentation

Allgemeine Dokumentation für den Wareneingang in QIN Production Web.

Stand: Update 3.2.4

## Zweck

Der Wareneingang dient dazu, angeliefertes Produktionsmaterial sauber zu erfassen, Chargen zu dokumentieren und Etiketten für die Weiterverarbeitung zu drucken.

Die Erfassung hilft dabei, Materialbewegungen nachvollziehbar zu machen und Qualitätsfälle früh sichtbar zu machen.

## Zielgruppe

Diese Dokumentation richtet sich an:

- Fertigung
- Wareneingang
- Schichtleitung
- QS
- Verwaltung

Für die technische Umsetzung gibt es ein eigenes Dokument:

`WARENEINGANG-BACKEND.md`

Für die direkte Arbeit am Arbeitsplatz gibt es eine Arbeitsanweisung:

`WARENEINGANG-ARBEITSANWEISUNG.md`

## Funktionsumfang

Der Wareneingang umfasst:

- Erfassen von Lieferanten
- Erfassen von Lieferscheinen
- Erfassen von Position und EBE-Nummer
- Bewerten des Warenzustands
- Dokumentieren von Palettentausch
- Erfassen von Mustermaterial
- Erfassen einer Dickenmessung
- Prüfen der Dickenmessung gegen Material-Sollwert und Toleranz
- Erfassen von Chargen und Mengen
- Automatisches Sperren von Chargen bei wiederholt falscher Dickenmessung
- Drucken von Chargenetiketten
- Anzeigen und Bearbeiten offener Wareneingänge
- Senden einer QS-Mail nach dem Speichern

## Seitenaufbau

Die Seite besteht aus drei Hauptbereichen.

### Stammdaten

In diesem Bereich werden die allgemeinen Daten des Wareneingangs erfasst.

Dazu gehören:

- Lieferant
- Lieferschein
- Position
- EBE-Nummer
- Zustand der Ware
- Palettentausch
- Mustermaterial
- Dickenmessung
- Bemerkung

### Chargenerfassung

In diesem Bereich werden Chargen und Mengen erfasst.

Chargen können gescannt oder manuell hinzugefügt werden.

Die Gesamtmenge wird in der Maske angezeigt.

### Offene Wareneingänge

Im Bereich `Offene Wareneingänge` werden noch nicht gebuchte Wareneingänge angezeigt.

Über die Tabelle kann ein bestehender Eintrag geladen und bearbeitet werden.

## Zustände der Ware

Es gibt drei Zustände:

### Gut

Die Ware ist in Ordnung.

Es ist keine zusätzliche QS-Meldung erforderlich.

### Mittel

Die Ware hat leichte Auffälligkeiten.

Eine Bemerkung ist sinnvoll, wenn die Auffälligkeit später nachvollziehbar sein soll.

### Schlecht

Die Ware hat deutliche Mängel oder muss durch QS geprüft werden.

Bei `Schlecht` ist eine Bemerkung Pflicht.

Zusätzlich muss QS informiert werden.

Nach dem Speichern sendet das System eine E-Mail an:

`qsintern@qin-form.de`

Bei Zustand `Schlecht` wird die E-Mail als wichtiger Prüffall gekennzeichnet.

## Palettentausch

Bei jedem Wareneingang muss angegeben werden, ob ein Palettentausch stattgefunden hat.

Die Bemerkung soll bei Besonderheiten kurz beschreiben, was getauscht wurde.

Aktueller Hinweis:

- Palettentausch macht die Bemerkung nicht mehr automatisch zum Pflichtfeld.
- Eine Bemerkung bleibt bei Zustand `Schlecht` Pflicht.
- Eine Bemerkung ist trotzdem sinnvoll, wenn Besonderheiten später nachvollziehbar sein sollen.

## Mustermaterial

Für Mustermaterial gibt es eine eigene Checkbox.

Wenn `Mustermaterial` aktiviert ist:

- Das Feld für den Materialnamen wird angezeigt.
- Die Dickenmessung wird ausgeblendet.
- Pflichtfeldprüfungen werden nicht erzwungen.

Mustermaterial wird verwendet, wenn das Material noch nicht wie normales Serienmaterial geprüft oder zugeordnet werden kann.

## Dickenmessung

Die Dickenmessung ist für relevante Lieferanten ein Pflichtfeld, außer wenn `Mustermaterial` aktiv ist.

Wenn kein Material-Sollwert gefunden wird, gilt der Standardbereich:

- `0,23 mm` bis `1,2 mm`

Wenn das Material in `dbo.Materialliste` gefunden wird, verwendet das System den dort gepflegten Sollwert aus `Dickenmessung`.

Die Materialsuche berücksichtigt:

- `Suchbegriff`
- `Beschreibung`
- `Beschreibung2`

Zum Sollwert wird eine erlaubte Toleranz angezeigt. Beispiel:

- Sollwert: `0,5 mm`
- Erlaubte Toleranz: `0,45 mm` bis `0,55 mm`

Das System akzeptiert Eingaben mit Komma oder Punkt.

Beispiele:

- `0,23`
- `0.23`
- `1,2`

Wenn die Dickenmessung außerhalb der erlaubten Toleranz liegt:

1. Das System zeigt zuerst ein großes Warnfenster.
2. Der Werker soll den Messwert prüfen und korrigieren.
3. Wenn beim nächsten Speicher-Versuch wieder ein falscher Wert eingetragen wird, werden vorhandene Chargen dieser Bestellung gesperrt.
4. Die Sperre wird in `dbo.Chargen.Gesperrt` gesetzt.
5. QSIntern erhält automatisch eine E-Mail.

Eine Sperre ist nur möglich, wenn Chargen vorhanden sind.

Wenn der Werker bereits eine Bemerkung eingetragen hat, bleibt diese erhalten. Die automatische Sperr-Bemerkung wird ergänzt.

## Chargen

Jede Lieferung kann eine oder mehrere Chargen enthalten.

Für jede Charge wird erfasst:

- Chargennummer
- Menge
- Art der Erfassung

Die Menge wird als Laufmeter oder Stück angezeigt.

## Etikettendruck

Für ausgewählte Chargen kann ein Etikett gedruckt werden.

Der Wareneingang nutzt testweise Zebra Browser Print, damit Etiketten lokal am Werker-PC direkt auf einem Zebra-Drucker ausgegeben werden können.

Das Etikett enthält:

- Charge
- Menge
- Material
- Eingangsdatum

Über `Auswahl Drucken` wird die markierte Charge gedruckt.

Über `Alle Drucken` werden alle in der aktuellen Erfassung vorhandenen Chargen als ZPL-Batch an den lokalen Zebra-Drucker gesendet.

Voraussetzung:

- Zebra Browser Print läuft auf dem Arbeitsplatz-PC.
- Der richtige Zebra-Drucker ist als Default Device gesetzt.
- Der Host der Weboberfläche steht in den Accepted Hosts von Zebra Browser Print.

Das Etikett muss auf das passende Gebinde oder den passenden Karton geklebt werden.

## Bearbeiten von Einträgen

Offene Wareneingänge können über die Tabelle `Offene Wareneingänge` erneut geladen werden.

Nach dem Laden können Angaben korrigiert und gespeichert werden.

Typische Korrekturen:

- Lieferschein
- Position
- EBE-Nummer
- Zustand
- Bemerkung
- Dickenmessung
- zusätzliche Chargen

## Qualitätsfälle

Ein Qualitätsfall liegt vor, wenn:

- Zustand `Schlecht` ausgewählt wird.
- Die Dickenmessung außerhalb des erlaubten Bereichs liegt.
- Chargen wegen wiederholt falscher Dickenmessung gesperrt wurden.
- Ware beschädigt oder auffällig ist.
- Chargen oder Mengen nicht plausibel sind.

In diesen Fällen:

1. Bemerkung im System eintragen.
2. QS informieren.
3. Ware nach interner Vorgabe kennzeichnen oder separieren.

Bei automatischer Chargensperre werden die betroffenen Chargen zusätzlich technisch gesperrt und müssen nach QS-Vorgabe behandelt werden.

Wenn der Wareneingang gespeichert wird, sendet das System eine QS-Mail an `qsintern@qin-form.de`.

Bei Zustand `Schlecht` enthält die E-Mail einen deutlichen Hinweis, dass der Vorgang zeitnah geprüft werden soll.

## Verantwortlichkeiten

Wareneingang und Fertigung:

- Daten korrekt erfassen.
- Chargen und Mengen prüfen.
- Etiketten korrekt drucken und anbringen.

QS:

- Qualitätsfälle prüfen.
- Weitere Maßnahmen festlegen.

Schichtleitung:

- Bei Unklarheiten unterstützen.
- Korrekturen freigeben, falls erforderlich.

## Mindestprüfung vor dem Speichern

Vor dem Speichern muss geprüft werden:

1. Lieferant stimmt.
2. Lieferschein stimmt.
3. Zustand wurde richtig bewertet.
4. Palettentausch ist korrekt gesetzt.
5. Dickenmessung ist eingetragen, falls erforderlich.
6. Chargen stimmen.
7. Mengen stimmen.
8. Bemerkung ist vorhanden, falls erforderlich.

## Ergebnis nach dem Speichern

Nach dem Speichern:

- Der Wareneingang ist im System erfasst.
- Chargen sind gespeichert.
- Die Tabelle `Offene Wareneingänge` wird aktualisiert.
- Bei Bedarf kann ein Etikett gedruckt werden.
- QS erhält eine systemgenerierte E-Mail an `qsintern@qin-form.de`.
- Bei Zustand `Schlecht` wird die QS-Mail als wichtiger Prüffall markiert.
- Bei wiederholt falscher Dickenmessung werden vorhandene Chargen gesperrt und QS wird informiert.
