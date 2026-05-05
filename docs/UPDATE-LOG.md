# Update-Log

Diese Datei sammelt kurz, was zuletzt geändert wurde und zu welchem Update es gehört.
Vor neuen Update-Benachrichtigungen soll diese Datei gelesen werden.

## Update 3.2.4
- Dokumentation: Für das Sperrlager wurden eine fachliche Dokumentation und eine Backend-Dokumentation ergänzt.
- Produktionslayout: Im Sperrlager-Detailpopup wurde `Chargendatum` entfernt.
- Sperrlager: Beim Vermüllen bleibt eine Charge gesperrt, wird als vermüllt markiert und verschwindet nur aus der offenen gesperrten Chargenliste.
- Produktionslayout: Sperrlager-Aktionen melden jetzt konkret, welche eingegebenen Chargen nicht in `dbo.Chargen` gefunden wurden.
- Produktionslayout: Im Sperrlager-Detailpopup wurde das Feld `Status` entfernt, weil Sperrgrund und Sperrlager-Aktion die relevanten Informationen liefern.
- Produktionslayout: Im Sperrlager wird die Charge-Eingabe jetzt sofort beim Tippen übernommen, damit der Sperren-Button direkt aktiv wird.
- Produktionslayout: Der Sperren-Button zeigt jetzt auch ohne ausgewählten Platz eine klare Meldung, statt verborgen deaktiviert zu bleiben.
- Produktionslayout: Im Sperrlager löscht ein Klick auf einen leeren Platz nicht mehr die manuell eingegebene Charge; belegte Plätze übernehmen weiterhin alle Chargen für Bulk-Aktionen.
- Produktionslayout: Sperrlagerplätze werden wieder als `Platz 1`, `Platz 2` und so weiter angezeigt.
- Produktionslayout: Das Sperrlager-Detailpopup zeigt keine doppelte Wareneingangs-Bemerkung mehr, wenn bereits ein Sperrgrund vorhanden ist.
- Sperrlager: Die neue Tabelle `Fertigung.dbo.Sperrlager` protokolliert Sperren, Entsperren und Vermüllen mit Charge, Benutzer, Bereich, Grund, Zeit und Lagerort.
- Datenbank: `Fertigung.dbo.Sperrlager` ist jetzt per Fremdschlüssel über `Chargen_ID` mit `dbo.Chargen` verbunden.
- Produktionslayout: Sperrlager-Chargen können jetzt einzeln in das Eingabefeld kopiert werden; ein Platzklick übernimmt alle Chargen des Platzes für Bulk-Aktionen.
- Produktionslayout: Die offene gesperrte Chargenliste öffnet beim Klick jetzt ebenfalls die Chargendetails.
- Produktionslayout: Die Sperrlager-Aktionsbuttons führen jetzt Datenbankaktionen für Sperren, Entsperren und Vermüllen aus.
- Produktionslayout: Das Sperrlager nutzt jetzt mehr Modalhöhe, hat keinen unteren Schließen-Button mehr und zeigt größere Regalplätze für mehrere sichtbare Chargen.
- Produktionslayout: Die Sperrlager-Ansicht wurde überarbeitet; Plätze zeigen nur noch klickbare Chargen mit Einlagerdatum, Chargendetails öffnen als Popup und offene gesperrte Chargen liegen jetzt in einem Sideview.
- Produktionslayout: Die Sperrlager-Plätze zeigen jetzt kurze Bezeichnungen wie `1S`, `2S` und `12S`.
- Produktionslayout: Die Sperrlager-Ansicht zeigt jetzt gesperrte Chargen, die noch nicht in einem Lagerort eingelagert sind, als Warteliste an.
- Produktionslayout: Der Sperrlager-Button öffnet jetzt eine eigene Sperrlager-Regalansicht mit 2 Regalen, 12 Plätzen und einem Aktionsbereich.
- Datenbank: Für das Sperrlager wurden in `dbo.Lagerorte` die 12 Plätze `H2R17P1S` bis `H2R17P12S` angelegt.
- Produktionslayout: Der Sperrlager-Button wurde auf die finale Position gesetzt und der Einstell-Overlay wurde wieder ausgeblendet.
- Produktionslayout: Für das kommende Sperrlager wurde ein roter Sperrlager-Button auf der Produktionskarte ergänzt.
- Dokumentation: Die Wareneingang-Dokumentation, Backend-Doku und Arbeitsanweisung wurden auf Mustermaterial, Dickenmessung-Toleranz, Chargensperre, Bemerkungen und Druckproblem-Hinweise aktualisiert.
- Wareneingang: Bei automatischer Chargensperre bleibt eine vorhandene Werker-Bemerkung erhalten und die Sperr-Bemerkung wird ergänzt.
- Wareneingang: Bei automatischer Chargensperre wird die Sperr-Bemerkung jetzt auch im Wareneingang gespeichert.
- Datenbank: `dbo.Wareneingang` hat jetzt auch in `qinFSK\table1` die Spalte `Bemerkung`, passend zur Fertigungsdatenbank.
- Wareneingang: Die Bemerkung wird jetzt in den Wareneingangstabellen in Fertigung und Verwaltung angezeigt, durchsucht und in der Verwaltung sortiert.
- Wareneingang: Der Bereich `Letzte Buchungen` heißt jetzt `Offene Wareneingänge`.
- Wareneingang: Bei falscher Dickenmessung erscheint zuerst ein großes Warnfenster; beim nächsten falschen Speicher-Versuch werden vorhandene Chargen der Bestellung gesperrt.
- Wareneingang: Die Chargensperre nutzt `dbo.Chargen.Gesperrt` und sendet bei einer Sperre direkt eine QS-Mail.
- Wareneingang: Der Dickenmessung-Hinweis zeigt den Sollwert jetzt grün und formuliert die Grenze als `Erlaubte Toleranz`.
- Wareneingang: Die Dickenmessung nutzt bei bekannten Materialien jetzt den Sollwert aus `dbo.Materialliste` und prüft mit 10 Prozent Toleranz.
- Wareneingang: Ohne Materialtreffer bleibt für neue Erfassungen der Standardbereich `0,23 mm` bis `1,2 mm` gültig.
- Wareneingang: Die Materialsuche für die Dickenmessung berücksichtigt `Suchbegriff`, `Beschreibung` und `Beschreibung2`.
- Wareneingang Verwaltung: Buchungen werden nicht mehr durch Pflichtfeldprüfungen blockiert, damit die Verwaltung offene Einträge immer buchen kann.
- Wareneingang: Palettentausch macht die Bemerkung nicht mehr zum Pflichtfeld; nur Zustand `Schlecht` erzwingt weiterhin eine Bemerkung.
- Wareneingang: Mustermaterial kann jetzt über eine Checkbox erfasst werden, zeigt ein Materialname-Feld an, blendet die Dickenmessung aus und überspringt Pflichtfeldprüfungen.

