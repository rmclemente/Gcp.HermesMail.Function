# Gcp.HermesMail.Function
This is an simple implementation of a Google Cloud Function to send e-mails, triggered by a Pub/Sub event.
It uses [MailKit](https://github.com/jstedfast/MailKit) project to send the e-mails.

## Setup
You can setup your sensible data using Environment variables when editing your Cloud Function in GCP panel or you can create secrets and reference them in your Cloud Function setup.
The simplest way to get these Environment variables is using the *Environment.GetEnvironmentVariable*:
```csharp
_gcpProjecId = Environment.GetEnvironmentVariable("YOUR GCP PROJECT ID");
_deadLetterTopicId = Environment.GetEnvironmentVariable("YOUR DEAD LETTER TOPIC");
_host = Environment.GetEnvironmentVariable("YOUR E-MAIL SMTP HOST");
_port = int.Parse(Environment.GetEnvironmentVariable("YOUR E-MAIL SMTP HOST PORT"));
_username = Environment.GetEnvironmentVariable("YOUR E-MAIL USERNAME");
_password = Environment.GetEnvironmentVariable("YOUR E-MAIL PASSWORD");
```

## Kinds of Trigger
As said before, this exemple is triggered by a Cloud Pub/Sub event as you can see below:
```csharp
public class Function : ICloudEventFunction<MessagePublishedData>
```

If you want to trigger this same function with a Http request, as it was an api, you can implement the IHttpFunction as below:
```csharp
public class Function : IHttpFunction
```

Then the *HandleAsync* method would look like this:
```csharp
public Task HandleAsync(HttpContext context)
```

As you can see, in a Cloud Function triggered by an Http request (POST), the "event" is our old friend HttpContext and we can access the body data in the same way we are used to.
```csharp
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
```

## Worth to Mention
In this project we have simple examples of how to setup the a startup class to work with Dependency Injection in a Cloud Function project:
```csharp
public class Startup : FunctionsStartup
{
    public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
    {
        services.AddScoped<Services>();
        services.AddSingleton(PublisherServiceApiClient.Create());
    }
}

[FunctionsStartup(typeof(Startup))]
public class Function : ICloudEventFunction<MessagePublishedData>
{
...
```
Validate the cloud event lifetime when the Cloud Function is enabled to retry processing it in case of failure:
```csharp
public static bool IsExpiredEvent(this CloudEvent cloudEvent, int timeInSeconds = 60)
{
    TimeSpan MaxEventAge = TimeSpan.FromSeconds(timeInSeconds);
    DateTimeOffset utcNow = DateTimeOffset.UtcNow;

    // Every PubSub CloudEvent will contain a timestamp.
    DateTimeOffset timestamp = cloudEvent.Time.Value;
    DateTimeOffset expiry = timestamp + MaxEventAge;

    return utcNow > expiry;
}
```
And publish the event to a dead letter pub/sub topic:
```csharp
public async Task HandleAsync(CloudEvent cloudEvent, MessagePublishedData data, CancellationToken cancellationToken)
{
    if (cloudEvent.IsExpiredEvent())
    {
        Console.WriteLine($"--- [{DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}] Publishing Message to Dead Letter Topic. '{data.Message?.TextData}' ---");
        await _services.PublishIntegrationEventToDeadLetterTopic(data.Message?.TextData);
        return;
    }
...
```
```csharp
public async Task PublishIntegrationEventToDeadLetterTopic(string message)
{
    TopicName topicName = TopicName.FromProjectTopic(_gcpProjecId, _deadLetterTopicId);
    var pubsubMessage = new PubsubMessage
    {
        Data = ByteString.CopyFromUtf8(message)
    };

    await _publisher.PublishAsync(topicName, new[] { pubsubMessage });
}
```

> Note that, when using c#, Google recommends that a Cloud Function project should have a main file called *Function.cs* and your Cloud Function Entrypoint will always be *projet namespace* + main class name.
```csharp
namespace HermesMail.Core
```
```csharp
public class Function : ICloudEventFunction<MessagePublishedData>
```
EntryPoint:
```
HermesMail.Core.Function
```
