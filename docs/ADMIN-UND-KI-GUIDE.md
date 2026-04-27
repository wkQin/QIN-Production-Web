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
- Beispiel: `Update (3.2.0) Schichtplan`

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
5. Titel im Format `Update (Version) Bereich` setzen.
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
