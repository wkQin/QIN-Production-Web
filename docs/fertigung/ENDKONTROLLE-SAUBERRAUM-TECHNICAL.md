# Endkontrolle Sauberraum Technical

## Zweck

Diese Dokumentation beschreibt, wie die Erfassung der Fehlersammelkarte im Bereich Endkontrolle / Sauberraum technisch aufgebaut ist.

Relevante Dateien:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)
- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:1)
- [FehleranalyseService.cs](/Data/FehleranalyseService.cs:1)

## Route und Seite

Die Seite ist unter folgender Route erreichbar:

- `/fertigung/endkontrolle`

Definiert in:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:1)

## Datenmodell

Das zentrale Modell ist `EndkontrolleEintrag`.

Definiert in:

- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:14)

Enthaltene Felder:

- `ID`
- `Charge`
- `FANr`
- `Kunde`
- `Projekt`
- `Artikel`
- `Dekor`
- `Datum`
- `Gutteile`
- `Fusseln`
- `Nadelstiche`
- `Pickel`
- `Dekorfehler`
- `Farbfehler`
- `Flecken`
- `Nebel`
- `Vertiefung`
- `Oelflecken`
- `Tiefziehfehler`
- `Fraesfehler`
- `Knicke`
- `Kratzer`
- `Bemerkung`
- `Personalnummer`

Standardwerte:

- `Datum = DateTime.Today`
- `Personalnummer = "100"` als Fallback

## UI-State in der Razor-Seite

Wichtige State-Variablen:

- `Kunden`
- `Projekte`
- `ArtikelListe`
- `DekorListe`
- `RecentEintraege`
- `UserPersonalnummer`
- `ActualUserName`
- `NeuerEintrag`
- `editingId`
- `editingField`

Definiert in:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:306)

## Initialisierung

Beim Laden der Seite passiert in `OnInitializedAsync()`:

1. Auth-State wird geladen.
2. Benutzername wird aus `user.Identity.Name` gelesen.
3. Die Personalnummer wird aus dem Claim `UserId` gelesen.
4. Falls der Claim fehlt, wird `UserService.Personalnummer` als Fallback verwendet.
5. `NeuerEintrag.Personalnummer` wird gesetzt.
6. Kundenliste wird geladen.
7. Die letzten Eintraege werden geladen.

Siehe:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:316)

## Laden der Auswahldaten

### Kunden

Die Kundenliste kommt aus `dbo.Kunden`.

Query:

- `SELECT Kunde, MAX(CAST(IstAktiv AS INT)) ... GROUP BY Kunde`

Ziel:

- Anzeige des Kundennamens
- Kennzeichnung, ob ein Kunde aktiv oder inaktiv ist

Siehe:

- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:47)

### Projekte

Nach Kundenauswahl werden die Projekte des Kunden geladen.

Siehe:

- UI-Handler: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:333)
- Service: [EndkontrolleService.cs](/Data/EndkontrolleService.cs:69)

### Artikel und Dekore

Nach Projektauswahl werden Artikel und Dekore geladen.

Technik:

- Quelle ist ebenfalls `dbo.Kunden`
- `Artikel` und `Dekor` werden aus den Datensaetzen gelesen
- mehrwertige Inhalte werden per `Split(", ")` getrennt
- Rueckgabe erfolgt als deduplizierte Listen ueber `HashSet<string>`

Siehe:

- UI-Handler: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:345)
- Service: [EndkontrolleService.cs](/Data/EndkontrolleService.cs:88)

## Speichern eines Eintrags

Das Speichern wird in `SaveEintrag()` ausgelost.

Siehe:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:363)

### Validierung in der UI

Vor dem Insert werden diese Felder auf `IsNullOrWhiteSpace` geprueft:

- `Charge`
- `FANr`
- `Artikel`
- `Dekor`

Wenn ein Pflichtfeld fehlt:

- JavaScript-`alert()` wird angezeigt
- der Insert wird abgebrochen

### DB-Insert

Der Insert passiert in:

- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:114)

Zieltabelle:

- `dbo.Table1`

Verwendete DB-Spalten:

- `Kunde`
- `Projekt`
- `Artikel`
- `Dekor`
- `Charge`
- `FSKdate`
- `Gutteile`
- `Fusseln`
- `Nadelstiche`
- `Pickel`
- `Dekorfehler`
- `Color`
- `Flecken`
- `Nebel`
- `Vertiefung`
- `Oelflecken`
- `Tiefziehfehler`
- `Fraesfehler`
- `Knicke`
- `Kratzer`
- `Personalnummer`
- `[FA-Nr]`
- `Bemerkungen`

Mapping Modell -> DB:

