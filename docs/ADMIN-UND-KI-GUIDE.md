# QIN Production Web: Admin- und KI-Guide

Kurze Arbeitsregeln für Admin-Aufgaben, Dokumentation und KI-Unterstützung im Projekt.

## Grundregeln

- Immer echte deutsche Buchstaben verwenden: `ä`, `ö`, `ü` und `ß`, nicht `ae`, `oe`, `ue` oder `ss`.
- Änderungen kurz, verständlich und nutzerbezogen beschreiben.
- Keine unnötig technischen Formulierungen in Nutzertexten.
- Keine fertige aktuelle Release-Meldung direkt in diesem Guide hinterlegen.

## Datenbank

Quelle für Verbindungsdaten: `Data/SqlManager.cs`

- Server: `QINSQL064`
- Hauptdatenbank: `qinFSK\table1`
- Fertigungsdatenbank: `Fertigung`
- Benutzer: `db.user`
- Passwort: `232323`

Wichtig:

- `qinFSK\table1` und `Fertigung` sind zwei getrennte Datenbanken.
- `dbo` ist jeweils nur das Schema innerhalb einer Datenbank.
- `dbo.Table1` ist keine eigene Datenbank.
- Benutzerdaten liegen aktuell in `qinFSK\table1`, zum Beispiel in `dbo.LoginDaten`.
- Fertigungsbezogene Tabellen wie der Schichtplan gehören in `Fertigung` unter `dbo`.

## Update-Log

Bei jeder Projektänderung muss `docs/UPDATE-LOG.md` aktualisiert werden.

Regeln:

- Die Änderung der passenden Update-Version zuordnen.
- Wenn keine passende Version vorhanden ist, eine neue Update-Überschrift anlegen.
- Kurz schreiben, was geändert wurde.
- Nutzerrelevante Änderungen verständlich formulieren.
- Technische Details nur aufnehmen, wenn sie später für Wartung oder Fehlersuche wichtig sind.
- Das reine Erstellen oder Senden einer Benachrichtigung ist keine Projektänderung und kommt nicht als eigener Update-Log-Eintrag hinein.

## Dokumentationen & Docu

Dokumentationen sollen sauber strukturiert, gut nach Word kopierbar, als PDF speicherbar und ausdruckbar sein.

Wenn der Nutzer eine Dokumentation für einen Bereich möchte, immer prüfen, welche Dokumente sinnvoll sind.

Standard-Aufteilung:

- `<BEREICH>-DOKUMENTATION.md` für die normale fachliche Dokumentation.
- `<BEREICH>-BACKEND.md` für technische Abläufe, Datenbank, Services und Code-Hinweise.
- `<BEREICH>-ARBEITSANWEISUNG.md` für klare Schritt-für-Schritt-Anweisungen für Mitarbeitende.
- Wenn druckbare Dateien gewünscht sind, zusätzlich passende `.html`-Dateien erstellen.

Ordner und Benennung:

- Dokumente thematisch in eigene Ordner legen, zum Beispiel `docs/fertigung/wareneingang/`.
- Dateinamen klar und einheitlich schreiben, zum Beispiel `WARENEINGANG-DOKUMENTATION.md`.
- Technische Dokumente `BACKEND` nennen, nicht `Technical`.
- Arbeitsanweisungen wirklich als Arbeitsanweisung schreiben, nicht als technische Doku.
- Alte Vorlagen, doppelte Inhalte und falsche Dateinamen bereinigen, wenn sie durch die neue Struktur ersetzt werden.

Inhalt:

- Allgemeine Dokumentationen erklären Zweck, Zielgruppe, Funktionen, Ablauf, Verantwortlichkeiten und Ergebnis.
- Backend-Dokumentationen erklären relevante Dateien, Tabellen, Services, Validierung, Speichern, Mails, Logs und Prüfpunkte.
- Arbeitsanweisungen sind einfach, direkt und für den Alltag geschrieben.
- Wichtige Sonderfälle aufnehmen, zum Beispiel automatische QS-Mails oder Pflichtfelder.
- Keine unnötig technischen Formulierungen in Arbeitsanweisungen.

Layout:

- Überschriften, Absätze und Listen sauber trennen.
- Keine harten Zeilen- oder Seitenumbrüche mitten im normalen Fließtext setzen.
- `Shift + Enter` nur bewusst für kurze zusammengehörige Zeilen nutzen, zum Beispiel in Benachrichtigungen oder kompakten Hinweisen.
- Bei Druck- oder HTML-Dokumenten darauf achten, dass Überschriften nicht allein am Seitenende stehen.
- Listenpunkte und kurze Absätze möglichst nicht mitten über zwei Seiten trennen.
- Absätze lieber sauber neu beginnen, statt mitten im Satz umzubrechen.
- Lange Listen in sinnvolle Abschnitte teilen, damit sie in Word und PDF gut aussehen.

