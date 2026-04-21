# Schichtplan Produktionsmonitor Technical

## Zweck

Diese Dokumentation beschreibt den aktuellen technischen Stand des Schichtplans im Dashboard und in der Monitoransicht.

Der Schichtplan ist aktuell ein UI-Prototyp für die Fertigung. Die Daten kommen noch nicht aus der Datenbank, sondern aus statischen Demo-Daten im Komponenten-Code.

Relevante Dateien:

- [Home.razor](/Components/Pages/Home.razor:1)
- [ShiftPlanBoard.razor](/Components/Shared/ShiftPlanBoard.razor:1)
- [ShiftPlanBoard.razor.css](/Components/Shared/ShiftPlanBoard.razor.css:1)
- [App.razor](/Components/App.razor:1)

## Route und Einbindung

Der Schichtplan ist aktuell auf dem Dashboard eingebunden.

Seitenroute:

- `/`

Einbindung:

- [Home.razor](/Components/Pages/Home.razor:1)

Die Dashboard-Seite rendert die Komponente:

- `<ShiftPlanBoard />`

## Aktueller Funktionsumfang

Der aktuelle Stand umfasst:

- eingebettete Übersicht im Dashboard
- Monitoransicht per Button `Monitoransicht`
- automatische Skalierung für Übersicht und Vollbild
- feste Bereiche mit Maschinen und Schichtspalten
- Karten je Schicht mit Material, Mitarbeiterinnen und Mitarbeitern sowie Zusatztext
- farbliche Trennung der Abteilungen
- Anzeige von `KW`, Datum und `Zuletzt aktualisiert`

Nicht enthalten im aktuellen Stand:

- Datenbankanbindung
- Bearbeiten oder Speichern von Schichtplandaten
- Benutzerrollen oder Freigabeworkflow
- Druckfunktion

## UI-Aufbau

### Eingebettete Ansicht

In der Dashboard-Ansicht besteht der Schichtplan aus:

1. einem minimalen Kopfbereich mit dem Button `Monitoransicht`
2. der Plan-Tafel mit Kopfzeile
3. Bereichsblöcken und Maschinenzeilen
4. drei Schichtspalten `Nachtschicht`, `Frühschicht`, `Spätschicht`

### Monitoransicht

Im Vollbild wird derselbe Plan als eigene Tafel dargestellt.

Merkmale:

- Vollbild ohne normales Dashboard-Chrome
- eigener `Schließen`-Button oben rechts
- automatische Anpassung an verfügbare Höhe und Breite
- Fokus auf reine Planansicht

## Datenquelle im aktuellen Stand

Die Daten sind momentan statisch im Komponenten-Code definiert.

Siehe:

- `BuildSections()` in [ShiftPlanBoard.razor](/Components/Shared/ShiftPlanBoard.razor:174)

Aktuell sind folgende Bereiche enthalten:

- `Thermoformung`
- `Fräsen`
- `Stanzen`
- `UV`
- `Ohne Bereich`
- `Sauberraum`
- `Sonstiges`

Jeder Bereich enthält:

- Bereichsname
- Beschreibung
- Theme-Klasse für die Darstellung
- Liste von Maschinen bzw. Arbeitsplätzen

Jede Maschine enthält:

- `Number`
- `Name`
- `Staffing`
- optionale Belegung für `Night`, `Early`, `Late`

## Datenmodell in der Komponente

Die Komponente arbeitet aktuell mit einfachen internen Records:

- `ShiftHeader`
- `ShiftPlanSlot`
- `ShiftPlanMachine`
- `ShiftPlanSection`

Definiert in:

- [ShiftPlanBoard.razor](/Components/Shared/ShiftPlanBoard.razor:433)

### ShiftPlanSlot

Ein Slot beschreibt eine belegte Schichtzelle.

Felder:

- `Material`
- `People`
- `Note`

Die Karte ist bereits dafür vorbereitet, mehrere Namen sauber darzustellen.

Aktueller Stand:

- 1 bis 4 Namen sind gestalterisch vorgesehen
- zusätzlicher Hinweistext kann unter den Namen dargestellt werden

## Kartenaufbau pro Schicht

Die Schichtkarten bestehen aktuell aus:

1. Kartenkopf mit Material
2. kleinem Zähler für die Personenzahl
3. Namensraster für Mitarbeiterinnen und Mitarbeiter
4. optionalem Zusatztext im unteren Bereich

Siehe:

- Markup: [ShiftPlanBoard.razor](/Components/Shared/ShiftPlanBoard.razor:99)
- Styling: [ShiftPlanBoard.razor.css](/Components/Shared/ShiftPlanBoard.razor.css:542)

