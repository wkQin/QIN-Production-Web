using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace QIN_Production_Web.Helpers
{
    public static class EmailHelper
    {
        public static async Task<bool> SendQSEmailAsync(string lsnr, string ebe, string lieferant, string material, string bemerkung, string username, string recipientEmails, string zustand)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("QIN-Production-Tool", "pod-tool@qin-form.local"));
                
                var recipients = recipientEmails.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var recipient in recipients)
                {
                    message.To.Add(new MailboxAddress("", recipient.Trim()));
                }
                
                bool isBad = zustand.Equals("Schlecht", StringComparison.OrdinalIgnoreCase);
                string subjectPrefix = isBad ? "WICHTIG: " : "Info: ";
                string titleColor = isBad ? "#d9534f" : "#2ca02c";
                string titleText = isBad ? "Wareneingang mit mangelhaftem Zustand" : "Neuer Wareneingang erfasst";
                string alertHtml = isBad ? "<p style='font-size: 15px; color: #d9534f; font-weight: bold;'>⚠️ Bitte prüfen Sie diesen Vorgang zeitnah, da der Zustand als 'Schlecht' markiert wurde.</p>" : "";

                message.Subject = $"{subjectPrefix}Wareneingang erfasst - Zustand: {zustand} (EBE: {ebe})";

                string htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333;'>
                    <h2 style='color: {titleColor}; border-bottom: 2px solid {titleColor}; padding-bottom: 5px;'>{titleText}</h2>
                    <p style='font-size: 14px;'>Hallo QS-Team,</p>
                    <p style='font-size: 14px;'>im Wareneingang wurde soeben ein neuer Eintrag mit dem Zustand <strong>{zustand}</strong> erfasst.</p>
                    {alertHtml}
                    
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold; width: 130px;'>EBE-Nummer:</td><td style='padding: 8px; border: 1px solid #ddd;'>{ebe}</td></tr>
                        <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Lieferschein:</td><td style='padding: 8px; border: 1px solid #ddd;'>{lsnr}</td></tr>
                        <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Lieferant:</td><td style='padding: 8px; border: 1px solid #ddd;'>{lieferant}</td></tr>
                        <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Material/Artikel:</td><td style='padding: 8px; border: 1px solid #ddd;'>{material}</td></tr>
                        <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold; color: {titleColor};'>Zustand:</td><td style='padding: 8px; border: 1px solid #ddd; color: {titleColor}; font-weight: bold;'>{zustand}</td></tr>
                        <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Bemerkung:</td><td style='padding: 8px; border: 1px solid #ddd;'>{bemerkung}</td></tr>
                        <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Erfasst von:</td><td style='padding: 8px; border: 1px solid #ddd;'>{username}</td></tr>
                    </table>

                    <p style='font-size: 12px; color: #777; margin-top: 30px;'>Dies ist eine systemgenerierte Benachrichtigung aus dem QIN-Production Web-Tool. Bitte antworten Sie nicht auf diese E-Mail.</p>
                </div>";

                message.Body = new TextPart("html")
                {
                    Text = htmlBody
                };

                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    await client.ConnectAsync("192.168.253.73", 587, SecureSocketOptions.StartTlsWhenAvailable);

                    try
                    {
                        await client.AuthenticateAsync("klein", "4Shizzle#");
                    }
                    catch (AuthenticationException)
                    {
                        await client.AuthenticateAsync("klein", "4shizzle#");
                    }

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Senden der QS Email via MailKit: {ex.Message}");
                throw; // Rethrow to allow UI to catch and alert
            }
        }
    }
}
