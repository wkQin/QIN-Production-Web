# Live-Fertigung Dokumentation

Diese Dokumentation beschreibt den fachlichen Stand der Verwaltungsseite `Live-Fertigung`.

## Zweck

Die Seite soll mehrere wichtige Produktionsbereiche in einer gemeinsamen Live-Ansicht bündeln.

Geplant sind aktuell:

- Endkontrolle
- Wareneingang
- Thermoformung
- UV
- Stanzen
- Fräsen

Ziel ist, dass die Verwaltung und später auch andere berechtigte Nutzer schnell sehen können:

- welcher Bereich gerade ausgewählt ist
- welche Arbeitsplätze aktiv sind
- welcher Mitarbeiter an welchem Platz arbeitet
- welches Material aktuell bearbeitet wird
- welche gemachte Menge bereits vorliegt
- welche Zielmenge geplant ist

## Aktueller Stand

Die Seite ist als Bereichsauswahl vorbereitet und die Endkontrolle hat eine erste echte Datenanbindung.

Aktuell gilt:

- `Endkontrolle` ist beim Öffnen automatisch ausgewählt.
- Die übrigen Bereiche sind bereits als Tabs sichtbar und auswählbar.
- Für `Endkontrolle` gibt es einen Aufbau mit `Raum 1` oben und `Raum 2` unten links.
- Die Tische sind als Karten direkt an den beschriebenen Raumwänden platziert.
- Die Tischkarten zeigen echte Sauberraum-Zuweisungen aus dem Schichtplan und verwenden die dort gepflegten Tisch-Arbeitsplätze.
- Die Tischkarten zeigen Benutzer, Material, gemachte Menge und Zielmenge.
- Mehrere Materialien pro Schichtplan-Zuweisung werden als Materialchips angezeigt.
- Belegte Tische werden grün markiert, freie Tische rot.
- Die erste Hover-Fläche zeigt Endkontrolle-Einträge der letzten 7 Tage mit Datum, Material, Charge, Gutteilen und Schlechtteilen ohne Uhrzeit.
- Die zweite Hover-Fläche zeigt einen Verlauf der letzten Tage mit Datum, Material, Menge und Zielmenge.
- Zielmengen werden als Tages-Snapshot im Schichtplan gespeichert, damit spätere Änderungen im Materialstamm alte Verlaufstage nicht rückwirkend verändern.
- Ein Schichtplan-Arbeitsplatz `Tisch 7` wird direkt auf die Live-Karte `Tisch 7` gesetzt.

## Erster Ausbau: Endkontrolle

Die Endkontrolle ist der erste Bereich, der vollständig aufgebaut werden soll.

Geplant ist dort:

- eine klare Live-Übersicht der relevanten Arbeitsplätze
- die räumliche Trennung zwischen `Raum 1` und `Raum 2`
- pro Arbeitsplatz der Name des Mitarbeiters
- das aktuell bearbeitete Material
- bei Bedarf mehrere Materialien pro Tag
- die gemachte Menge
- die Zielmenge
- eine Detailansicht für Einträge der letzten 7 Tage
- eine Verlaufansicht für die letzten Tage

## Nutzen Für Die Verwaltung

Die Seite soll die Suche über mehrere einzelne Verwaltungsseiten reduzieren.

Statt zwischen verschiedenen Bereichen wechseln zu müssen, soll ein zentraler Einstieg entstehen, der:

- schneller Orientierung gibt
- aktuelle Auslastung sichtbar macht
- Rückfragen an die Fertigung reduziert
- Ziel- und Mengenabweichungen früher erkennbar macht

## Nächste Ausbaustufen

Die nächsten fachlichen Schritte sind:

1. Entscheiden, ob mehrere Schichten oder mehrere Personen auf demselben Tisch gemeinsam oder getrennt angezeigt werden.
2. Die Hover- oder Info-Ansicht für ältere und fertige Informationen weiter ausbauen.
3. Danach die weiteren Bereiche nacheinander in dieselbe Struktur übernehmen.
