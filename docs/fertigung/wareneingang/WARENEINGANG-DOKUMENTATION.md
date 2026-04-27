# Wareneingang Dokumentation

Allgemeine Dokumentation für den Wareneingang in QIN Production Web.

Stand: Update 3.2.2

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
- Erfassen einer Dickenmessung
- Erfassen von Chargen und Mengen
- Drucken von Chargenetiketten
- Anzeigen und Bearbeiten offener Wareneingänge

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
- Dickenmessung
- Bemerkung

### Chargenerfassung

In diesem Bereich werden Chargen und Mengen erfasst.

Chargen können gescannt oder manuell hinzugefügt werden.

Die Gesamtmenge wird in der Maske angezeigt.

### Historie

In der Historie werden offene Wareneingänge angezeigt.

Über die Historie kann ein bestehender Eintrag geladen und bearbeitet werden.

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

## Palettentausch

Bei jedem Wareneingang muss angegeben werden, ob ein Palettentausch stattgefunden hat.

Wenn Palettentausch ausgewählt wird, ist eine Bemerkung Pflicht.

Die Bemerkung soll kurz beschreiben, was getauscht wurde.

## Dickenmessung

Die Dickenmessung ist für relevante Lieferanten ein Pflichtfeld.

Erlaubter Bereich:

- `0,23 mm` bis `1,2 mm`

Das System akzeptiert Eingaben mit Komma oder Punkt.

Beispiele:

- `0,23`
- `0.23`
- `1,2`

## Chargen

Jede Lieferung kann eine oder mehrere Chargen enthalten.

Für jede Charge wird erfasst:

- Chargennummer
- Menge
- Art der Erfassung

Die Menge wird als Laufmeter oder Stück angezeigt.

## Etikettendruck

Für ausgewählte Chargen kann ein Etikett gedruckt werden.

Das Etikett enthält:

- Charge
- Menge
- Material
- Eingangsdatum

Das Etikett muss auf das passende Gebinde oder den passenden Karton geklebt werden.

## Bearbeiten von Einträgen

Offene Wareneingänge können über die Historie erneut geladen werden.

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
- Ware beschädigt oder auffällig ist.
- Chargen oder Mengen nicht plausibel sind.

In diesen Fällen:

1. Bemerkung im System eintragen.
2. QS informieren.
3. Ware nach interner Vorgabe kennzeichnen oder separieren.

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
- Die Historie wird aktualisiert.
- Bei Bedarf kann ein Etikett gedruckt werden.
- QS kann bei relevanten Fällen informiert werden.
