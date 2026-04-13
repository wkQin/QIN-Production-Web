# Endkontrolle Sauberraum

## Fuer wen ist diese Seite?

Diese Seite ist fuer die Erfassung von Fehlersammelkarten im Sauberraum.
Hier werden Gutteile, Fehlerarten und die wichtigsten Auftragsdaten zu einer Charge eingetragen.

Seite im System:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)

## Zweck

Mit dieser Maske wird dokumentiert:

- zu welcher Charge der Eintrag gehoert
- welche FA-Nummer bearbeitet wurde
- fuer welchen Kunden, welches Projekt, welchen Artikel und welches Dekor gearbeitet wurde
- wie viele Gutteile vorhanden sind
- welche Fehler in welcher Anzahl gefunden wurden
- ob es eine Bemerkung zur Karte gibt

## Aufbau der Seite

Die Seite besteht aus zwei Bereichen:

1. Auftragsdaten
2. Fehlersammelkarte

Unterhalb davon gibt es noch eine Tabelle:

- Letzte Eintraege bearbeiten

## Auftragsdaten ausfuellen

Links werden die allgemeinen Daten erfasst:

- Charge
- FA-Nummer
- Kunde
- Projekt
- Artikel
- Dekor
- Bemerkung

Wichtig:

- Nach Auswahl eines Kunden werden die passenden Projekte geladen.
- Nach Auswahl eines Projekts werden die passenden Artikel und Dekore geladen.
- Wenn es nur einen moeglichen Projekt-, Artikel- oder Dekorwert gibt, wird dieser teilweise automatisch vorausgewaehlt.

## Fehlersammelkarte ausfuellen

Rechts werden Datum, Gutteile und Fehler eingetragen.

Zuerst:

- Datum pruefen
- Gutteile eintragen

Dann die Fehlerwerte eintragen.

Erfasste Fehlerarten:

- Fusseln
- Nadelstiche
- Pickel
- Dekorfehler
- Farbfehler
- Flecken
- Nebel
- Vertiefung
- Oelflecken
- Tiefziehfehler / Blasen
- Fraes- / Stanzfehler
- Knicke / Weissbruch
- Kratzer

Wenn ein Fehler nicht vorhanden ist:

- Feld auf `0` lassen

## Speichern

Zum Speichern unten auf `Speichern` klicken.

Pflichtangaben vor dem Speichern:

- Charge
- FA-Nummer
- Artikel
- Dekor

Wenn eine dieser Angaben fehlt, erscheint eine Meldung und der Eintrag wird nicht gespeichert.

## Was passiert nach dem Speichern?

Wenn das Speichern erfolgreich war:

- erscheint eine kurze Erfolgsmeldung
- Charge, FA-Nummer und Bemerkung werden geleert
- alle Fehlerzahlen und Gutteile werden auf `0` gesetzt
- die Liste der letzten Eintraege wird neu geladen

Kunde, Projekt, Artikel und Dekor bleiben dabei erhalten. Das ist praktisch, wenn mehrere Karten fuer denselben Auftrag erfasst werden.

## Letzte Eintraege bearbeiten

Im unteren Bereich werden die letzten 10 Eintraege der angemeldeten Personalnummer angezeigt.

Moeglich ist dort:

- Eintrag loeschen
- Werte direkt in der Tabelle aendern

So funktioniert das Aendern:

1. Auf die gewuenschte Zelle klicken.
2. Den neuen Wert eingeben.
3. Mit `Enter` oder durch Verlassen des Feldes speichern.

## Loeschen

Ein Eintrag kann ueber das Papierkorb-Symbol geloescht werden.

Vor dem Loeschen kommt eine Sicherheitsabfrage.

## Praktischer Ablauf im Alltag

1. Charge und FA-Nummer eintragen.
2. Kunde auswaehlen.
3. Projekt auswaehlen.
4. Artikel und Dekor pruefen oder auswaehlen.
5. Datum pruefen.
6. Gutteile eintragen.
7. Fehlerzahlen eintragen.
8. Optional eine Bemerkung erfassen.
9. Speichern.
10. Falls noetig den Eintrag unten noch einmal kontrollieren oder korrigieren.

## Hinweise fuer den Betrieb

- Zahlen immer als Stueckzahl eintragen.
- Leere Fehlerfelder am besten als `0` belassen.
- Bei unklaren Zuordnungen zuerst Kunde, Projekt, Artikel und Dekor pruefen.
- Wenn mehrere Karten nacheinander fuer denselben Auftrag erfasst werden, spart die bestehende Vorauswahl Zeit.

## Relevante Code-Stellen

- Seitenaufbau und Eingabefelder: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:20)
- Speichern und Pflichtfeld-Pruefung: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:363)
- Letzte Eintraege und Inline-Bearbeitung: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:169)
