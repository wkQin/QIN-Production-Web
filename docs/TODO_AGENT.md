Erste Regel: Vor jeder Aufgabe zuerst [ADMIN-UND-KI-GUIDE.md](ADMIN-UND-KI-GUIDE.md) für die Grundanweisungen prüfen.

# TODO Agent

Wenn ein Punkt erledigt ist, die Checkbox von `[ ]` auf `[x]` ändern.

## Wareneingang

Dateien:

- [Components/Pages/Fertigung/Wareneingang.razor](../Components/Pages/Fertigung/Wareneingang.razor)
- [Data/WareneingangService.cs](../Data/WareneingangService.cs)

Aufgaben:

- [x] Bemerkung in den offenen Wareneingängen rot und deutlich sichtbar anzeigen, weil Bemerkungen meist wichtige Hinweise sind.
- [x] Prüfen, an welchen Stellen die Bemerkung in der Wareneingang-Tabelle und in den offenen Wareneingängen ausgegeben wird.
- [x] Die rote Darstellung so umsetzen, dass sie im Darkmode und Lightmode gut lesbar bleibt.
- [x] In `dbo.Materialliste` eine neue Spalte `Dickenmessung_Toleranz` für die materialbezogene Dickenmessung-Toleranz einplanen.
- [x] Für `Dickenmessung_Toleranz` als Prozentwert arbeiten: Für 10 Prozent Toleranz den Wert `10` oder `10.00` eintragen, nicht `0.10`.
- [x] Das Wareneingang-System mit `Dickenmessung_Toleranz` verknüpfen.
- [x] Wenn bei einem Material keine Toleranz eingetragen ist, automatisch 10 Prozent verwenden.
- [x] Im Wareneingang sichtbar machen, wenn die Standard-Toleranz von 10 Prozent verwendet wird, weil keine materialbezogene Toleranz vorhanden ist.
- [x] Dickenmessung-Prüfung so anpassen, dass pro Material die jeweilige Toleranz aus `dbo.Materialliste.Dickenmessung_Toleranz` gilt.
- [x] Backend-Dokumentation für Wareneingang um die neue Toleranz-Spalte und die 10-Prozent-Fallback-Regel ergänzen.

## Sperrlager

Dateien:

- [Components/Pages/Produktion/RegalModal.razor](../Components/Pages/Produktion/RegalModal.razor)
- [Data/ProduktionslayoutService.cs](../Data/ProduktionslayoutService.cs)
- [Data/WareneingangService.cs](../Data/WareneingangService.cs)
- [docs/fertigung/sperrlager/SPERRLAGER-BACKEND.md](fertigung/sperrlager/SPERRLAGER-BACKEND.md)

Aufgaben:

- [x] Im Sperrlager-Chargenpopup anzeigen, wer den Wareneingang für die Charge gemacht hat.
- [x] Beim Klick auf eine Charge die Wareneingang-Informationen inklusive Benutzer klar sichtbar laden.
- [x] Gesperrte Menge sauber speichern und anzeigen.
- [x] Bei automatischer Sperrung im Wareneingang die Gesamtmenge der Charge als gesperrte Menge verwenden.
- [x] Bei manueller Sperrung im Sperrlager eine Menge abfragen, damit nicht automatisch die Gesamtmenge angenommen wird.
- [x] Entscheiden, ob die gesperrte Menge direkt in `dbo.Chargen` oder in `dbo.Sperrlager` gespeichert wird.
- [x] Bei der Tabellenentscheidung sicherstellen, dass später immer nachvollziehbar ist, wie viel von einer Charge gesperrt wurde.
- [x] Sperrlager-Chargenpopup übersichtlicher gestalten.
- [x] Im Popup einen eigenen Bereich für Wareneingang-Informationen anzeigen.
- [x] Im Popup einen getrennten Bereich für Sperr-Informationen anzeigen.
- [x] Popup so strukturieren, dass Charge, Menge, gesperrte Menge, Sperrgrund, Benutzer, Wareneingang und Lagerort schnell lesbar sind.
- [x] Backend-Dokumentation für Sperrlager um Wareneingang-Benutzer, gesperrte Menge und Popup-Struktur ergänzen.