## Update 3.2.3
- Datenbank: `dbo.Materialliste` hat jetzt die neue Spalte `Dickenmessung` als Dezimalwert für die Soll-Dickenmessung pro Material.
- Dokumentation: Der Admin- und KI-Guide stellt jetzt klar, dass `Update machen` oder eine Versionsanhebung auch das Schreiben einer Benachrichtigung in `Alerts` umfasst.
- Allgemein: Die sichtbare Web-Version im Navigationsmenü wurde von `v3.2.2 Web` auf `v3.2.3 Web` aktualisiert.
- Schichtplan: In der Dashboard-Schichtplanung ist das Badge `Arbeitsplatz` im Light-Modus jetzt kontrastreicher und wieder gut lesbar.
- Schichtplan: Die Dashboard- und Monitoransicht der Produktions-Schichtplanung unterstützt jetzt ebenfalls einen abgestimmten Light-Modus mit helleren Karten, Tabellen und Bereichsfarben.
- Schichtplan: Der Verwaltungs-Schichtplan unterstützt jetzt einen abgestimmten Light-Modus mit hellen Flächen, gut lesbaren Eingaben und kontrolliert aufgehellten Bereichsfarben.
- Fehleranalyse: Der große Seiten-Header wurde entfernt und der Export kompakt in die Filterkarte verschoben, damit die Ansicht besser auf eine einzelne Bildschirmseite passt.
- Fehleranalyse: Datum-, Kunden-, Projekt-, Artikel-, Dekor- und Charge-Filter starten die Analyse jetzt automatisch, sodass der manuelle Start-Button entfällt.
- Fehleranalyse: Die Verwaltungsseite nutzt jetzt die gemeinsame Design-Vorlage mit Shared-Hero, Panels, Tabs, Tabellen und Statistik-Karten statt eigenem Insel-CSS.
- Fehleranalyse: Der Light-Modus ist jetzt vollständig in die Fehleranalyse integriert, inklusive kontrastreicherer Tabellen-/Filter-Flächen und themefähiger Apex-Charts.
- Allgemein: Theme-Wechsel benachrichtigen jetzt auch Seiten mit ApexCharts, damit Diagramme beim Umschalten zwischen Light- und Darkmode direkt neu auf das aktive Theme reagieren.
- Wareneingang: Der Button `Auswahl Drucken` ist im Light-Modus jetzt kontrastreicher und auch im deaktivierten Zustand besser lesbar.
- Wareneingang: Der Light-Modus nutzt jetzt dunklere Textfarben, besser lesbare Status-Badges und kontrastreichere Suffixe wie `mm` und `LM/STK`.
- Allgemein: Der Light-Modus hat jetzt hellere Seitenhintergründe, dunklere Navigationsschrift und verbesserte Shared-Farben für Layout, Karten und Tabellen.
- Wareneingang: Für Fertigung und Verwaltung wurde eine gemeinsame Design-Vorlage mit zentralen Layout-, Panel-, Tabellen-, Formular- und Modal-Klassen aufgebaut.
- Wareneingang: Die gemeinsamen Design-Bausteine liegen jetzt in `wwwroot/app.css`, damit spätere Light- und Darkmode-Anpassungen zentral über Variablen und Shared-Klassen laufen können.
- Dokumentation: Der Admin- und KI-Guide schreibt bei Design-Arbeiten jetzt vor, dass zuerst die gemeinsame Vorlage verwendet und nur bei echten Sonderfällen lokale Seiten-CSS ergänzt wird.
- Fehleranalyse: Im Bereich `Einzelne Einträge` werden jetzt die Bemerkungen der Fehlersammelkarten angezeigt.
- Fehleranalyse: Die Spalten im Bereich `Einzelne Einträge` können jetzt sortiert und in der Breite angepasst werden.

