using System;
using System.Net;
using QRCoder;

namespace QIN_Production_Web.Helpers
{
    public static class WareneingangPrintHelper
    {
        public static string BuildSingleChargePrintDocument(string charge, string menge, string material, string eingangsdatum)
        {
            if (string.IsNullOrWhiteSpace(charge))
            {
                throw new ArgumentException("Charge ist leer.", nameof(charge));
            }

            string safeCharge = Encode(charge);
            string safeMenge = Encode(string.IsNullOrWhiteSpace(menge) ? "0" : menge.Trim());
            string safeMaterial = Encode(material);
            string safeDatum = Encode(eingangsdatum);
            string qrData = $"{charge}|{menge}|{material}|{eingangsdatum}";
            string qrSvg = BuildQrSvg(qrData);
            string screenHint = Encode("Druckdialog: Format 55 x 28 mm, Raender Keine, Skalierung 100 %, Kopf- und Fusszeilen aus.");

            return $$"""
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Wareneingang Etikett</title>
    <style>
        * {
            box-sizing: border-box;
        }

        @page {
            size: 55mm 28mm;
            margin: 0;
        }

        html, body {
            margin: 0;
            padding: 0;
            background: #fff;
            font-family: Arial, Helvetica, sans-serif;
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
        }

        body {
            color: #111;
        }

        .screen-note {
            max-width: 720px;
            margin: 16px auto 0;
            padding: 10px 14px;
            border-radius: 12px;
            background: #172554;
            color: #eff6ff;
            font-size: 14px;
            line-height: 1.4;
            text-align: center;
        }

        .preview-shell {
            min-height: calc(100vh - 76px);
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 24px;
            background:
                linear-gradient(180deg, #e2e8f0 0%, #cbd5e1 100%);
        }

        .label {
            position: relative;
            width: 55mm;
            height: 28mm;
            overflow: hidden;
            background: #fff;
            box-shadow: 0 10px 30px rgba(15, 23, 42, 0.18);
        }

        .qr {
            position: absolute;
            left: 0.7mm;
            top: 1.05mm;
            width: 19.2mm;
            height: 19.2mm;
        }

        .qr svg {
            width: 100%;
            height: 100%;
            display: block;
        }

        .text {
            position: absolute;
            left: 22.45mm;
            width: 31.2mm;
            min-width: 0;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        .caption {
            font-size: 2.3mm;
            font-weight: 700;
            line-height: 1;
        }

        .value {
            font-size: 3.35mm;
            font-weight: 700;
            line-height: 1;
        }

        .line {
            font-size: 2.4mm;
            line-height: 1.05;
            word-break: break-word;
        }

        .charge-label {
            top: 1.9mm;
        }

        .charge-value {
            top: 4.15mm;
            letter-spacing: -0.03em;
        }

        .menge-label {
            top: 9.55mm;
        }

        .menge-value {
            top: 11.85mm;
            font-size: 2.95mm;
            font-weight: 700;
        }

        .material-label {
            top: 16.95mm;
        }

        .material {
            top: 19.15mm;
            width: 30.7mm;
            height: 5.1mm;
            white-space: normal;
            overflow: hidden;
            display: -webkit-box;
            -webkit-box-orient: vertical;
            -webkit-line-clamp: 2;
            line-clamp: 2;
        }

        .date {
            position: absolute;
            left: 22.45mm;
            top: 24.95mm;
            width: 31.2mm;
            font-size: 2.15mm;
            font-weight: 600;
            line-height: 1;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        @media print {
            html, body {
                width: 55mm;
                height: 28mm;
                overflow: hidden;
                background: #fff;
            }

            .screen-note {
                display: none !important;
            }

            .preview-shell {
                min-height: 0;
                padding: 0;
                background: #fff;
            }

            .label {
                box-shadow: none;
            }
        }
    </style>
</head>
<body>
    <div class="screen-note">{{screenHint}}</div>
    <div class="preview-shell">
        <div class="label">
            <div class="qr">{{qrSvg}}</div>
            <div class="text caption charge-label">Charge:</div>
            <div class="text value charge-value">{{safeCharge}}</div>
            <div class="text caption menge-label">Menge:</div>
            <div class="text menge-value">{{safeMenge}} LM/STK</div>
            <div class="text caption material-label">Material:</div>
            <div class="text line material">{{safeMaterial}}</div>
            <div class="date">Datum: {{safeDatum}}</div>
        </div>
    </div>
</body>
</html>
""";
        }

        private static string BuildQrSvg(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new SvgQRCode(qrCodeData);

            return qrCode.GetGraphic(4, "#000000", "#ffffff", false, SvgQRCode.SizingMode.ViewBoxAttribute, null);
        }

        private static string Encode(string? value)
        {
            return WebUtility.HtmlEncode(value?.Trim() ?? string.Empty);
        }
    }
}
