# Wareneingang

## Fuer wen ist diese Seite?

Diese Seite ist fuer Mitarbeitende in der Fertigung gedacht, die angelieferte Ware erfassen, Chargen aufnehmen und bei Bedarf Etiketten drucken.

Seite im System:

- [Wareneingang.razor](/Components/Pages/Fertigung/Wareneingang.razor:1)

## Zweck

Mit dieser Maske wird dokumentiert:

- von welchem Lieferanten die Ware kommt
- zu welchem Lieferschein, welcher Position und welcher EBE-Nummer der Eingang gehoert
- in welchem Zustand die Ware angekommen ist
- ob ein Palettentausch stattgefunden hat
- welche Dickenmessung festgestellt wurde
- welche Chargen mit welcher Menge zum Wareneingang gehoeren

## Aufbau der Seite

Die Seite besteht aus drei Hauptbereichen:

1. Stammdaten
2. Chargen Erfassung
3. Letzte Buchungen

Im Kopf der Seite ist immer sichtbar, ob gerade eine neue Buchung oder die Bearbeitung eines vorhandenen Eintrags aktiv ist.

## So wird ein neuer Wareneingang erfasst

1. Lieferant auswaehlen.
2. Lieferschein eintragen.
3. Position und EBE-Nr. eintragen.
4. Zustand der Ware auswaehlen.
5. Palettentausch auf `Ja` oder `Nein` setzen.
6. Dickenmessung eintragen.
7. Falls noetig eine Bemerkung ergaenzen.
8. Charge scannen oder eintippen.
9. Menge zur Charge eintragen.
10. Charge mit `Enter` oder ueber `Hinzufuegen` in die Liste uebernehmen.
11. Alle Chargen pruefen.
12. Ueber `Wareneingang Buchen` speichern.

## Pflichtangaben vor dem Speichern

Vor dem Speichern muessen mindestens diese Angaben vorhanden sein:

- Lieferant
- Lieferschein
- Dickenmessung mit gueltigem Wert

Zusaetzlich gilt:

- Bei `Palettentausch = Ja` ist eine Bemerkung zwingend.
- Bei Zustand `Schlecht` ist eine Bemerkung zwingend.
- Die Dickenmessung muss zwischen `0,23` und `1,2` mm liegen.

## Chargen richtig erfassen

Die Chargenliste wird rechts in der Seite aufgebaut.

Wichtig:

- Eine Charge kann per Scanner oder per Tastatur erfasst werden.
- Mit `Enter` wird die Charge direkt hinzugefuegt.
- Die Gesamtmenge wird oben rechts als `Gesamt` mitgerechnet.
- Bereits hinzugefuegte Chargen koennen wieder entfernt werden.
- Die Mengen in der Liste sollten direkt nach dem Scannen noch einmal geprueft werden.

## Etikett fuer eine Charge drucken

Wenn fuer eine Charge ein Etikett gebraucht wird:

1. Die gewuenschte Charge in der Liste anklicken.
2. `Auswahl Drucken` verwenden.

Das Drucken ist nur moeglich, wenn eine Charge markiert ist.

## Historie und Bearbeiten

Im unteren Bereich stehen die letzten Buchungen.

So wird ein vorhandener Eintrag bearbeitet:

1. In der Historie die passende Zeile anklicken.
2. Der Eintrag wird oben in die Maske geladen.
3. Die Statusanzeige wechselt auf `Edit`.
4. Werte anpassen.
5. Ueber `Aenderungen Speichern` sichern.

Mit `Leeren` wird die Maske wieder auf eine neue Erfassung zurueckgesetzt.

## Wichtiger Hinweis beim Bearbeiten

Nach aktuellem Stand werden beim Laden eines alten Eintrags nicht alle Felder sichtbar in die Maske zurueckgeschrieben.

Darum gilt im Alltag:

- Bemerkung immer aktiv pruefen und bei Bedarf neu eintragen.
- Palettentausch vor dem Speichern bewusst neu setzen.
- Chargenmengen nach dem Laden eines alten Eintrags besonders sorgfaeltig kontrollieren.

## Was passiert nach dem Speichern?

Wenn das Speichern erfolgreich ist:

- erscheint eine Erfolgsmeldung
- die Historie wird neu geladen
- die Maske wird geleert
- im Hintergrund wird eine QS-E-Mail verschickt

## Praktischer Ablauf im Alltag

1. Lieferant und Lieferschein zuerst erfassen.
2. Zustand und Palettentausch direkt mitpruefen.
3. Dickenmessung sofort eintragen, solange die Ware vorliegt.
4. Chargen nacheinander scannen.
5. Gesamtmenge und einzelne Mengen kurz gegenpruefen.
6. Bei Auffaelligkeiten eine klare Bemerkung hinterlegen.
7. Erst danach buchen.

## Hinweise fuer den Betrieb

- Die Dickenmessung kann mit Komma oder Punkt eingegeben werden.
- Die Einheit fuer die Dickenmessung ist `mm`.
- Bei Zustand `Schlecht` sollte die Bemerkung moeglichst konkret sein.
- Wenn der Druck nicht funktioniert, zuerst pruefen, ob die richtige Charge markiert ist.
- Wenn eine alte Buchung korrigiert werden soll, immer ueber die Historie arbeiten und nicht einfach neu erfassen.

## Relevante Code-Stellen

- Seitenaufbau und Bedienlogik: [Wareneingang.razor](/Components/Pages/Fertigung/Wareneingang.razor:1)
- Datenzugriffe und Speichern: [WareneingangService.cs](/Data/WareneingangService.cs:1)
