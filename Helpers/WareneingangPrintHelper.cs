using System;

namespace QIN_Production_Web.Helpers
{
    public static class WareneingangPrintHelper
    {
        public static string BuildSingleChargeZpl(string charge, string menge, string material, string eingangsdatum, bool gesperrt = false)
        {
            if (string.IsNullOrWhiteSpace(charge))
            {
                throw new ArgumentException("Charge ist leer.", nameof(charge));
            }

            int dpi = 203;
            double labelWidthMm = 55.0;
            double labelHeightMm = 28.0;
            double dpm = dpi / 25.4;
            int labelWidth = (int)Math.Round(labelWidthMm * dpm);
            int labelHeight = (int)Math.Round(labelHeightMm * dpm);

            string safeCharge = SanitizeZpl(charge);
            string safeMenge = SanitizeZpl(string.IsNullOrWhiteSpace(menge) ? "0" : menge.Trim());
            string safeMaterial = SanitizeZpl(material);
            string safeDatum = SanitizeZpl(eingangsdatum);
            string qrData = SanitizeZpl($"{charge}|{safeMenge}|{material}|{eingangsdatum}");
            string labelText = gesperrt
                ? "^FO210,15^A0N,18,18^FDCharge:^FS" +
                  $"^FO210,34^A0N,23,23^FD{safeCharge}^FS" +
                  "^FO210,63^GB220,28,28^FS" +
                  "^FO229,67^FR^A0N,22,22^FDGESPERRT^FS" +
                  "^FO210,97^A0N,18,18^FDMenge:^FS" +
                  $"^FO210,116^A0N,22,22^FD{safeMenge} LM/STK^FS" +
                  "^FO210,150^A0N,18,18^FDMaterial:^FS" +
                  $"^FO210,169^A0N,20,20^FB220,2,,L^FD{safeMaterial}^FS" +
                  $"^FO210,205^A0N,18,18^FDEingang: {safeDatum}^FS"
                : "^FO210,15^A0N,20,20^FDCharge:^FS" +
                  $"^FO210,35^A0N,25,25^FD{safeCharge}^FS" +
                  "^FO210,75^A0N,20,20^FDMenge:^FS" +
                  $"^FO210,95^A0N,24,24^FD{safeMenge} LM/STK^FS" +
                  "^FO210,135^A0N,20,20^FDMaterial:^FS" +
                  $"^FO210,155^A0N,22,22^FB220,2,,L^FD{safeMaterial}^FS" +
                  $"^FO210,200^A0N,20,20^FDEingang: {safeDatum}^FS";

            return
                "^XA" +
                "^CI28" +
                $"^PW{labelWidth}" +
                $"^LL{labelHeight}" +
                "^LH0,0" +
                $"^FO2,2^BQN,2,7^FDQA,{qrData}^FS" +
                labelText +
                "^PQ1,0,1,N" +
                "^XZ";
        }

        public static string BuildChargeZplBatch(IEnumerable<(string Charge, string Menge, bool Gesperrt)> charges, string material, string eingangsdatum)
        {
            if (charges == null)
            {
                throw new ArgumentNullException(nameof(charges));
            }

            return string.Concat(charges
                .Where(c => !string.IsNullOrWhiteSpace(c.Charge))
                .Select(c => BuildSingleChargeZpl(c.Charge, c.Menge, material, eingangsdatum, c.Gesperrt)));
        }

        private static string SanitizeZpl(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace("^", " ")
                .Replace("~", " ");
        }
    }
}
