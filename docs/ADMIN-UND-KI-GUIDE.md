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

## Benachrichtigungen schreiben

Tabelle: `Alerts`

Verwendete Spalten:

- `Title`
- `Message`
- `CreatedAt`
- `CreatedBy`
- `TargetGroup`

Standard-Insert:

```sql
INSERT INTO Alerts (Title, Message, CreatedAt, CreatedBy, TargetGroup)
VALUES (
    N'Update 3.1.2',
    N'- Erste Zeile
- Zweite Zeile
- Dritte Zeile',
    SYSDATETIME(),
    N'System',
    NULL
);
```

Wichtig fuer KI und manuelle Eingaben:

- In der Nachricht immer echte Zeilenumbrueche verwenden.
- Wenn die Meldung im UI geschrieben wird: `Shift + Enter` fuer neue Zeilen nutzen.
- Texte kurz, klar und release-orientiert formulieren.
- Titel am besten im Format `Update x.y.z`.

Empfohlene Schreibweise:

- Pro Punkt eine kurze Zeile.
- Keine langen Einleitungen.
- Aenderungen zuerst, Umbenennungen zuletzt.

Beispiel:

```text
- Zeiterfassung springt bei Buchungen jetzt direkt zum Buchungsdatum.
- Wareneingang: Erfassung der Dichtenmessung hinzugefuegt.
- Kunde IAC wurde durch Artifex ersetzt.
```

## KI-Kurzanweisung

Wenn eine KI eine neue Benachrichtigung anlegen soll, dann standardmaessig:

1. `Data/SqlManager.cs` fuer die DB-Daten pruefen.
2. Nachricht kurz und sauber formulieren.
3. In `Alerts` einfuegen.
4. Fuer mehrere Punkte echte Zeilenumbrueche benutzen.
5. Nach dem Insert den neuesten Datensatz kurz pruefen.

Kurzprompt fuer spaeter:

```text
Lege eine neue Benachrichtigung in Alerts an. Nutze die DB-Daten aus Data/SqlManager.cs, formuliere den Text kurz und sauber und verwende fuer neue Zeilen Shift+Enter bzw. echte Zeilenumbrueche im Message-Feld.
```

## Tab-Dokumentation

Diese Struktur kann fuer weitere Bereiche wiederverwendet werden.

### Einstellungen

Funktionen:

- Persoenliche Anzeigeeinstellungen speichern
- System-Ankuendigungen an alle Nutzer senden

Kurztutorial:

1. Titel eintragen.
2. Nachricht mit kurzen Punkten schreiben.
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