- `FANr` -> `[FA-Nr]`
- `Datum` -> `FSKdate`
- `Farbfehler` -> `Color`
- `Bemerkung` -> `Bemerkungen`

Nach erfolgreichem Insert:

- es wird ein Activity-Log mit `[Sauberraum]` geschrieben
- die UI setzt Charge, FA-Nummer, Bemerkung und alle Fehlerzaehler zurueck
- Auswahldaten wie Kunde, Projekt, Artikel und Dekor bleiben erhalten

## Laden der letzten Eintraege

Die letzten Eintraege werden personalnummerbezogen geladen.

Siehe:

- UI: [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:393)
- Service: [EndkontrolleService.cs](/Data/EndkontrolleService.cs:158)

Query-Verhalten:

- `SELECT TOP 10 ... FROM dbo.Table1 WHERE Personalnummer = @Personalnummer ORDER BY ID DESC`

Das bedeutet:

- jede Person sieht standardmaessig nur die eigenen letzten 10 Eintraege

## Inline-Bearbeitung

Die Tabelle unten verwendet ein einfaches Inline-Editing mit zwei State-Feldern:

- `editingId`
- `editingField`

Siehe:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:408)

Ablauf:

1. Klick auf eine Zelle ruft `StartEdit(id, field)` auf.
2. Das Feld wechselt von Text zu Input.
3. `onchange` ruft `SaveField(...)` auf.
4. `SaveField(...)` ruft `UpdateEintragFieldAsync(...)`.
5. Danach wird die Tabelle neu geladen.

Update-Service:

- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:212)

Delete-Service:

- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:237)

## Analyse und Weiterverwendung der Daten

Die in `dbo.Table1` gespeicherten Endkontrolle-Daten werden spaeter fuer Auswertungen wiederverwendet.

Technische Stelle:

- [FehleranalyseService.cs](/Data/FehleranalyseService.cs:129)

Dort werden geladen:

- Fehlerwerte
- Gutteile
- Charge
- Kunde
- Projekt
- Artikel
- Dekor
- Personalnummer
- Benutzername aus `LoginDaten`

Berechnete Kennzahlen:

- `SchlechtIntern`
- `SchlechtExtern`
- `Schlechtteile`
- `Gesamt`

Definiert in:

- [FehleranalyseService.cs](/Data/FehleranalyseService.cs:13)

## Wichtige Besonderheiten

### Historische DB-Namen

Einige DB-Feldnamen sind historisch und nicht 1:1 identisch mit den UI-Begriffen:

- `Color` statt `Farbfehler`
- `Bemerkungen` statt `Bemerkung`
- `[FA-Nr]` statt `FANr`
- `FSKdate` statt `Datum`

### Vertauschte Ueberschriften in der UI

In der Eingabemaske steht links `Externe Fehler` und rechts `Interne Fehler`.
Inhaltlich wirkt die Zuordnung im Code jedoch umgekehrt:

- links stehen eher interne optische Fehler wie `Fusseln`, `Pickel`, `Dekorfehler`
- rechts stehen eher externe bzw. prozessbezogene Fehler wie `Oelflecken`, `Tiefziehfehler`, `Fraesfehler`

Siehe:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:97)
- [FehleranalyseService.cs](/Data/FehleranalyseService.cs:32)

### Feldname bei Farbfehler

Beim Inline-Edit wird fuer Farbfehler der Feldname `Color` direkt verwendet.
Das ist korrekt fuer die DB, aber in der UI weniger selbsterklaerend.

Siehe:

- [Endkontrolle.razor](/Components/Pages/Fertigung/Endkontrolle.razor:258)

### Update per Feldname

`UpdateEintragFieldAsync()` baut das SQL mit dem Feldnamen direkt zusammen:

```csharp
string q = $"UPDATE dbo.Table1 SET [{field}] = @value WHERE ID = @ID";
```

Aktuell funktioniert das, weil die UI feste Feldnamen uebergibt.
Technisch bleibt das aber nur sicher, solange keine freien oder unkontrollierten Feldnamen uebergeben werden.

Siehe:

- [EndkontrolleService.cs](/Data/EndkontrolleService.cs:220)

## Empfehlenswerte Erweiterungen fuer spaeter

- Feldnamen zentral als Mapping definieren statt als freie Strings
- Interne und externe Fehler in der UI eindeutig und fachlich korrekt beschriften
- Pflichtfelder serverseitig zusaetzlich validieren
- Erfolgsmeldungen im UI moderner statt ueber `alert()` anzeigen
- Optional Summen fuer Schlecht intern, Schlecht extern und Gesamt direkt in der Eingabeseite anzeigen
