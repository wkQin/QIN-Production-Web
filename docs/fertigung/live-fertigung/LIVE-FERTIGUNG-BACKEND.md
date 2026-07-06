# Live-Fertigung Backend

Diese Dokumentation beschreibt den technischen Stand der Verwaltungsseite `Live-Fertigung`.

## Seite im System

- Route: `/verwaltung/live-fertigung`
- Datei: [LiveFertigung.razor](/Components/Pages/Verwaltung/LiveFertigung.razor:1)
- Styles: [LiveFertigung.razor.css](/Components/Pages/Verwaltung/LiveFertigung.razor.css:1)
- Service: [LiveFertigungService.cs](/Data/LiveFertigungService.cs:1)
- Modelle: [LiveFertigungModels.cs](/Data/LiveFertigungModels.cs:1)

## Aktueller Umfang

Die Seite hat für `Endkontrolle` eine erste echte Datenanbindung.

Aktuell umgesetzt sind:

- Verwaltungsroute für die neue Seite
- Bereichsauswahl über Tabs
- Standardauswahl `Endkontrolle`
- Endkontrolle-Aufbau mit `Raum 1`, `Raum 2` und Tischkarten
- Datenservice `LiveFertigungService`
- echte Sauberraum-Zuweisungen aus dem Schichtplan
- echte Wochen- und historische Endkontrolle-Einträge aus `dbo.Table1`
- Anzeige von Benutzer, ein oder mehreren Materialien, Menge und Zielmenge
- seitlich ausgelagerte Hover-Aktionsleisten für heutige Einträge und Verlauf der letzten Tage

## Datenquellen

Die Endkontrolle nutzt aktuell zwei Datenbereiche:

- `Fertigung.dbo.SchichtplanPlan`, `SchichtplanEintrag`, `SchichtplanEintragBenutzer`, `SchichtplanArbeitsplatz` und `SchichtplanMaterialStamm`
- `qinFSK\table1.dbo.Table1` mit `dbo.LoginDaten`

Der Schichtplan liefert:

- Benutzer
- Personalnummer
- Sauberraum-Zuweisung inklusive Arbeitsplatz `Tisch 1` bis `Tisch 12`
- Material 1 und Material 2
- Zielmenge je Material aus dem Tages-Snapshot `SchichtplanEintrag.MaterialZielMenge` und `Material2ZielMenge`

`dbo.Table1` liefert:

- Einträge der letzten 7 Tage
- Gutteile
- Schlechtteile als Summe der Fehlerfelder
- Artikel als Material
- Charge
- Bemerkungen
- Verlauf der letzten Tage

Die Zuordnung zur Live-Fertigung erfolgt über `SchichtplanArbeitsplatz.ArbeitsplatzName`. Ein Schichtplan-Arbeitsplatz `Tisch 7` wird dadurch direkt auf die Live-Karte `Tisch 7` gesetzt. Freie Plätze bleiben rot.

Historische Zielmengen werden aus dem Schichtplan-Tages-Snapshot gelesen. Wird die Tagesmenge eines Materials später im Materialstamm geändert, ändern sich alte Live-Fertigung-Verlaufstage dadurch nicht mehr rückwirkend.

## Noch Offen

Folgende technische Schritte sind noch offen:

- Prüfung, ob Mehrfachzuweisungen pro Tisch im Tagesbetrieb getrennt oder zusammengefasst angezeigt werden sollen
- Detailansicht für alte und fertige Informationen über die aktuelle Wochen- und Verlaufsansicht hinaus
- gemeinsame Datenmodelle und Services für weitere Live-Fertigung-Bereiche

## Geplanter Technischer Ausbau

Voraussichtlich wird die Seite später Folgendes benötigen:

1. Eine fachliche Entscheidung zur Darstellung mehrerer Schichten oder Personen auf demselben Tisch.
2. Einen gemeinsamen Datenvertrag für Bereich, Arbeitsplatz, Mitarbeiter, Material und Mengen.
3. Eine klare Trennung zwischen aktueller Live-Information, älterer Information und bereits fertiger Information.
4. Eine strukturierte Wiederverwendung von Karten- oder Arbeitsplatz-Komponenten für mehrere Bereiche.
