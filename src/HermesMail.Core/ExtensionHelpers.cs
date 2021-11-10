using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Http;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HermesMail.Core
{
    public static class ExtensionHelpers
    {
        /// <summary>
        /// Check if an event is older than 60s.
        /// </summary>
        /// <param name="cloudEvent"></param>
        /// <returns></returns>
        public static bool IsExpiredEvent(this CloudEvent cloudEvent, int timeInSeconds = 60)
        {
            TimeSpan MaxEventAge = TimeSpan.FromSeconds(timeInSeconds);
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;

            // Every PubSub CloudEvent will contain a timestamp.
            DateTimeOffset timestamp = cloudEvent.Time.Value;
            DateTimeOffset expiry = timestamp + MaxEventAge;

            return utcNow > expiry;
        }

        public static MailMessage ToMailMessage(this HttpContext context)
        {
            var readToEndTask = new StreamReader(context.Request.Body).ReadToEndAsync();
            var body = readToEndTask.Result;

            if (string.IsNullOrWhiteSpace(body))
                return null;

            return JsonSerializer.Deserialize<MailMessage>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public static MailMessage ToMailMessage(this string textData)
        {
            if (string.IsNullOrWhiteSpace(textData))
                return null;

            return JsonSerializer.Deserialize<MailMessage>(textData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public static MimeMessage ToMimeMessage(this MailMessage message, string subType = "mixed")
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(message.From.Name, message.From.MailAddress));
            mimeMessage.Subject = message.Subject;
            message.To.ForEach(p => mimeMessage.To.Add(new MailboxAddress(p.Name, p.MailAddress)));
            message.Cc?.ForEach(p => mimeMessage.Cc.Add(new MailboxAddress(p.Name, p.MailAddress)));
            message.Bcc?.ForEach(p => mimeMessage.Bcc.Add(new MailboxAddress(p.Name, p.MailAddress)));
            mimeMessage.Body = new Multipart(subType).WithBody(message.Body).AddAttachments(message.Attachments);
            return mimeMessage;
        }

        public static Multipart WithBody(this Multipart multipart, MailBody mailBody)
        {
            var body = string.Equals(mailBody.BodyType, MailBody.BodyTypeHtml, StringComparison.OrdinalIgnoreCase) ? new TextPart("html") { Text = mailBody.BodyText } : new TextPart("plain") { Text = mailBody.BodyText };
            multipart.Add(body);
            return multipart;
        }

        public static Multipart AddAttachments(this Multipart multipart, List<MailAttachment> attachments)
        {
            foreach (var file in attachments)
            {
                multipart.Add(new MimePart(file.MediaType, file.MediaSubType)
                {
                    Content = new MimeContent(new MemoryStream(System.Text.Encoding.ASCII.GetBytes(file.FileContent)), ContentEncoding.Base64),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = file.FileName
                });
            }

            return multipart;
        }
    }
}
