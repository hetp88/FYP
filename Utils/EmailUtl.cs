using System.Net.Mail;

namespace RP.SOI.DotNet.Utils
{

    public static class EmailUtl
    {

        static readonly string? env =
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        private static readonly IConfiguration config =
           new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json")
              .AddJsonFile($"appsettings.{env}.json", optional: true)
              .Build()
              .GetSection("EmailSettings");

        private static readonly string EMAIL_ID = config.GetValue<String>("OutlookID");
        private static readonly string EMAIL_PW = config.GetValue<String>("OutlookPW");

        // Using Microsoft's OUTLOOK
        private static readonly string HOST = "smtp-mail.outlook.com";  // Tested OK Jan '23 PK
        private static readonly int PORT = 587;                         // Tested OK Jan '23 PK

        public static bool SendEmail(string recipient,
                                     string subject, string msg,
                                 out string error)
        {
            SmtpClient client = new(HOST, PORT);
            client.EnableSsl = true;
            client.Timeout = 20000;
            client.Credentials = new System.Net.NetworkCredential(EMAIL_ID, EMAIL_PW);

            MailMessage mm = new(EMAIL_ID, recipient, subject, msg);
            mm.IsBodyHtml = true;
            bool success = true;
            error = "";
            try
            {
                client.Send(mm);
            }
            catch (Exception e)
            {
                error = e.Message;
                success = false;
            }
            return success;
        }

    }
}
