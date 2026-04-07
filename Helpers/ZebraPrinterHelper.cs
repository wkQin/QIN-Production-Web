#pragma warning disable CA1416
using System;
using System.Drawing.Printing;

namespace QIN_Production_Web.Helpers
{
    public class ZebraPrinterHelper
    {
        private int CalculateAdjustedFontSize(string text, int baseFontSize, int maxLength)
        {
            int excessLength = text.Length - maxLength;
            if (excessLength > 0)
            {
                return Math.Max(baseFontSize - excessLength, 19);
            }
            return baseFontSize;
        }

        public bool PrintSingleChargeQr(string charge, string menge, string material, string eingangsdatum)
        {
            if (string.IsNullOrWhiteSpace(charge))
                return false;

            int dpi = 203;                 
            double labelWidthMm = 55.0;   
            double labelHeightMm = 28.0;   

            string targetPrinter = new PrintDocument().PrinterSettings.PrinterName;
            if (string.IsNullOrWhiteSpace(targetPrinter))
            {
                Console.WriteLine("Fehler: Kein Drucker verfügbar.");
                return false;
            }

            double dpm = dpi / 25.4; 

            int PW = (int)Math.Round(labelWidthMm * dpm);
            int LL = (int)Math.Round(labelHeightMm * dpm);

            // Data format for QR
            string qrData = $"{charge}|{menge}|{material}|{eingangsdatum}";

            string zpl = 
                "^XA" +
                "^CI28" +                 
                $"^PW{PW}" +              
                $"^LL{LL}" +              
                "^LH0,0" +
                $"^FO10,15^BQN,2,4^FDQA,{qrData}^FS" +
                $"^FO210,15^A0N,20,20^FDCharge:^FS" +
                $"^FO210,35^A0N,25,25^FD{charge}^FS" +
                $"^FO210,75^A0N,20,20^FDMenge:^FS" +
                $"^FO210,95^A0N,24,24^FD{menge} LM/STK^FS" +
                $"^FO210,135^A0N,20,20^FDMaterial:^FS" +
                $"^FO210,155^A0N,22,22^FB220,2,,L^FD{material}^FS" +
                $"^FO210,200^A0N,20,20^FDDatum: {eingangsdatum}^FS" +
                $"^PQ1,0,1,N" +
                "^XZ";

            return RawPrinterHelper.SendStringToPrinter(targetPrinter, zpl);
        }

        public bool PrintPalettenChargeQr(string charge, string ebe)
        {
            if (string.IsNullOrWhiteSpace(charge))
                throw new ArgumentException("Charge ist leer.", nameof(charge));

            int dpi = 203;                 
            double labelWidthMm = 55.0;   
            double labelHeightMm = 28.0;   

            double qrXmm = 3.0;            
            double qrYmm = 3.0;            
            int qrMag = 9;              

            double headlineXmm = 29.0;     
            double headlineYmm = 10.0;
            int fontHeadlineDots = 20;  

            double chargeXmm = 29.0;
            double chargeYmm = 13.5;
            int fontChargeDots = 25;    

            double lineXmm = 29.0;
            double lineYmm = 18.0;
            double lineWidthMm = 23.0;     
            int lineThicknessDots = 2;  

            double dateXmm = 29.0;
            double dateYmm = 24.0;
            int fontDateDots = 22;

            int copies = 1;                

            var targetPrinter = new PrintDocument().PrinterSettings.PrinterName;
            if (string.IsNullOrWhiteSpace(targetPrinter))
            {
                Console.WriteLine("Fehler: Kein Drucker verfügbar.");
                return false;
            }

            double dpm = dpi / 25.4; 

            int PW = (int)Math.Round(labelWidthMm * dpm);
            int LL = (int)Math.Round(labelHeightMm * dpm);

            int qrX = (int)Math.Round(qrXmm * dpm);
            int qrY = (int)Math.Round(qrYmm * dpm);

            int headlineX = (int)Math.Round(headlineXmm * dpm);
            int headlineY = (int)Math.Round(headlineYmm * dpm);

            int chargeX = (int)Math.Round(chargeXmm * dpm);
            int chargeY = (int)Math.Round(chargeYmm * dpm);

            int lineX = (int)Math.Round(lineXmm * dpm);
            int lineY = (int)Math.Round(lineYmm * dpm);
            int lineW = (int)Math.Round(lineWidthMm * dpm);

            int dateX = (int)Math.Round(dateXmm * dpm);
            int dateY = (int)Math.Round(dateYmm * dpm);

            string zpl =
                "^XA" +
                "^CI28" +                 
                $"^PW{PW}" +              
                $"^LL{LL}" +              
                "^LH0,0" +
                $"^FO{qrX},{qrY}^BQN,2,{qrMag}^FDQA,{charge}^FS" +
                $"^FO{headlineX},{headlineY}^A0N,{fontHeadlineDots},{fontHeadlineDots}^FD{ebe}^FS" +
                $"^FO{chargeX},{chargeY}^A0N,{fontChargeDots},{fontChargeDots}^FD{charge}^FS" +
                $"^FO{lineX},{lineY}^GB{lineW},{Math.Max(1, lineThicknessDots)},{Math.Max(1, lineThicknessDots)}^FS" +
                $"^FO{dateX},{dateY}^A0N,{fontDateDots},{fontDateDots}^FDDatum: {DateTime.Now:dd.MM.yyyy}^FS" +
                $"^PQ{Math.Max(1, copies)},0,1,N" +
                "^XZ";

            return RawPrinterHelper.SendStringToPrinter(targetPrinter, zpl);
        }
    }
}