## Update 3.2.2
- Fehleranalyse: Die Summen für `Schlecht Extern` und `Schlecht Intern` werden jetzt in der richtigen Tabelle angezeigt.
- Fehleranalyse: Das Kalender-Icon in den Datumsfeldern ist jetzt hell und auf dunklem Hintergrund besser sichtbar.
- Zeiterfassung: Der Excel-Export zeigt manuelle Terminal-Nachträge von Nutzern ohne Verwaltungsrechte jetzt in einer eigenen Spalte `Manueller Nachtrag` an.
- Zeiterfassung: Die Export-Spalte `Manueller Nachtrag` reagiert jetzt auch direkt auf das Datenbankfeld `Manuel = 1`.
- Allgemein: Die sichtbare Web-Version im Navigationsmenü wurde von `v3.2.1 Web` auf `v3.2.2 Web` aktualisiert.
- Dokumentation: Der Update-Log wurde als zentrale Übersicht für letzte Änderungen eingeführt.
- Dokumentation: Der Admin- und KI-Guide verweist jetzt auf den Update-Log und verlangt echte deutsche Buchstaben.
- Dokumentation: Der Admin- und KI-Guide schreibt jetzt vor, dass der Update-Log bei jeder Änderung aktualisiert werden muss.
- Dokumentation: Das reine Erstellen oder Senden einer Benachrichtigung wird nicht als eigener Update-Log-Eintrag aufgenommen.
- Dokumentation: Der Admin- und KI-Guide wurde kompakter neu strukturiert und von Wiederholungen bereinigt.
- Dokumentation: Die Fertigungsdokumente wurden in Themenordner sortiert und technische Dokumente wurden von `Technical` auf `Backend` umbenannt.
- Dokumentation: Für den Wareneingang gibt es jetzt getrennte Dokumente für allgemeine Dokumentation, Backend und Arbeitsanweisung.
- Dokumentation: Die Wareneingang-Arbeitsanweisung wurde neu geschrieben und die alte Word-Vorlage entfernt.
- Dokumentation: Für die Wareneingang-Dokumente wurden zusätzlich HTML-Versionen mit Word- und PDF-freundlicher Formatierung erstellt.
- Dokumentation: Die Wareneingang-HTML-Dokumente enthalten jetzt eine schlanke PDF-Fußzeile mit Version und Datum.
- Dokumentation: Die Wareneingang-HTML-Dokumente verwenden jetzt eine stabile Dokument-Kopfzeile mit Logo-Platz und eine normale Fußzeile statt fixer Druckelemente.
- Dokumentation: Das QIN-FORM-Logo wurde als Asset abgelegt und in die Kopfzeile der Wareneingang-HTML-Dokumente eingebunden.
- Dokumentation: Der Logo-Pfad in den Wareneingang-HTML-Dokumenten wurde korrigiert und die Kopfzeile wurde kompakter gesetzt.
- Dokumentation: Die Wareneingang-Dokumentation beschreibt jetzt die QS-Mail an `qsintern@qin-form.de` und den besonderen Hinweis bei Zustand `Schlecht`.
- Dokumentation: Die Wareneingang-HTML-Dokumente haben jetzt eine wiederholte Druck-Kopfzeile mit QIN-FORM-Logo für jede PDF-Seite.
- Dokumentation: Die Druck-Kopfzeile der Wareneingang-HTML-Dokumente wurde korrigiert, damit das Logo nicht unten rechts angezeigt wird.
- Dokumentation: Die Druck-Kopfzeile der Wareneingang-HTML-Dokumente nutzt jetzt den oberen Seitenrand, damit sie den Inhalt nicht überlappt.
- Dokumentation: Die instabile wiederholte Druck-Kopfzeile der Wareneingang-HTML-Dokumente wurde deaktiviert, damit das Logo im PDF nicht unten rechts verrutscht.
- Dokumentation: Die versteckten Druck-Kopfzeilen-Elemente wurden aus den Wareneingang-HTML-Dokumenten entfernt, damit der PDF-Druck stabil bleibt.
- Dokumentation: Der Admin- und KI-Guide enthält jetzt Regeln für saubere Zeilen- und Seitenumbrüche in Dokumentationen.
- Dokumentation: Die Wareneingang-HTML-Dokumente vermeiden im Druck jetzt stärker getrennte Überschriften, Listenpunkte und kurze Absätze.
- Dokumentation: Der Admin- und KI-Guide beschreibt jetzt ausführlich die gewünschte Dokumentationsstruktur mit Dokumentation, Backend, Arbeitsanweisung, Logo, PDF-Regeln und Umbruch-Regeln.
- Dokumentation: Der Admin- und KI-Guide stellt klar, dass Benachrichtigungstitel fachliche Bereiche wie Fehleranalyse, Zeiterfassung oder Dokumentation nutzen sollen.
- Projektpflege: `.dll`-Dateien und der Ordner `publish_output/` werden jetzt von Git ignoriert.
