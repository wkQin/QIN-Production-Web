using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace QIN_Production_Web.Helpers
{
    public static class EmailHelper
    {
        public static async Task<bool> SendQSEmailAsync(string lsnr, string ebe, string lieferant, string material, string bemerkung, string username, string recipientEmails)
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
                
                message.Subject = $"WICHTIG: Schlechter Zustand bei Wareneingang (EBE: {ebe})";

                message.Body = new TextPart("plain")
                {
                    Text =
                           $"------------------------------------------------------\n\n" +
                           $"Es wurde ein Wareneingang mit schlechtem Zustand erfasst:\n" +
                           $"- EBE: {ebe}\n" +
                           $"- Lieferschein: {lsnr}\n" +
                           $"- Lieferant: {lieferant}\n" +
                           $"- Material: {material}\n" +
                           $"- Bemerkung: {bemerkung}\n" +
                           $"- Bearbeitet von: {username}\n\n" +
                           $"Bitte prüfen.\n\nDies ist eine automatisch generierte Nachricht."
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
                return false;
            }
        }
    }
}
