# Live-Fertigung Backend

Diese Dokumentation beschreibt den aktuellen technischen Startstand der Verwaltungsseite `Live-Fertigung`.

## Seite im System

- Route: `/verwaltung/live-fertigung`
- Datei: [LiveFertigung.razor](/Components/Pages/Verwaltung/LiveFertigung.razor:1)
- Styles: [LiveFertigung.razor.css](/Components/Pages/Verwaltung/LiveFertigung.razor.css:1)

## Aktueller Umfang

Die Seite enthält im Startstand noch keine angebundenen Live-Daten.

Aktuell umgesetzt sind:

- Verwaltungsroute für die neue Seite
- Bereichsauswahl über Tabs
- Standardauswahl `Endkontrolle`
- neuer Endkontrolle-Aufbau mit `Raum 1`, `Raum 2` und vorbereiteten Tischkarten
- statische Testdaten für Tischbelegung, Benutzer, ein oder mehrere Materialien, Menge und Zielmenge
- seitlich ausgelagerte Hover-Aktionsleisten für heutige Einträge und Verlauf der letzten Tage
- vorbereitete Inhaltsfläche pro ausgewähltem Bereich
- vorbereitete Beschreibung für den geplanten späteren Ausbau

## Bereichsauswahl

Die Tabs werden aktuell direkt in der Seite definiert.

Pro Bereich sind hinterlegt:

- technischer Schlüssel
- sichtbarer Name
- Icon
- Kurzbeschreibung
- Beschreibung für den Inhaltsbereich
- geplanter Fokus des Bereichs

Die aktive Auswahl wird lokal über `ActiveAreaKey` gesteuert.

Standardwert:

- `Endkontrolle`

## Noch nicht umgesetzt

Folgende technische Schritte sind noch offen:

- Anbindung an echte Fertigungs- und Live-Daten
- Auflösung einzelner Arbeitsplätze pro Bereich
- Ersetzen der statischen Testdaten durch Daten aus Schichtplan und `dbo.Table1`
- Befüllung der Endkontrolle-Arbeitsplätze mit echten Arbeitsplatzinformationen
- Zuordnung von Mitarbeiter, Material, Zielmenge und gemachter Menge
- spätere Hover- oder Detailansicht für alte und fertige Informationen
- eventuelle gemeinsame Datenmodelle und Services für die Bereichskarten

## Geplanter technischer Ausbau

Voraussichtlich wird die Seite später Folgendes benötigen:

1. Ein gemeinsames Datenmodell für Bereich, Arbeitsplatz, Mitarbeiter, Material und Mengen.
2. Einen Service für Live-Fertigungsdaten oder mehrere Bereichs-Services mit einheitlichem Rückgabeformat.
3. Eine klare Trennung zwischen:
   - aktuelle Live-Information
   - ältere Information
   - bereits fertige Information
4. Eine strukturierte Wiederverwendung von Karten- oder Arbeitsplatz-Komponenten für mehrere Bereiche.

## Wichtiger Hinweis

Die Seite ist aktuell bewusst nur als navigierbarer Startaufbau umgesetzt.

Damit kann die visuelle und fachliche Struktur zuerst abgestimmt werden, bevor echte Live-Daten und Bereichslogik angebunden werden.
