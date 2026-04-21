param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\\docs\\ARBEITSANWEISUNG-WARENEINGANG.docx")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Security

function Escape-Xml {
    param([AllowNull()][string]$Text)

    if ($null -eq $Text) {
        return ""
    }

    return [System.Security.SecurityElement]::Escape($Text)
}

function New-ParagraphXml {
    param(
        [string]$Text,
        [int]$Size = 22,
        [bool]$Bold = $false,
        [string]$Color = "1F2937",
        [string]$Align = "start",
        [int]$Before = 0,
        [int]$After = 120,
        [bool]$KeepNext = $false
    )

    $escapedText = Escape-Xml $Text
    $boldXml = if ($Bold) { "<w:b/>" } else { "" }
    $keepNextXml = if ($KeepNext) { "<w:keepNext/>" } else { "" }

    return @"
<w:p>
  <w:pPr>
    $keepNextXml
    <w:spacing w:before="$Before" w:after="$After"/>
    <w:jc w:val="$Align"/>
  </w:pPr>
  <w:r>
    <w:rPr>
      <w:rFonts w:ascii="Calibri" w:hAnsi="Calibri"/>
      $boldXml
      <w:color w:val="$Color"/>
      <w:sz w:val="$Size"/>
    </w:rPr>
    <w:t xml:space="preserve">$escapedText</w:t>
  </w:r>
</w:p>
"@
}

function New-LabelValueParagraphXml {
    param(
        [string]$Label,
        [string]$Value,
        [int]$After = 140
    )

    $escapedLabel = Escape-Xml $Label
    $escapedValue = Escape-Xml $Value

    return @"
<w:p>
  <w:pPr>
    <w:spacing w:before="0" w:after="$After"/>
  </w:pPr>
  <w:r>
    <w:rPr>
      <w:rFonts w:ascii="Calibri" w:hAnsi="Calibri"/>
      <w:b/>
      <w:color w:val="123B64"/>
      <w:sz w:val="24"/>
    </w:rPr>
    <w:t xml:space="preserve">${escapedLabel}: </w:t>
  </w:r>
  <w:r>
    <w:rPr>
      <w:rFonts w:ascii="Calibri" w:hAnsi="Calibri"/>
      <w:color w:val="1F2937"/>
      <w:sz w:val="22"/>
    </w:rPr>
    <w:t>$escapedValue</w:t>
  </w:r>
</w:p>
"@
}

function New-ImagePlaceholderXml {
    param(
        [string]$Label,
        [int]$Height = 2100
    )

    $escapedLabel = Escape-Xml $Label

    return @"
<w:tbl>
  <w:tblPr>
    <w:tblW w:w="9800" w:type="dxa"/>
    <w:jc w:val="center"/>
    <w:tblBorders>
      <w:top w:val="single" w:sz="8" w:color="9CA3AF"/>
      <w:left w:val="single" w:sz="8" w:color="9CA3AF"/>
      <w:bottom w:val="single" w:sz="8" w:color="9CA3AF"/>
      <w:right w:val="single" w:sz="8" w:color="9CA3AF"/>
    </w:tblBorders>
  </w:tblPr>
  <w:tblGrid>
    <w:gridCol w:w="9800"/>
  </w:tblGrid>
  <w:tr>
    <w:trPr>
      <w:trHeight w:val="$Height" w:hRule="exact"/>
    </w:trPr>
    <w:tc>
      <w:tcPr>
        <w:tcW w:w="9800" w:type="dxa"/>
        <w:shd w:val="clear" w:fill="F3F4F6"/>
        <w:vAlign w:val="center"/>
      </w:tcPr>
      <w:p>
        <w:pPr>
          <w:spacing w:before="0" w:after="0"/>
          <w:jc w:val="center"/>
        </w:pPr>
        <w:r>
          <w:rPr>
            <w:rFonts w:ascii="Calibri" w:hAnsi="Calibri"/>
            <w:b/>
            <w:color w:val="6B7280"/>
            <w:sz w:val="22"/>
          </w:rPr>
          <w:t>$escapedLabel</w:t>
        </w:r>
      </w:p>
    </w:tc>
  </w:tr>
</w:tbl>
"@
}