## Farblogik

Es gibt aktuell zwei Ebenen der Farbdarstellung:

### 1. Abteilungsfarben

Jede Abteilung hat eine eigene Farbkennung.

Umgesetzt über:

- Bereichszeile (`theme-*`)
- linke Farbkante in der Maschinenzelle
- farbige Kennzeichnung von Maschinen-Nummer und Staffing-Badge

Siehe:

- Bereichsfarben: [ShiftPlanBoard.razor.css](/Components/Shared/ShiftPlanBoard.razor.css:320)
- Maschinenfarben: [ShiftPlanBoard.razor.css](/Components/Shared/ShiftPlanBoard.razor.css:415)

### 2. Schichtfarben

Zusätzlich werden die drei Schichten leicht unterschieden:

- `Nachtschicht`
- `Frühschicht`
- `Spätschicht`

Die Karten selbst verwenden dafür eine obere Akzentlinie und angepasste Flächen.

## Autoscaling

Der Schichtplan nutzt eine JS-Hilfe in `App.razor`, damit die Plan-Tafel automatisch eingepasst wird.

Siehe:

- [App.razor](/Components/App.razor:152)

### Aktuelles Verhalten

In der Vollbildansicht werden Breite und Höhe getrennt berechnet:

- `scaleX`
- `scaleY`

Die Transform-Logik setzt:

- `translateX(-50%) scale(scaleX, scaleY)`

Dadurch wird der Plan nicht nur vertikal, sondern auch horizontal an die verfügbare Fläche angepasst.

Siehe:

- [App.razor](/Components/App.razor:194)

### Übersicht im Dashboard

Die eingebettete Ansicht verwendet zusätzlich flexible Grid-Spalten, damit die Übersicht die verfügbare Breite besser nutzt.

Siehe:

- [ShiftPlanBoard.razor.css](/Components/Shared/ShiftPlanBoard.razor.css:147)
- [ShiftPlanBoard.razor.css](/Components/Shared/ShiftPlanBoard.razor.css:237)

## Wichtiger UI-State

Wichtige Variablen in der Komponente:

- `isFullscreen`
- `activateMonitorView`
- `calendarWeekLabel`
- `boardDateLabel`
- `lastUpdatedLabel`
- `sections`
- `shiftHeaders`

Siehe:

- [ShiftPlanBoard.razor](/Components/Shared/ShiftPlanBoard.razor:119)

## Vollbild-Ablauf

Der Ablauf für die Monitoransicht ist aktuell:

1. Klick auf `Monitoransicht`
2. `isFullscreen = true`
3. `OnAfterRenderAsync()` ruft die JS-Hilfe auf
4. JS misst die Tafelgröße
5. JS skaliert die Bühne passend auf Bildschirmbreite und -höhe
6. Beim Schließen wird die Session zurückgesetzt

Siehe:

- [ShiftPlanBoard.razor](/Components/Shared/ShiftPlanBoard.razor:143)
- [App.razor](/Components/App.razor:245)

## Aktuelle Einschränkungen

Aus dem sichtbaren Code ergeben sich aktuell diese technischen Einschränkungen:

- alle Daten sind fest im Quellcode hinterlegt
- `Zuletzt aktualisiert` zeigt aktuell die Renderzeit der Komponente, nicht den echten letzten Bearbeitungszeitpunkt aus einer Datenquelle
- es gibt noch keine Eingabemaske für Schichtplandaten
- die Maschinenlisten sind noch nicht an eine zentrale Konfiguration oder Tabelle angebunden
- Änderungen am Schichtplan erfordern aktuell Code-Anpassungen

## Vorschlag für die nächste Ausbaustufe

Für die spätere Datenbankanbindung ist folgende Richtung sinnvoll:

1. Tabelle für Schichtplan-Kopf mit Datum, KW, Status, Änderungszeitpunkt
2. Tabelle für Bereiche und Maschinen
3. Tabelle für Schichtbelegungen je Maschine und Schicht
4. separate Tabelle für zugeordnete Personen je Schichtkarte
5. optionale Notiz oder Zusatztext je Karte

Dann kann `BuildSections()` durch einen Service ersetzt werden.

## Relevante technische Stellen

- Dashboard-Einbindung: [Home.razor](/Components/Pages/Home.razor:1)
- UI-Komponente: [ShiftPlanBoard.razor](/Components/Shared/ShiftPlanBoard.razor:1)
- Styling: [ShiftPlanBoard.razor.css](/Components/Shared/ShiftPlanBoard.razor.css:1)
- JS-Autoscaling: [App.razor](/Components/App.razor:1)
