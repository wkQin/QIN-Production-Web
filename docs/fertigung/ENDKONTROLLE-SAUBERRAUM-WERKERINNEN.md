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

Die Seite besteht aus mehreren klar getrennten Bereichen:

1. Auftragsdaten
2. Fehlersammelkarte
3. Hinweise zur Bedienung
4. Letzte Eintraege

Neu in der aktuellen Version:

- Die Seite ist optisch einfacher und besser lesbar aufgebaut.
- Oben gibt es direkte Hinweise, wie neue Eintraege gespeichert oder alte Eintraege bearbeitet werden.
- Unten koennen Eintraege ueber einen klaren `Bearbeiten`-Button geoeffnet werden.
- In grossen Listen kann jetzt mit gedrueckter Maustaste gezogen werden, damit nicht genau der Scrollbalken getroffen werden muss.

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

## Hinweise direkt in der Seite

Im oberen Bereich steht jetzt eine kurze Anleitung.

Damit ist sofort sichtbar:

- wie neue Eintraege gespeichert werden
- wie bestehende Eintraege bearbeitet werden
- wann gerade ein Bearbeitungsmodus aktiv ist

Wenn ein alter Eintrag bearbeitet wird, erscheint oben ein deutlich sichtbarer Hinweis:

- `Bearbeitungsmodus aktiv`

Zusatz:

- Der ausgewaehlte Eintrag wird unten markiert.
- Oben kann dann weitergearbeitet werden.

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
- das Formular wird wieder fuer eine neue Erfassung vorbereitet
- die Liste der letzten Eintraege wird aktualisiert

Kunde, Projekt, Artikel und Dekor bleiben dabei erhalten. Das ist praktisch, wenn mehrere Karten fuer denselben Auftrag erfasst werden.

## Letzte Eintraege bearbeiten

Im unteren Bereich werden die letzten 10 Eintraege der angemeldeten Personalnummer angezeigt.

Moeglich ist dort:

- Eintrag loeschen
- Eintrag zum Bearbeiten oben laden

So funktioniert das Bearbeiten jetzt:

1. Unten auf `Bearbeiten` klicken.
2. Der Eintrag wird oben in das Formular geladen.
3. Werte oben anpassen.
4. Auf `Aktualisieren` klicken.

Wichtig:

- Es wird nur der ausgewaehlte Eintrag aktualisiert.
- Es wird kein neuer doppelter Eintrag erzeugt.
- Mit `Abbrechen` oder `Neue Erfassung starten` kann der Bearbeitungsmodus verlassen werden.

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

Wenn ein alter Eintrag geaendert werden soll:

1. Unten auf `Bearbeiten` klicken.
2. Oben die noetigen Felder aendern.
3. Auf `Aktualisieren` klicken.

## Scrollen mit der Maus

Vor allem auf Notebooks ist der Scrollbalken oft schwer zu treffen.

Darum gilt jetzt:

- Im unteren Tabellenbereich kann mit gedrueckter linker Maustaste gezogen werden.
- Dabei bewegt sich die Liste mit der Maus.
- So kann leichter nach unten oder zur Seite gescrollt werden.

Das ist hilfreich:

- wenn viele Spalten sichtbar sind
- wenn der Scrollbalken zu klein ist
- wenn mit Touchpad oder Notebook-Maus gearbeitet wird

## Hinweise fuer den Betrieb

- Zahlen immer als Stueckzahl eintragen.
- Leere Fehlerfelder am besten als `0` belassen.
- Bei unklaren Zuordnungen zuerst Kunde, Projekt, Artikel und Dekor pruefen.
- Wenn mehrere Karten nacheinander fuer denselben Auftrag erfasst werden, spart die bestehende Vorauswahl Zeit.
- Fuer Korrekturen immer den `Bearbeiten`-Button unten verwenden.

## Relevante Code-Stellen

- Seitenaufbau und Bedienhinweise: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)
- Speichern und Aktualisieren: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)
- Letzte Eintraege und Bearbeiten-Button: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)
