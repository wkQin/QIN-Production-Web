# Wareneingang Arbeitsanweisung

Arbeitsanweisung für Mitarbeiterinnen und Mitarbeiter im Wareneingang.

Stand: Update 3.2.2

## Ziel

Diese Arbeitsanweisung beschreibt, wie angeliefertes Produktionsmaterial im System erfasst wird.

Am Ende müssen folgende Informationen im System stehen:

- Lieferant
- Lieferschein
- Position
- EBE-Nummer, falls vorhanden
- Zustand der Ware
- Palettentausch
- Dickenmessung, wenn erforderlich
- Chargen
- Mengen
- Bemerkung, wenn erforderlich

## Vor dem Start

Bereitlegen:

- Lieferschein
- Material oder Gebinde
- Chargenetiketten
- Messmittel für Dickenmessung, falls erforderlich
- Scanner

## System öffnen

1. QIN Production Web öffnen.
2. Zum Bereich `Fertigung` wechseln.
3. `Wareneingang` öffnen.

## Neuen Wareneingang erfassen

### 1. Lieferant auswählen

Im Feld `Lieferant` den passenden Lieferanten auswählen.

Wichtig:

- Immer den Lieferanten vom Lieferschein verwenden.
- Bei Unsicherheit Rücksprache mit Schichtleitung oder QS halten.

### 2. Lieferschein eintragen

Im Feld `Lieferschein` die Lieferscheinnummer eintragen.

Wichtig:

- Die Nummer vollständig übernehmen.
- Keine zusätzlichen Leerzeichen eintragen.

### 3. Position eintragen

Im Feld `Position` die Position vom Lieferschein eintragen.

Wenn keine Position vorhanden ist:

- Feld leer lassen oder nach interner Vorgabe ausfüllen.

### 4. EBE-Nummer eintragen

Im Feld `EBE-Nr.` die EBE-Nummer eintragen, falls vorhanden.

Wenn keine EBE-Nummer vorhanden ist:

- Feld leer lassen.

## Ware prüfen

### 5. Zustand auswählen

Im Feld `Zustand der Ware` einen Wert auswählen:

- `Gut`: Ware ist in Ordnung.
- `Mittel`: Ware hat leichte Auffälligkeiten.
- `Schlecht`: Ware hat deutliche Mängel oder muss durch QS geprüft werden.

Bei Zustand `Schlecht`:

1. Bemerkung schreiben.
2. QS informieren.
3. Ware nach interner Vorgabe kennzeichnen oder separieren.

Nach dem Speichern sendet das System zusätzlich eine E-Mail an `qsintern@qin-form.de`.

### 6. Palettentausch auswählen

Bei `Palettentausch` immer `Ja` oder `Nein` auswählen.

Wenn `Ja` ausgewählt wird:

- In der Bemerkung kurz beschreiben, was getauscht wurde.

### 7. Dickenmessung eintragen

Wenn das Feld `Dickenmessung` angezeigt wird, ist es ein Pflichtfeld.

Erlaubter Bereich:

- Minimum: `0,23 mm`
- Maximum: `1,2 mm`

Beispiele für gültige Eingaben:

- `0,23`
- `0,50`
- `1,2`

Wenn der Wert außerhalb des Bereichs liegt:

1. Wert erneut prüfen.
2. Messung wiederholen.
3. Bei Auffälligkeit QS informieren.

## Chargen erfassen

### 8. Charge scannen

Den Barcode der Charge in das Feld `Charge scannen` scannen.

Danach die Menge eintragen.

Einheit:

- Laufmeter oder Stück, je nach Material.

### 9. Charge hinzufügen

Eine Charge wird hinzugefügt durch:

- Scanner mit Enter
- oder Klick auf `Hinzufügen`

Nach dem Hinzufügen erscheint die Charge in der Chargenliste.

### 10. Chargenliste prüfen

Vor dem Speichern prüfen:

- Ist jede Charge vorhanden?
- Stimmt jede Menge?
- Ist die Gesamtmenge plausibel?
- Gibt es doppelte oder falsche Chargen?

Falsche Chargen können über das `X` entfernt werden.

## Etikett drucken

### 11. Charge auswählen

In der Chargenliste die gewünschte Charge anklicken.

### 12. Etikett drucken

Auf `Auswahl Drucken` klicken.

Danach:

1. Druckdialog prüfen.
2. Etikett drucken.
3. Etikett auf das richtige Gebinde oder den richtigen Karton kleben.

Wichtig:

- Etikett und Charge müssen zusammenpassen.
- Bei falschem Etikett nicht weiterbuchen, sondern neu drucken.

## Speichern

### 13. Wareneingang buchen

Wenn alle Angaben geprüft sind, auf `Wareneingang Buchen` klicken.

Nach erfolgreichem Speichern:

- Die Maske wird geleert.
- Der Eintrag erscheint in der Historie.
- Die nächste Ware kann erfasst werden.
- QS erhält automatisch eine E-Mail an `qsintern@qin-form.de`.
- Bei Zustand `Schlecht` ist diese E-Mail als wichtiger Prüffall markiert.

## Bestehenden Eintrag bearbeiten

Wenn ein Eintrag korrigiert werden muss:

1. In der Historie auf den Eintrag klicken.
2. Die Daten werden in die Maske geladen.
3. Korrektur vornehmen.
4. Auf `Änderungen Speichern` klicken.

Wichtig:

- Nur korrigieren, wenn klar ist, welcher Eintrag gemeint ist.
- Bei Unsicherheit Rücksprache halten.

## Pflichtfelder

Diese Angaben müssen vorhanden sein:

- Lieferant
- Lieferschein
- Zustand
- Dickenmessung, wenn das Feld angezeigt wird
- Bemerkung bei Palettentausch
- Bemerkung bei Zustand `Schlecht`

## Häufige Fehler

### Lieferant fehlt

Lieferant auswählen und erneut speichern.

### Lieferschein fehlt

Lieferscheinnummer eintragen und erneut speichern.

### Bemerkung fehlt

Bemerkung eintragen, wenn Palettentausch ausgewählt wurde oder Zustand `Schlecht` ist.

### Dickenmessung ungültig

Wert prüfen und im erlaubten Bereich eintragen.

### Falsche Charge

Falsche Charge aus der Liste entfernen und richtige Charge scannen.

## Abschlusskontrolle

Vor dem Speichern immer prüfen:

1. Lieferant stimmt.
2. Lieferschein stimmt.
3. Zustand ist richtig ausgewählt.
4. Dickenmessung ist eingetragen, falls erforderlich.
5. Alle Chargen sind vorhanden.
6. Alle Mengen stimmen.
7. Bemerkung ist vorhanden, falls erforderlich.
8. Etiketten wurden korrekt gedruckt und angebracht.

## Verhalten bei Problemen

Bei technischen Problemen:

- Seite einmal neu laden.
- Wenn der Fehler bleibt, Vorgesetzten oder IT informieren.

Bei Qualitätsproblemen:

- Ware nicht einfach weiterbuchen.
- QS informieren.
- Bemerkung im System eintragen.
- Wareneingang speichern, damit die QS-Mail ausgelöst wird.