function New-PageBreakXml {
    return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'
}

$pages = @(
    @(
        (New-ParagraphXml -Text "Arbeitsanweisung Wareneingang" -Size 34 -Bold $true -Color "123B64" -Align "center" -Before 0 -After 120 -KeepNext $true),
        (New-ParagraphXml -Text "Kurzanleitung fuer Werkerinnen und Werker" -Size 22 -Color "5B6470" -Align "center" -Before 0 -After 100 -KeepNext $true),
        (New-ParagraphXml -Text "Stand: 20.04.2026" -Size 20 -Color "6B7280" -Align "center" -Before 0 -After 260),
        (New-ParagraphXml -Text "Teil 1: Formular ausfuellen" -Size 28 -Bold $true -Color "123B64" -Before 0 -After 180 -KeepNext $true),
        (New-ParagraphXml -Text "1. Lieferant auswaehlen." -Size 24 -Bold $true -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 1 hier einfuegen" -Height 1850),
        (New-ParagraphXml -Text "" -After 120),
        (New-ParagraphXml -Text "2. Passenden Eintrag aus der Liste unten auswaehlen." -Size 24 -Bold $true -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 2 hier einfuegen" -Height 1850),
        (New-ParagraphXml -Text "" -After 120),
        (New-ParagraphXml -Text "3. Lieferschein vom Lieferpapier eintragen." -Size 24 -Bold $true -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 3 hier einfuegen" -Height 1850)
    ),
    @(
        (New-ParagraphXml -Text "Teil 2: Ware pruefen" -Size 28 -Bold $true -Color "123B64" -Before 0 -After 180 -KeepNext $true),
        (New-ParagraphXml -Text "4. Zustand der Ware auswaehlen." -Size 24 -Bold $true -Before 0 -After 60 -KeepNext $true),
        (New-ParagraphXml -Text "Wenn Zustand Schlecht ist: Bemerkung schreiben und QS kontaktieren." -Size 21 -Color "374151" -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 4 hier einfuegen" -Height 1750),
        (New-ParagraphXml -Text "" -After 120),
        (New-ParagraphXml -Text "5. Palettentausch auf Ja oder Nein setzen." -Size 24 -Bold $true -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 5 hier einfuegen" -Height 1750),
        (New-ParagraphXml -Text "" -After 120),
        (New-ParagraphXml -Text "6. Dickenmessung eintragen." -Size 24 -Bold $true -Before 0 -After 60 -KeepNext $true),
        (New-ParagraphXml -Text "Nur Werte zwischen 0,23 mm und 1,2 mm sind erlaubt." -Size 21 -Color "374151" -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 6 hier einfuegen" -Height 1750)
    ),
    @(
        (New-ParagraphXml -Text "Teil 3: Chargen" -Size 28 -Bold $true -Color "123B64" -Before 0 -After 180 -KeepNext $true),
        (New-ParagraphXml -Text "7. Wenn alles ausgefuellt ist, zu den Chargen gehen." -Size 24 -Bold $true -Before 0 -After 120 -KeepNext $true),
        (New-ParagraphXml -Text "Variante A: Chargen sind schon eingetragen." -Size 22 -Bold $true -Color "123B64" -Before 0 -After 50 -KeepNext $true),
        (New-ParagraphXml -Text "Charge und Menge kontrollieren." -Size 21 -Color "374151" -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 7A hier einfuegen" -Height 1600),
        (New-ParagraphXml -Text "" -After 120),
        (New-ParagraphXml -Text "Variante B: Charge ist noch nicht eingetragen." -Size 22 -Bold $true -Color "123B64" -Before 0 -After 50 -KeepNext $true),
        (New-ParagraphXml -Text "Charge scannen und Menge eingeben." -Size 21 -Color "374151" -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 7B hier einfuegen" -Height 1600)
    ),
    @(
        (New-ParagraphXml -Text "Teil 4: Abschluss" -Size 28 -Bold $true -Color "123B64" -Before 0 -After 180 -KeepNext $true),
        (New-ParagraphXml -Text "8. Chargen-Etiketten drucken." -Size 24 -Bold $true -Before 0 -After 60 -KeepNext $true),
        (New-ParagraphXml -Text "Etikett auf Karton oder Gebinde kleben." -Size 21 -Color "374151" -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 8 hier einfuegen" -Height 2200),
        (New-ParagraphXml -Text "" -After 140),
        (New-ParagraphXml -Text "9. Zum Schluss speichern." -Size 24 -Bold $true -Before 0 -After 60 -KeepNext $true),
        (New-ParagraphXml -Text "Auf Wareneingang Buchen oder Aenderungen Speichern klicken." -Size 21 -Color "374151" -Before 0 -After 80 -KeepNext $true),
        (New-ImagePlaceholderXml -Label "Bild Schritt 9 hier einfuegen" -Height 2200)
    ),
    @(
        (New-ParagraphXml -Text "Wichtige Punkte" -Size 30 -Bold $true -Color "123B64" -Before 0 -After 180 -KeepNext $true),
        (New-ParagraphXml -Text "Diese Punkte vor dem Speichern immer beachten." -Size 22 -Color "374151" -Before 0 -After 180),
        (New-LabelValueParagraphXml -Label "Pflichtfelder" -Value "Alle Pflichtfelder muessen ausgefuellt sein."),
        (New-LabelValueParagraphXml -Label "Hinweistext" -Value "Wenn etwas fehlt oder falsch ist, erscheint ein Hinweistext. Feld ausfuellen oder korrigieren und danach erneut speichern."),
        (New-LabelValueParagraphXml -Label "Lieferant und Lieferschein" -Value "Beides immer kontrollieren, bevor gespeichert wird."),
        (New-LabelValueParagraphXml -Label "Zustand Schlecht" -Value "Immer Bemerkung schreiben und QS kontaktieren."),
        (New-LabelValueParagraphXml -Label "Palettentausch" -Value "Immer Ja oder Nein richtig auswaehlen."),
        (New-LabelValueParagraphXml -Label "Dickenmessung" -Value "Nur Werte zwischen 0,23 mm und 1,2 mm eintragen."),
        (New-LabelValueParagraphXml -Label "Vorhandene Chargen" -Value "Charge und Menge immer kontrollieren."),
        (New-LabelValueParagraphXml -Label "Neue Charge" -Value "Charge scannen und Menge richtig eingeben."),
        (New-LabelValueParagraphXml -Label "Etikett" -Value "Etikett immer auf den richtigen Karton oder das richtige Gebinde kleben.")
    )
)

$bodyParts = New-Object System.Collections.Generic.List[string]

for ($i = 0; $i -lt $pages.Count; $i++) {
    foreach ($xmlPart in $pages[$i]) {
        [void]$bodyParts.Add($xmlPart)
    }

    if ($i -lt ($pages.Count - 1)) {
        [void]$bodyParts.Add((New-PageBreakXml))
    }
}

$documentXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    $($bodyParts -join [Environment]::NewLine)
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="1080" w:right="900" w:bottom="1080" w:left="900" w:header="708" w:footer="708" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>
"@

$contentTypesXml = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>
'@

$relationshipsXml = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
'@

$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Path $resolvedOutputPath -Parent

if (-not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

if (Test-Path $resolvedOutputPath) {
    Remove-Item -LiteralPath $resolvedOutputPath -Force
}

$fileStream = [System.IO.File]::Open($resolvedOutputPath, [System.IO.FileMode]::CreateNew)

try {
    $archive = New-Object System.IO.Compression.ZipArchive($fileStream, [System.IO.Compression.ZipArchiveMode]::Create, $false)

    try {
        $files = [ordered]@{
            "[Content_Types].xml" = $contentTypesXml
            "_rels/.rels" = $relationshipsXml
            "word/document.xml" = $documentXml
        }

        foreach ($entryName in $files.Keys) {
            $entry = $archive.CreateEntry($entryName)
            $writer = New-Object System.IO.StreamWriter($entry.Open(), [System.Text.UTF8Encoding]::new($false))
            $writer.Write($files[$entryName])
            $writer.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $fileStream.Dispose()
}

Write-Host "DOCX erstellt: $resolvedOutputPath"
