Erste Regel: Vor jeder Aufgabe zuerst [ADMIN-UND-KI-GUIDE.md](ADMIN-UND-KI-GUIDE.md) für die Grundanweisungen prüfen.

# TODO Agent

Wenn ein Punkt erledigt ist, die Checkbox von `[ ]` auf `[x]` ändern.

## Wareneingang

Dateien:

- [Components/Pages/Fertigung/Wareneingang.razor](../Components/Pages/Fertigung/Wareneingang.razor)
- [Data/WareneingangService.cs](../Data/WareneingangService.cs)

- [x] Wenn `Palettentausch` auf `Ja` steht, darf das Bemerkungsfeld kein Pflichtfeld sein.
- [x] Wenn `Palettentausch` auf `Nein` steht, darf das Bemerkungsfeld ebenfalls kein Pflichtfeld sein.
- [x] Oben im Wareneingang eine Checkbox für `Mustermaterial` ergänzen.
- [x] Wenn `Mustermaterial` aktiviert ist, eine sichtbare Textbox für den Materialnamen anzeigen.
- [x] Wenn `Mustermaterial` aktiviert ist, das Feld `Dickenmessung` ausblenden.
- [x] Wenn `Mustermaterial` aktiviert ist, keine Pflichtfelder erzwingen.
- [x] Die Soll-Dickenmessung für Material aus der Datentabelle `dbo.Materialliste` laden.
- [x] Den Sollwert der Dickenmessung im UI anzeigen, zum Beispiel `0,5`.
- [x] Für die Dickenmessung eine Toleranz von 10 Prozent erlauben.
- [x] Beispiel für die Toleranz prüfen: Bei Sollwert `0,5` müssen Werte von `0,45` bis `0,55` akzeptiert werden.
- [x] Wenn die Dickenmessung beim Speichern nicht passt, zuerst eine große Fehlermeldung anzeigen.
- [x] Die Fehlermeldung soll klar sagen, dass die Dickenmessung außerhalb der erlaubten Werte liegt.
- [x] Die Fehlermeldung soll klar warnen, dass die Charge beim nächsten falschen Versuch gesperrt wird.
- [x] Beim zweiten falschen Speicher-Versuch alle Chargen der Bestellung sperren.
- [x] Die Sperre über `dbo.chargen` setzen.
- [x] Für die Sperre die Spalte `Gesperrt` in `dbo.chargen` verwenden.
- [ ] Wenn gesperrte Chargen gedruckt werden, muss auf dem Etikett sichtbar stehen, dass die Charge gesperrt ist.
- [x] Bei einer Sperre direkt eine E-Mail an `QSIntern` senden.
- [x] Für den E-Mail-Versand das bestehende System in [Data/WareneingangService.cs](../Data/WareneingangService.cs) verwenden.
- [x] Im UI den Text `Letzte Buchungen` in `Offene Wareneingänge` ändern.
- [x] Im UI die Dickenmessung anzeigen.
- [x] Im UI die Toleranz zur Dickenmessung anzeigen.

## Wareneingang Verwaltung

Datei:

- [Components/Pages/Verwaltung/WareneingangVerwaltung.razor](../Components/Pages/Verwaltung/WareneingangVerwaltung.razor)

- [x] Die Verwaltung soll immer buchen können.
- [x] Die Verwaltung darf beim Buchen nicht blockiert werden, nur weil nicht alle Felder gefüllt sind.
- [x] Pflichtfeldprüfung in der Verwaltung entsprechend entfernen oder umgehen.

## Manuelle Aufgaben

- [ ] Über das Auto-Export-Tool auch Non-Automotive-EBs übertragen.
- [ ] Wareneingangs-Anweisung anpassen.
- [ ] In die Wareneingangs-Anweisung aufnehmen, was der Werker bei einem Druckproblem machen kann.
- [ ] Wareneingangs-Anweisung Dickenmessung Fehlermeldung beschreiben.
- [x] In `dbo.Materialliste` ein neues Feld `Dickenmessung` erstellen.
- [ ] Default-Dickenmessung pro Material in `dbo.Materialliste` eintragen.
- [x] Sicherstellen, dass alle Materialien in `dbo.Materialliste` vorhanden sind.
- [ ] Regal QRCodes erstellen und kleben.

## Chargensperre System

Datei:

- [Components/Pages/Produktion/Produktionslayout.razor](../Components/Pages/Produktion/Produktionslayout.razor)

- [x] Im Produktionslayout im rot markierten Bereich aus dem Bild einen neuen Regalplatz ergänzen.
- [x] In `dbo.Lagerorte` die 12 Sperrlager-Plätze `H2R17P1S` bis `H2R17P12S` für Halle 2, Regal 17 anlegen.
- [ ] Der neue Regalplatz soll alle gesperrten Chargen enthalten.
- [x] Chargen entsperren soll im Produktionslayout über die Regalview möglich sein.
- [x] Chargen vermüllen soll im Produktionslayout über die Regalview möglich sein.
- [x] Für das Chargenregal eine eigene Regalview erstellen.
- [x] Als Vorlage die vorhandene Logik oder Ansicht für Regal `43-47` prüfen.
- [x] Für das Chargenregal nur 2 Regale anzeigen, nicht 5 wie bei Regal `43-47`.
- [x] Beim Klick auf eine Charge im Chargenregal nicht die Chargenanalyse öffnen.
- [x] Im Sperrlager eine kleine Liste mit gesperrten Chargen anzeigen, die noch nicht eingelagert sind.
- [x] Beim Klick auf eine Charge im Chargenregal ein Popup mit detaillierten Informationen öffnen.
- [x] Das Popup soll anzeigen, warum die Charge gesperrt wurde.
- [x] Das Popup soll anzeigen, wo die Charge gesperrt wurde.
- [x] Das Popup soll anzeigen, von wem die Charge gesperrt wurde.
- [x] Das Popup soll Materialinformationen anzeigen.
- [x] Das Popup soll Zeitinformationen anzeigen.
- [x] Für die Detailinformationen zur Chargensperre eine extra Datentabelle anlegen.
- [x] In der neuen Datentabelle alle nötigen Informationen zur Sperre speichern, damit das Popup vollständig gefüllt werden kann.
