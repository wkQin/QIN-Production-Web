# QIN Production Web: Admin- und KI-Guide

Diese Datei ist die zentrale Kurz-Doku fuer wiederkehrende Admin-Aufgaben und KI-Anweisungen.
Sie kann nach und nach pro Tab erweitert werden.

## DB-Informationen

Quelle: `Data/SqlManager.cs`

- Server: `QINSQL064`
- Hauptdatenbank: `qinFSK\table1`
- Fertigungsdatenbank: `Fertigung`
- Benutzer: `db.user`
- Passwort: `232323`

Wichtige Klarstellung:

- Es gibt zwei Datenbanken: `qinFSK\table1` und `Fertigung`.
- `dbo` ist jeweils nur das Schema innerhalb der Datenbank.
- `dbo.Table1` ist keine eigene Datenbank.
- Benutzerdaten liegen aktuell in `qinFSK\table1`, zum Beispiel in `dbo.LoginDaten`.
- Fertigungsbezogene Tabellen wie der Schichtplan sollen in `Fertigung` unter dem Schema `dbo` angelegt werden.

## Benachrichtigungen schreiben

Benachrichtigungen immer per `INSERT` in die Tabelle `Alerts` schreiben.

Bedeutung fuer KI:

- Wenn der Nutzer sagt `Benachrichtigung schreiben`, `Benachrichtigung anlegen` oder `Update schreiben`, ist ein echter DB-Eintrag in `Alerts` gemeint.
- In diesem Fall nicht nur einen Textvorschlag liefern.
- In diesem Fall nicht nur den Guide oder ein Markdown-Beispiel anpassen.
- Die KI soll den Text formulieren, den `INSERT` ausfuehren und den neuen Datensatz kurz pruefen.

Pflichtregeln:

- Titel immer im Format `Update (Version) Bereich` schreiben.
- Beispiel fuer den Titel: `Update (3.2.0) Schichtplan`.
- Aenderungen kurz, klar und fuer Nutzer verstaendlich formulieren.
- Pro Aenderung nur einen kurzen Satz schreiben.
- Deutsche Umlaute und `ß` normal schreiben, also `ä`, `ö`, `ü` und `ß` benutzen.
- Wenn eine Schrift diese Zeichen nicht sauber darstellt, eine andere Schrift waehlen.
- In der Nachricht echte Zeilenumbrueche verwenden.
- Wenn die Meldung im UI geschrieben wird: `Shift + Enter` fuer neue Zeilen nutzen.
- Im Guide keine fertige neue Benachrichtigung hinterlegen.

Verwendete Spalten:

- `Title`
- `Message`
- `CreatedAt`
- `CreatedBy`
- `TargetGroup`

Vorlage fuer ein Insert:

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

Wichtig fuer KI und manuelle Eingaben:

- Immer ein `INSERT INTO Alerts (...) VALUES (...)` verwenden.
- In der Nachricht immer echte Zeilenumbrueche verwenden.
- Im UI fuer neue Zeilen immer `Shift + Enter` verwenden.
- Texte kurz, klar und leicht verstaendlich formulieren.
- Titel immer im Format `Update (Version) Bereich` schreiben.
- Deutsche Buchstaben wie `ä`, `ö`, `ü` und `ß` bewusst verwenden.
- Wenn die aktuelle Schrift diese Zeichen nicht sauber anzeigt, eine andere Schrift verwenden.
- Keine aktuelle Release-Meldung direkt im Guide ausformulieren.
- Nach dem Insert den neuen Datensatz kurz pruefen.

Empfohlene Schreibweise:

- Pro Zeile ein kurzer Satz.
- Keine langen Einleitungen.
- Aenderungen zuerst, Umbenennungen zuletzt.
- Keine internen Fachsaetze oder unnoetig technische Formulierungen.

Beispiel:

```text
Zeiterfassung springt bei Buchungen jetzt direkt zum Buchungsdatum.
Wareneingang hat jetzt eine Dickenmessung.
Kunde IAC wurde durch Artifex ersetzt.
```

## KI-Kurzanweisung

Wenn eine KI eine neue Benachrichtigung anlegen soll, dann standardmaessig:

1. `Data/SqlManager.cs` fuer die DB-Daten pruefen.
2. Titel immer als `Update (Version) Bereich` formulieren.
3. Nachricht kurz, verstaendlich, mit echten Zeilenumbruechen und mit echten deutschen Buchstaben schreiben.
4. Wenn die Schrift `ä`, `ö`, `ü` oder `ß` nicht sauber darstellt, eine andere Schrift waehlen.
5. Erst den Text neu formulieren und nicht aus dem Guide als fertige Meldung kopieren.
6. Den `INSERT` wirklich ausfuehren.
7. Den neuesten Datensatz direkt danach kurz pruefen.
8. Dem Nutzer kurz bestaetigen, was eingefuegt wurde.

Kurzprompt fuer spaeter:

```text
Lege eine neue Benachrichtigung per INSERT in Alerts an. Wenn ich Benachrichtigung schreiben sage, ist immer ein echter DB-Insert in Alerts gemeint. Nutze fuer den Titel das Format Update (Version) Bereich, schreibe pro Zeile einen kurzen Satz, benutze echte deutsche Buchstaben wie ä, ö, ü und ß, fuehre den Insert aus und pruefe danach kurz den neuesten Datensatz.
```

## Tab-Dokumentation

Diese Struktur kann fuer weitere Bereiche wiederverwendet werden.

### Einstellungen

Funktionen:

- Persoenliche Anzeigeeinstellungen speichern
- System-Ankuendigungen an alle Nutzer senden

Kurztutorial:

1. Titel eintragen.
2. Nachricht mit kurzen Saetzen schreiben.
3. Fuer neue Zeilen `Shift + Enter` nutzen.
4. Absenden.

### Vorlage fuer weitere Tabs

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
