# Live-Fertigung Dokumentation

Diese Dokumentation beschreibt den fachlichen Startstand der neuen Verwaltungsseite `Live-Fertigung`.

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

## Aktueller Startstand

Die Seite ist zunächst als Bereichsauswahl vorbereitet.

Aktuell gilt:

- `Endkontrolle` ist beim Öffnen automatisch ausgewählt.
- Die übrigen Bereiche sind bereits als Tabs sichtbar und auswählbar.
- Für `Endkontrolle` gibt es jetzt einen neuen Aufbau mit `Raum 1` oben und `Raum 2` unten links.
- Die Tische sind als vorbereitete Karten direkt an den beschriebenen Raumwänden platziert.
- Die Tischkarten zeigen aktuell Testdaten für Benutzer, Material, gemachte Menge und Zielmenge.
- Die Testdaten berücksichtigen, dass ein Tisch am selben Tag auch mehrere Materialien haben kann.
- Belegte Test-Tische werden grün markiert, freie Test-Tische rot.
- Zwei vorbereitete Icon-Flächen je Tisch liegen neben der Tischkarte und zeigen im Testdesign heutige Einträge und die letzten Tage per Hover.
- Die echte Live-Datenanzeige folgt erst im nächsten Ausbauschritt.
- Die geplante Hover- oder Info-Funktion für ältere und fertige Informationen ist noch nicht umgesetzt.

## Erster Ausbau: Endkontrolle

Die Endkontrolle ist der erste Bereich, der vollständig aufgebaut werden soll.

Geplant ist dort:

- eine klare Live-Übersicht der relevanten Arbeitsplätze
- ein neuer visueller Entwurf passend zum tatsächlichen Arbeitsablauf
- die räumliche Trennung zwischen `Raum 1` und `Raum 2`
- pro Arbeitsplatz der Name des Mitarbeiters
- das aktuell bearbeitete Material
- bei Bedarf mehrere Materialien pro Tag
- die gemachte Menge
- die Zielmenge
- eine Detailansicht für heutige Einträge
- eine Verlaufansicht für die letzten Tage

Zusätzlich ist vorgesehen, dass die Verwaltung über eine spätere Zusatzfunktion auch ältere oder bereits fertige Informationen sehen kann, ohne den Live-Überblick zu verlassen.

## Nutzen für die Verwaltung

Die Seite soll die Suche über mehrere einzelne Verwaltungsseiten reduzieren.

Statt zwischen verschiedenen Bereichen wechseln zu müssen, soll ein zentraler Einstieg entstehen, der:

- schneller Orientierung gibt
- aktuelle Auslastung sichtbar macht
- Rückfragen an die Fertigung reduziert
- Ziel- und Mengenabweichungen früher erkennbar macht

## Nächste Ausbaustufen

Die nächsten fachlichen Schritte sind:

1. Endkontrolle als ersten Live-Bereich fachlich und visuell aufbauen.
2. Die benötigten Live-Daten je Arbeitsplatz anbinden.
3. Die Hover- oder Info-Ansicht für ältere und fertige Informationen ergänzen.
4. Danach die weiteren Bereiche nacheinander in dieselbe Struktur übernehmen.