HTML und PDF:

- Das QIN-FORM-Logo in der normalen Dokument-Kopfzeile oben rechts platzieren.
- Links in der Kopfzeile den Dokumentnamen oder `QIN Production Web` anzeigen.
- Version und Datum schlank in die Fußzeile setzen.
- Keine instabilen `position: fixed` Druck-Kopfzeilen verwenden, wenn der Browserdruck dadurch Logo oder Text verschiebt.
- Für Chrome oder Edge beim Speichern als PDF die Browser-Option `Kopf- und Fußzeilen` ausschalten, damit kein Dateipfad und keine Browser-Seitenzahl eingefügt werden.
- Druck-CSS so setzen, dass Überschriften, Listenpunkte und kurze Absätze möglichst nicht unschön getrennt werden.
- Vor dem Fertigmelden kurz prüfen, ob Kopfzeile, Fußzeile, Logo und Seitenumbrüche ordentlich aussehen.

## Benachrichtigungen

Wenn der Nutzer sagt `Benachrichtigung schreiben`, `Benachrichtigung anlegen` oder `Update schreiben`, ist ein echter DB-Eintrag in `Alerts` gemeint.

Immer:

- Vorher `docs/UPDATE-LOG.md` lesen.
- Falls die aktuelle Projektänderung dort noch fehlt, zuerst den Update-Log ergänzen.
- Aus dem Update-Log eine kurze, nutzerverständliche Meldung formulieren.
- Das Senden der Benachrichtigung selbst nicht in den Update-Log schreiben.
- Den `INSERT` wirklich ausführen.
- Danach den neuesten Datensatz kurz prüfen.

Titel:

- Format: `Update (Version) Bereich`
- Als Bereich immer den fachlich passenden Bereich nutzen, nicht allgemein `QIN Production Web`.
- Bei mehreren Bereichen die wichtigsten Bereiche nennen.
- Beispiele: `Update (3.2.2) Fehleranalyse`, `Update (3.2.2) Zeiterfassung`, `Update (3.2.2) Verwaltung und Dokumentation`

Nachricht:

- Pro Zeile ein kurzer Satz.
- Echte Zeilenumbrüche verwenden.
- Änderungen zuerst, Umbenennungen zuletzt.
- Keine langen Einleitungen.
- Im UI für neue Zeilen `Shift + Enter` nutzen.

Verwendete Spalten:

- `Title`
- `Message`
- `CreatedAt`
- `CreatedBy`
- `TargetGroup`

Vorlage:

```sql
INSERT INTO Alerts (Title, Message, CreatedAt, CreatedBy, TargetGroup)
VALUES (
    N'Update (<Version>) <Bereich>',
    N'<Kurzer Satz 1>
<Kurzer Satz 2>
<Kurzer Satz 3>',
    SYSDATETIME(),
    N'System',
    NULL
);
```

Beispieltext:

```text
Zeiterfassung springt bei Buchungen jetzt direkt zum Buchungsdatum.
Wareneingang hat jetzt eine Dickenmessung.
Kunde IAC wurde durch Artifex ersetzt.
```

## KI-Checkliste

Bei einer Benachrichtigung:

1. `Data/SqlManager.cs` für DB-Daten prüfen.
2. `docs/UPDATE-LOG.md` lesen.
3. Fehlende Projektänderungen zuerst im Update-Log ergänzen.
4. Benachrichtigung formulieren, aber das Senden selbst nicht in den Update-Log schreiben.
5. Titel im Format `Update (Version) Bereich` setzen und einen echten Bereich wählen, nicht `QIN Production Web`.
6. Nachricht kurz, klar und mit echten deutschen Buchstaben schreiben.
7. `INSERT INTO Alerts (...) VALUES (...)` ausführen.
8. Neuesten Datensatz prüfen.
9. Dem Nutzer kurz bestätigen, was eingefügt wurde.

Kurzprompt:

```text
Lege eine neue Benachrichtigung per INSERT in Alerts an. Lies vorher docs/UPDATE-LOG.md. Ergänze fehlende Projektänderungen zuerst im Update-Log, aber schreibe das Senden der Benachrichtigung selbst nicht hinein. Nutze den Titel Update (Version) Bereich, schreibe kurze Sätze mit echten Zeilenumbrüchen und verwende echte deutsche Buchstaben wie ä, ö, ü und ß. Führe den Insert aus und prüfe danach den neuesten Datensatz.
```

## Tab-Dokumentation

Diese Struktur kann für weitere Bereiche wiederverwendet werden:

```text
## <Tab-Name>

Funktionen:
- ...

Kurztutorial:
1. ...
2. ...

KI-Hinweise:
- ...
```
