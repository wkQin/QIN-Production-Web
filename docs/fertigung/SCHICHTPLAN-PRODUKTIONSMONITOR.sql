USE [Fertigung];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'dbo.SchichtplanPlan', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SchichtplanPlan
        (
            ID                  INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_SchichtplanPlan PRIMARY KEY,
            PlanDatum           DATE NOT NULL,
            Kalenderwoche       TINYINT NOT NULL,
            Titel               NVARCHAR(100) NULL,
            Bemerkung           NVARCHAR(500) NULL,
            CreatedAt           DATETIME2(0) NOT NULL
                CONSTRAINT DF_SchichtplanPlan_CreatedAt DEFAULT SYSDATETIME(),
            CreatedBy           NVARCHAR(150) NULL,
            ZuletztGeaendertAm  DATETIME2(0) NOT NULL
                CONSTRAINT DF_SchichtplanPlan_ZuletztGeaendertAm DEFAULT SYSDATETIME(),
            ZuletztGeaendertVon NVARCHAR(150) NULL,

            CONSTRAINT UQ_SchichtplanPlan_PlanDatum UNIQUE (PlanDatum),
            CONSTRAINT CK_SchichtplanPlan_Kalenderwoche CHECK (Kalenderwoche BETWEEN 1 AND 53)
        );
    END;

    IF OBJECT_ID(N'dbo.SchichtplanArbeitsplatz', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SchichtplanArbeitsplatz
        (
            ID                     INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_SchichtplanArbeitsplatz PRIMARY KEY,
            Bereich                NVARCHAR(100) NOT NULL,
            BereichSortierung      INT NOT NULL,
            ArbeitsplatzNr         NVARCHAR(20) NULL,
            ArbeitsplatzName       NVARCHAR(120) NOT NULL,
            ArbeitsplatzSortierung INT NOT NULL,
            Aktiv                  BIT NOT NULL
                CONSTRAINT DF_SchichtplanArbeitsplatz_Aktiv DEFAULT (1),
            CreatedAt              DATETIME2(0) NOT NULL
                CONSTRAINT DF_SchichtplanArbeitsplatz_CreatedAt DEFAULT SYSDATETIME(),
            UpdatedAt              DATETIME2(0) NULL,

            CONSTRAINT UQ_SchichtplanArbeitsplatz UNIQUE (Bereich, ArbeitsplatzNr, ArbeitsplatzName)
        );
    END;

    IF OBJECT_ID(N'dbo.SchichtplanMaterialStamm', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SchichtplanMaterialStamm
        (
            ID          INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_SchichtplanMaterialStamm PRIMARY KEY,
            Material    NVARCHAR(200) NOT NULL,
            Sortierung  INT NOT NULL
                CONSTRAINT DF_SchichtplanMaterialStamm_Sortierung DEFAULT (0),
            IstStandard BIT NOT NULL
                CONSTRAINT DF_SchichtplanMaterialStamm_IstStandard DEFAULT (0),
            Aktiv       BIT NOT NULL
                CONSTRAINT DF_SchichtplanMaterialStamm_Aktiv DEFAULT (1),
            CreatedAt   DATETIME2(0) NOT NULL
                CONSTRAINT DF_SchichtplanMaterialStamm_CreatedAt DEFAULT SYSDATETIME(),
            CreatedBy   NVARCHAR(150) NULL,
            UpdatedAt   DATETIME2(0) NULL,

            CONSTRAINT UQ_SchichtplanMaterialStamm_Material UNIQUE (Material)
        );
    END;

    IF OBJECT_ID(N'dbo.SchichtplanZusatzBenutzer', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SchichtplanZusatzBenutzer
        (
            ID        INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_SchichtplanZusatzBenutzer PRIMARY KEY,
            Benutzer  NVARCHAR(150) NOT NULL,
            Aktiv     BIT NOT NULL
                CONSTRAINT DF_SchichtplanZusatzBenutzer_Aktiv DEFAULT (1),
            CreatedAt DATETIME2(0) NOT NULL
                CONSTRAINT DF_SchichtplanZusatzBenutzer_CreatedAt DEFAULT SYSDATETIME(),
            CreatedBy NVARCHAR(150) NULL,
            UpdatedAt DATETIME2(0) NULL,

            CONSTRAINT UQ_SchichtplanZusatzBenutzer_Benutzer UNIQUE (Benutzer)
        );
    END;

    IF OBJECT_ID(N'dbo.SchichtplanEintrag', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SchichtplanEintrag
        (
            ID                  INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_SchichtplanEintrag PRIMARY KEY,
            SchichtplanPlanID   INT NOT NULL,
            ArbeitsplatzID      INT NOT NULL,
            Schicht             NVARCHAR(20) NOT NULL,
            MaterialStammID     INT NULL,
            Material            NVARCHAR(200) NULL,
            FA_Nr               NVARCHAR(50) NULL,
            Bemerkung           NVARCHAR(500) NULL,
            CreatedAt           DATETIME2(0) NOT NULL
                CONSTRAINT DF_SchichtplanEintrag_CreatedAt DEFAULT SYSDATETIME(),
            CreatedBy           NVARCHAR(150) NULL,
            ZuletztGeaendertAm  DATETIME2(0) NOT NULL
                CONSTRAINT DF_SchichtplanEintrag_ZuletztGeaendertAm DEFAULT SYSDATETIME(),
            ZuletztGeaendertVon NVARCHAR(150) NULL,

            CONSTRAINT FK_SchichtplanEintrag_Plan
                FOREIGN KEY (SchichtplanPlanID) REFERENCES dbo.SchichtplanPlan(ID) ON DELETE CASCADE,
            CONSTRAINT FK_SchichtplanEintrag_Arbeitsplatz
                FOREIGN KEY (ArbeitsplatzID) REFERENCES dbo.SchichtplanArbeitsplatz(ID),
            CONSTRAINT UQ_SchichtplanEintrag UNIQUE (SchichtplanPlanID, ArbeitsplatzID, Schicht),
            CONSTRAINT CK_SchichtplanEintrag_Schicht
                CHECK (Schicht IN (N'Nachtschicht', N'Frühschicht', N'Spätschicht'))
        );
    END;

    IF OBJECT_ID(N'dbo.SchichtplanEintragBenutzer', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SchichtplanEintragBenutzer
        (
            ID                   INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_SchichtplanEintragBenutzer PRIMARY KEY,
            SchichtplanEintragID INT NOT NULL,
            SchichtplanPlanID    INT NOT NULL,
            BenutzerQuelle       NVARCHAR(20) NOT NULL
                CONSTRAINT DF_SchichtplanEintragBenutzer_BenutzerQuelle DEFAULT (N'LoginDaten'),
            BenutzerSchluessel   NVARCHAR(150) NOT NULL,
            Personalnummer       NVARCHAR(50) NULL,
            Benutzer             NVARCHAR(150) NOT NULL,
            Sortierung           TINYINT NOT NULL
                CONSTRAINT DF_SchichtplanEintragBenutzer_Sortierung DEFAULT (1),
            CreatedAt            DATETIME2(0) NOT NULL
                CONSTRAINT DF_SchichtplanEintragBenutzer_CreatedAt DEFAULT SYSDATETIME(),

            CONSTRAINT FK_SchichtplanEintragBenutzer_Eintrag
                FOREIGN KEY (SchichtplanEintragID) REFERENCES dbo.SchichtplanEintrag(ID) ON DELETE CASCADE,
            CONSTRAINT FK_SchichtplanEintragBenutzer_Plan
                FOREIGN KEY (SchichtplanPlanID) REFERENCES dbo.SchichtplanPlan(ID),
            CONSTRAINT UQ_SchichtplanEintragBenutzer UNIQUE (SchichtplanEintragID, Sortierung),
            CONSTRAINT CK_SchichtplanEintragBenutzer_Sortierung CHECK (Sortierung BETWEEN 1 AND 4),
            CONSTRAINT CK_SchichtplanEintragBenutzer_Quelle CHECK (BenutzerQuelle IN (N'LoginDaten', N'Manuell'))
        );
    END;

    IF COL_LENGTH(N'dbo.SchichtplanEintrag', N'MaterialStammID') IS NULL
    BEGIN
        ALTER TABLE dbo.SchichtplanEintrag
            ADD MaterialStammID INT NULL;
    END;

    IF COL_LENGTH(N'dbo.SchichtplanEintragBenutzer', N'SchichtplanPlanID') IS NULL
    BEGIN
        ALTER TABLE dbo.SchichtplanEintragBenutzer
            ADD SchichtplanPlanID INT NULL;
    END;

    IF COL_LENGTH(N'dbo.SchichtplanEintragBenutzer', N'BenutzerQuelle') IS NULL
    BEGIN
        ALTER TABLE dbo.SchichtplanEintragBenutzer
            ADD BenutzerQuelle NVARCHAR(20) NOT NULL
                CONSTRAINT DF_SchichtplanEintragBenutzer_BenutzerQuelle DEFAULT (N'LoginDaten');
    END;

    IF COL_LENGTH(N'dbo.SchichtplanEintragBenutzer', N'BenutzerSchluessel') IS NULL
    BEGIN
        ALTER TABLE dbo.SchichtplanEintragBenutzer
            ADD BenutzerSchluessel NVARCHAR(150) NULL;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.SchichtplanEintragBenutzer')
          AND name = N'SchichtplanPlanID'
    )
    BEGIN
        EXEC(N'
            UPDATE ben
            SET ben.SchichtplanPlanID = entryRow.SchichtplanPlanID
            FROM dbo.SchichtplanEintragBenutzer ben
            INNER JOIN dbo.SchichtplanEintrag entryRow
                ON entryRow.ID = ben.SchichtplanEintragID
            WHERE ben.SchichtplanPlanID IS NULL;
        ');
    END;

    EXEC(N'
        UPDATE dbo.SchichtplanEintragBenutzer
        SET BenutzerQuelle = N''LoginDaten''
        WHERE BenutzerQuelle IS NULL
           OR LTRIM(RTRIM(BenutzerQuelle)) = N'''';
    ');

    EXEC(N'
        UPDATE dbo.SchichtplanEintragBenutzer
        SET BenutzerSchluessel =
            CASE
                WHEN BenutzerQuelle = N''Manuell'' THEN
                    COALESCE(NULLIF(LTRIM(RTRIM(BenutzerSchluessel)), N''''), N''MANUELL:'' + CAST(ID AS NVARCHAR(20)))
                ELSE
                    COALESCE(
                        NULLIF(LTRIM(RTRIM(Personalnummer)), N''''),
                        NULLIF(N''LOGIN:'' + NULLIF(LTRIM(RTRIM(Benutzer)), N''''), N''LOGIN:''),
                        N''LOGIN:'' + CAST(ID AS NVARCHAR(20))
                    )
            END
        WHERE BenutzerSchluessel IS NULL
           OR LTRIM(RTRIM(BenutzerSchluessel)) = N'''';
    ');

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.SchichtplanEintragBenutzer')
          AND name = N'SchichtplanPlanID'
          AND is_nullable = 1
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.SchichtplanEintragBenutzer
                ALTER COLUMN SchichtplanPlanID INT NOT NULL;
        ');
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.SchichtplanEintragBenutzer')
          AND name = N'BenutzerSchluessel'
          AND is_nullable = 1
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.SchichtplanEintragBenutzer
                ALTER COLUMN BenutzerSchluessel NVARCHAR(150) NOT NULL;
        ');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_SchichtplanEintrag_MaterialStamm'
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.SchichtplanEintrag
                ADD CONSTRAINT FK_SchichtplanEintrag_MaterialStamm
                    FOREIGN KEY (MaterialStammID) REFERENCES dbo.SchichtplanMaterialStamm(ID);
        ');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_SchichtplanEintragBenutzer_Plan'
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.SchichtplanEintragBenutzer
                ADD CONSTRAINT FK_SchichtplanEintragBenutzer_Plan
                    FOREIGN KEY (SchichtplanPlanID) REFERENCES dbo.SchichtplanPlan(ID);
        ');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_SchichtplanEintragBenutzer_Quelle'
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.SchichtplanEintragBenutzer
                ADD CONSTRAINT CK_SchichtplanEintragBenutzer_Quelle
                    CHECK (BenutzerQuelle IN (N''LoginDaten'', N''Manuell''));
        ');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_SchichtplanArbeitsplatz_BereichSortierung_ArbeitsplatzSortierung'
          AND object_id = OBJECT_ID(N'dbo.SchichtplanArbeitsplatz')
    )
    BEGIN
        CREATE INDEX IX_SchichtplanArbeitsplatz_BereichSortierung_ArbeitsplatzSortierung
            ON dbo.SchichtplanArbeitsplatz (BereichSortierung, ArbeitsplatzSortierung);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_SchichtplanEintrag_Plan_Schicht'
          AND object_id = OBJECT_ID(N'dbo.SchichtplanEintrag')
    )
    BEGIN
        CREATE INDEX IX_SchichtplanEintrag_Plan_Schicht
            ON dbo.SchichtplanEintrag (SchichtplanPlanID, Schicht, ArbeitsplatzID);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_SchichtplanEintragBenutzer_Eintrag'
          AND object_id = OBJECT_ID(N'dbo.SchichtplanEintragBenutzer')
    )
    BEGIN
        CREATE INDEX IX_SchichtplanEintragBenutzer_Eintrag
            ON dbo.SchichtplanEintragBenutzer (SchichtplanEintragID, Sortierung);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_SchichtplanEintragBenutzer_Plan_Mitarbeiter'
          AND object_id = OBJECT_ID(N'dbo.SchichtplanEintragBenutzer')
    )
    BEGIN
        EXEC(N'
            CREATE UNIQUE INDEX UX_SchichtplanEintragBenutzer_Plan_Mitarbeiter
                ON dbo.SchichtplanEintragBenutzer (SchichtplanPlanID, BenutzerQuelle, BenutzerSchluessel);
        ');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_SchichtplanMaterialStamm_Aktiv_Sortierung'
          AND object_id = OBJECT_ID(N'dbo.SchichtplanMaterialStamm')
    )
    BEGIN
        CREATE INDEX IX_SchichtplanMaterialStamm_Aktiv_Sortierung
            ON dbo.SchichtplanMaterialStamm (Aktiv, Sortierung, Material);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_SchichtplanZusatzBenutzer_Aktiv_Benutzer'
          AND object_id = OBJECT_ID(N'dbo.SchichtplanZusatzBenutzer')
    )
    BEGIN
        CREATE INDEX IX_SchichtplanZusatzBenutzer_Aktiv_Benutzer
            ON dbo.SchichtplanZusatzBenutzer (Aktiv, Benutzer);
    END;

    IF EXISTS (
        SELECT 1
        FROM dbo.SchichtplanArbeitsplatz
        WHERE Bereich = N'Sonstiges'
          AND Aktiv = 1
    )
    AND NOT EXISTS (
        SELECT 1
        FROM dbo.SchichtplanArbeitsplatz
        WHERE Bereich = N'Sonstiges'
          AND ArbeitsplatzName = N'Sonstiges'
    )
    BEGIN
        ;WITH SonstigesPrimar AS
        (
            SELECT TOP (1) a.ID
            FROM dbo.SchichtplanArbeitsplatz a
            WHERE a.Bereich = N'Sonstiges'
            ORDER BY
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM dbo.SchichtplanEintrag e
                    WHERE e.ArbeitsplatzID = a.ID
                ) THEN 0 ELSE 1 END,
                CASE WHEN a.ArbeitsplatzName = N'Springer' THEN 0 ELSE 1 END,
                a.ArbeitsplatzSortierung,
                a.ID
        )
        UPDATE target
        SET ArbeitsplatzName = N'Sonstiges',
            ArbeitsplatzSortierung = 10,
            Aktiv = 1,
            UpdatedAt = SYSDATETIME()
        FROM dbo.SchichtplanArbeitsplatz target
        INNER JOIN SonstigesPrimar source
            ON source.ID = target.ID;
    END;

    ;WITH ArbeitsplatzSeed AS
    (
        SELECT *
        FROM (VALUES
            (10, N'Thermoformung', 10, N'005180', N'Geiss U8'),
            (10, N'Thermoformung', 20, N'005181', N'Geiss T10'),
            (10, N'Thermoformung', 30, N'005194', N'Rudholzer'),
            (10, N'Thermoformung', 40, N'005192', N'Parco'),
            (10, N'Thermoformung', 50, N'005193', N'Kiefel'),

            (20, N'Fräsen', 10, N'005525', N'Geiss Eco'),
            (20, N'Fräsen', 20, N'005528', N'MKM 4 K.'),
            (20, N'Fräsen', 30, N'005529', N'MKM 4 K.'),
            (20, N'Fräsen', 40, N'005543', N'MKM 2 K.'),
            (20, N'Fräsen', 50, N'005542', N'MKM 2 K.'),
            (20, N'Fräsen', 60, N'005541', N'MKM 2 K.'),
            (20, N'Fräsen', 70, N'005540', N'MKM 2 K.'),

            (30, N'Stanzen', 10, N'005317', N'Handstanze'),
            (30, N'Stanzen', 20, N'005318', N'Hydraulikstanze'),
            (30, N'Stanzen', 30, N'005319', N'Hydraulikstanze'),
            (30, N'Stanzen', 40, N'005320', N'Hydraulikstanze'),
            (30, N'Stanzen', 50, N'005321', N'Hydraulikstanze'),
            (30, N'Stanzen', 60, N'005322', N'Hydraulikstanze'),

            (40, N'UV', 10, N'005410', N'UV-Anlage'),

            (50, N'Ohne Bereich', 10, NULL, N'Biegemaschine'),

            (60, N'Sauberraum', 10, N'005591', N'Tisch 1'),
            (60, N'Sauberraum', 20, N'005592', N'Tisch 2'),
            (60, N'Sauberraum', 30, N'005593', N'Tisch 3'),
            (60, N'Sauberraum', 40, N'005594', N'Tisch 4'),
            (60, N'Sauberraum', 50, N'005595', N'Tisch 5'),
            (60, N'Sauberraum', 60, N'005596', N'Tisch 6'),
            (60, N'Sauberraum', 70, N'005597', N'Tisch 7'),
            (60, N'Sauberraum', 80, N'005598', N'Tisch 8'),
            (60, N'Sauberraum', 90, N'005599', N'Tisch 9'),
            (60, N'Sauberraum', 100, N'005600', N'Tisch 10'),
            (60, N'Sauberraum', 110, N'005600', N'Tisch 11'),
            (60, N'Sauberraum', 120, N'005600', N'Tisch 12'),

            (70, N'Sonstiges', 10, NULL, N'Sonstiges')
        ) AS sourceData(BereichSortierung, Bereich, ArbeitsplatzSortierung, ArbeitsplatzNr, ArbeitsplatzName)
    )
    MERGE dbo.SchichtplanArbeitsplatz AS target
    USING ArbeitsplatzSeed AS source
       ON target.Bereich = source.Bereich
      AND ISNULL(target.ArbeitsplatzNr, N'') = ISNULL(source.ArbeitsplatzNr, N'')
      AND target.ArbeitsplatzName = source.ArbeitsplatzName
    WHEN MATCHED THEN
        UPDATE SET
            target.BereichSortierung = source.BereichSortierung,
            target.ArbeitsplatzSortierung = source.ArbeitsplatzSortierung,
            target.Aktiv = 1,
            target.UpdatedAt = SYSDATETIME()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Bereich, BereichSortierung, ArbeitsplatzNr, ArbeitsplatzName, ArbeitsplatzSortierung, Aktiv)
        VALUES (source.Bereich, source.BereichSortierung, source.ArbeitsplatzNr, source.ArbeitsplatzName, source.ArbeitsplatzSortierung, 1);

    ;WITH SonstigesKanonisch AS
    (
        SELECT TOP (1) ID
        FROM dbo.SchichtplanArbeitsplatz
        WHERE Bereich = N'Sonstiges'
          AND ArbeitsplatzName = N'Sonstiges'
        ORDER BY ArbeitsplatzSortierung, ID
    )
    UPDATE eintrag
    SET ArbeitsplatzID = ziel.ID
    FROM dbo.SchichtplanEintrag eintrag
    INNER JOIN dbo.SchichtplanArbeitsplatz quelle
        ON quelle.ID = eintrag.ArbeitsplatzID
    CROSS JOIN SonstigesKanonisch ziel
    WHERE quelle.Bereich = N'Sonstiges'
      AND quelle.ID <> ziel.ID
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.SchichtplanEintrag konflikt
            WHERE konflikt.SchichtplanPlanID = eintrag.SchichtplanPlanID
              AND konflikt.ArbeitsplatzID = ziel.ID
              AND konflikt.Schicht = eintrag.Schicht
      );

    IF EXISTS (
        SELECT 1
        FROM dbo.SchichtplanEintrag eintrag
        INNER JOIN dbo.SchichtplanArbeitsplatz platz
            ON platz.ID = eintrag.ArbeitsplatzID
        WHERE platz.Bereich = N'Sonstiges'
          AND platz.ArbeitsplatzName <> N'Sonstiges'
    )
    BEGIN
        THROW 51000, N'Sonstiges-Einträge konnten nicht vollständig auf den Arbeitsplatz Sonstiges konsolidiert werden.', 1;
    END;

    DELETE platz
    FROM dbo.SchichtplanArbeitsplatz platz
    WHERE platz.Bereich = N'Sonstiges'
      AND platz.ArbeitsplatzName <> N'Sonstiges'
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.SchichtplanEintrag eintrag
            WHERE eintrag.ArbeitsplatzID = platz.ID
      );

    ;WITH MaterialSeed AS
    (
        SELECT *
        FROM (VALUES
            (10, N'BMW BFS LHD', 1),
            (20, N'BMW BFS RHD', 1),
            (30, N'BMW CBF LHD', 1),
            (40, N'BMW CBF RHD', 1),
            (50, N'Ford Kuga Tür LH Black Weave', 1),
            (60, N'Matikon Rollo', 1)
        ) AS sourceData(Sortierung, Material, IstStandard)
    )
    MERGE dbo.SchichtplanMaterialStamm AS target
    USING MaterialSeed AS source
       ON target.Material = source.Material
    WHEN MATCHED THEN
        UPDATE SET
            target.Sortierung = source.Sortierung,
            target.IstStandard = source.IstStandard,
            target.Aktiv = 1,
            target.UpdatedAt = SYSDATETIME()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Material, Sortierung, IstStandard, Aktiv)
        VALUES (source.Material, source.Sortierung, source.IstStandard, 1);

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    THROW;
END CATCH;
GO
