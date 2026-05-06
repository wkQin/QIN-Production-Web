-- Update 3.2.5
-- Neue Spalten für materialbezogene Dickenmessung-Toleranz und gesperrte Menge.
-- Für 10 Prozent Toleranz in Dickenmessung_Toleranz den Wert 10 oder 10.00 eintragen.

USE [qinFSK\table1];

IF COL_LENGTH('dbo.Materialliste', 'Dickenmessung_Toleranz') IS NULL
BEGIN
    ALTER TABLE dbo.Materialliste
    ADD Dickenmessung_Toleranz decimal(5, 2) NULL;
END;
GO

USE [Fertigung];

IF COL_LENGTH('dbo.Sperrlager', 'GesperrteMenge') IS NULL
BEGIN
    ALTER TABLE dbo.Sperrlager
    ADD GesperrteMenge int NULL;
END;
GO
