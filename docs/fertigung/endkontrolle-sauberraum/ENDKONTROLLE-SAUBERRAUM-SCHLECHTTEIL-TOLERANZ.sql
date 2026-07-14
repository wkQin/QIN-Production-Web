IF COL_LENGTH('dbo.Materialliste', 'Schlechtteile_Toleranz') IS NULL
BEGIN
    ALTER TABLE dbo.Materialliste
    ADD Schlechtteile_Toleranz DECIMAL(10,2) NULL;
END;

IF COL_LENGTH('dbo.Materialliste', 'Schlechtteile_Vertragswert') IS NULL
BEGIN
    ALTER TABLE dbo.Materialliste
    ADD Schlechtteile_Vertragswert DECIMAL(10,2) NULL;
END;
