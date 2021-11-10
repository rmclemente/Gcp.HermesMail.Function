using System.Collections.Generic;
using System.Linq;

namespace HermesMail.Core
{
    public class MailMessage
    {
        public string Subject { get; set; }
        public MailContact From { get; set; }
        public List<MailContact> To { get; set; }
        public List<MailContact> Cc { get; set; }
        public List<MailContact> Bcc { get; set; }
        public MailBody Body { get; set; }
        public List<MailAttachment> Attachments { get; set; }
    }

    public class MailBody
    {
        public const string BodyTypePlainText = "plain";
        public const string BodyTypeHtml = "html";
        public string BodyText { get; set; }
        public string BodyType { get; set; }
    }

    public class MailAttachment
    {
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public string FileContent { get; set; }
        public string MediaType => MimeType?.Split('/').First();
        public string MediaSubType => MimeType?.Split('/').Last();
    }

    public class MailContact
    {
        public string MailAddress { get; set; }
        public string Name { get; set; }
    }
}
