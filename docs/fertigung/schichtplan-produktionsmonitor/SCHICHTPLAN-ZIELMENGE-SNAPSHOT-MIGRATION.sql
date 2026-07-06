USE [Fertigung];
GO

SET XACT_ABORT ON;
BEGIN TRAN;

IF COL_LENGTH(N'dbo.SchichtplanEintrag', N'MaterialZielMenge') IS NULL
BEGIN
    ALTER TABLE dbo.SchichtplanEintrag
        ADD MaterialZielMenge INT NULL;
END;

IF COL_LENGTH(N'dbo.SchichtplanEintrag', N'Material2ZielMenge') IS NULL
BEGIN
    ALTER TABLE dbo.SchichtplanEintrag
        ADD Material2ZielMenge INT NULL;
END;

-- Nur heutige und zukünftige Pläne bekommen beim Migrationslauf die aktuelle Tagesvorgabe.
-- Historische Tage bleiben bewusst NULL, damit spätere Materialänderungen nicht rückwirkend
-- als falsche Zielmenge angezeigt werden.
UPDATE entryRow
SET MaterialZielMenge = materialRow.TagesMenge
FROM dbo.SchichtplanEintrag entryRow
INNER JOIN dbo.SchichtplanPlan planRow
    ON planRow.ID = entryRow.SchichtplanPlanID
INNER JOIN dbo.SchichtplanMaterialStamm materialRow
    ON materialRow.ID = entryRow.MaterialStammID
WHERE entryRow.MaterialZielMenge IS NULL
  AND planRow.PlanDatum >= CAST(GETDATE() AS date);

UPDATE entryRow
SET Material2ZielMenge = materialRow.TagesMenge
FROM dbo.SchichtplanEintrag entryRow
INNER JOIN dbo.SchichtplanPlan planRow
    ON planRow.ID = entryRow.SchichtplanPlanID
INNER JOIN dbo.SchichtplanMaterialStamm materialRow
    ON materialRow.ID = entryRow.MaterialStammID2
WHERE entryRow.Material2ZielMenge IS NULL
  AND planRow.PlanDatum >= CAST(GETDATE() AS date);

COMMIT TRAN;
GO
