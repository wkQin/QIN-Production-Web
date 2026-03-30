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

        public bool PrintChargeAndDate(string KundenName, string GCharge, string KCharge, string eingangsDatum)
        {
            string FEingangsDatum = string.IsNullOrEmpty(eingangsDatum) ? DateTime.Now.ToString("dd.MM.yyyy") : eingangsDatum;
            string FProduktionDatum = DateTime.Now.ToString("dd.MM.yyyy");

            int baseFontSize = 36;
            int maxLength = 10;
            int chargeNameFontSize = CalculateAdjustedFontSize(KundenName, baseFontSize, maxLength);
            int chargeIDFontSize = CalculateAdjustedFontSize(GCharge, baseFontSize, maxLength);

            string zplCommand = $"^XA" +
                                $"^FO20,60^A0N,{chargeNameFontSize},{chargeNameFontSize}^FB245,2,,L^FD{KundenName}^FS" +
                                $"^FO250,60^A0N,{chargeIDFontSize},{chargeIDFontSize}^FD{GCharge}^FS" +
                                $"^FO0,12^A0N,95,1005^FD_________________________________________^FS" +
                                $"^FO30,120^A0N,30,30^FDFolieneingang^FS" +
                                $"^FO50,150^A0N,30,30^FD{FEingangsDatum}^FS" +
                                $"^FO255,120^A0N,30,30^FDFolienproduktion^FS" +
                                $"^FO285,150^A0N,30,30^FD{FProduktionDatum}^FS" +
                                $"^FO100,200^A0N,30,40^FD{KCharge}^FS" +
                                $"^XZ";

            string defaultPrinterName = new PrintDocument().PrinterSettings.PrinterName;
            if (string.IsNullOrEmpty(defaultPrinterName))
            {
                Console.WriteLine("Fehler: Kein Standarddrucker gefunden.");
                return false;
            }

            return RawPrinterHelper.SendStringToPrinter(defaultPrinterName, zplCommand);
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
