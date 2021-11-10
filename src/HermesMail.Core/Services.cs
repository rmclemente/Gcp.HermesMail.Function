using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using MailKit.Net.Smtp;
using System;
using System.Threading.Tasks;

namespace HermesMail.Core
{
    public class Services
    {
        private readonly PublisherServiceApiClient _publisher;
        private readonly string _gcpProjecId;
        private readonly string _deadLetterTopicId;
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _useSsl;

        public Services(PublisherServiceApiClient publisher)
        {
            _publisher = publisher;
            _gcpProjecId = Environment.GetEnvironmentVariable("YOUR GCP PROJECT ID");
            _deadLetterTopicId = Environment.GetEnvironmentVariable("YOUR DEAD LETTER TOPIC");
            _host = Environment.GetEnvironmentVariable("YOUR E-MAIL SMTP HOST");
            _port = int.Parse(Environment.GetEnvironmentVariable("YOUR E-MAIL SMTP HOST PORT"));
            _username = Environment.GetEnvironmentVariable("YOUR E-MAIL USERNAME");
            _password = Environment.GetEnvironmentVariable("YOUR E-MAIL PASSWORD");
            _useSsl = Environment.GetEnvironmentVariable("USE SSL 'TRUE' OR 'FALSE'").Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Publish cloud event to a dead letter topic when cloud function is enabled to retry event processing in case of failure
        /// the event is expired.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task PublishIntegrationEventToDeadLetterTopic(string message)
        {
            TopicName topicName = TopicName.FromProjectTopic(_gcpProjecId, _deadLetterTopicId);
            var pubsubMessage = new PubsubMessage
            {
                Data = ByteString.CopyFromUtf8(message)
            };

            await _publisher.PublishAsync(topicName, new[] { pubsubMessage });
        }

        public void SendMail(MailMessage mailMessage)
        {
            var message = mailMessage.ToMimeMessage();

            using var client = new SmtpClient();
            client.Connect(_host, _port, _useSsl);
            client.Authenticate(_username, _password);
            client.Send(message);
            client.Disconnect(true);
        }
    }
}
