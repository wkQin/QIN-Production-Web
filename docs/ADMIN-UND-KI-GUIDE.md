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

Benachrichtigungen immer per `INSERT` in die Tabelle `Alerts` schreiben.

Pflichtregeln:

- Titel immer im Format `Update (Version)` schreiben.
- Beispiel fuer den Titel: `Update (3.1.4)`.
- Aenderungen kurz, klar und fuer Nutzer verstaendlich formulieren.
- Pro Aenderung nur eine kurze Zeile schreiben.
- In der Nachricht echte Zeilenumbrueche verwenden.
- Wenn die Meldung im UI geschrieben wird: `Shift + Enter` fuer neue Zeilen nutzen.

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
    N'Update (3.1.4)',
    N'- Wareneingang: Manuelle Chargen werden vor dem Hinzufuegen bestaetigt.
- Wareneingang: Pflichtfelder werden beim Bearbeiten blau markiert.
- Wareneingang: Fehlende Pflichtfelder werden mit klarer Meldung angezeigt.',
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
- Titel immer im Format `Update (Version)` schreiben.

Empfohlene Schreibweise:

- Pro Punkt eine kurze Zeile.
- Keine langen Einleitungen.
- Aenderungen zuerst, Umbenennungen zuletzt.
- Keine internen Fachsaetze oder unnoetig technische Formulierungen.

Beispiel:

```text
- Zeiterfassung springt bei Buchungen jetzt direkt zum Buchungsdatum.
- Wareneingang: Erfassung der Dichtenmessung hinzugefuegt.
- Kunde IAC wurde durch Artifex ersetzt.
```

Aktuelle Kurzvorlage fuer `3.1.4`:

```text
- Wareneingang: Manuelle Chargen werden vor dem Hinzufuegen bestaetigt.
- Wareneingang: Pflichtfelder werden beim Bearbeiten blau markiert.
- Wareneingang: Fehlende Pflichtfelder werden mit klarer Meldung angezeigt.
```

## KI-Kurzanweisung

Wenn eine KI eine neue Benachrichtigung anlegen soll, dann standardmaessig:

1. `Data/SqlManager.cs` fuer die DB-Daten pruefen.
2. Titel immer als `Update (Version)` formulieren.
3. Nachricht kurz, verstaendlich und mit echten Zeilenumbruechen schreiben.
4. Per `INSERT` in `Alerts` einfuegen.
5. Nach dem Insert den neuesten Datensatz kurz pruefen.

Kurzprompt fuer spaeter:

```text
Lege eine neue Benachrichtigung per INSERT in Alerts an. Nutze fuer den Titel immer das Format Update (Version), schreibe die Aenderungen kurz und verstaendlich und verwende im Message-Feld echte Zeilenumbrueche bzw. im UI Shift+Enter.
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
