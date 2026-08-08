using Azure.Messaging.ServiceBus;

// Standalone, transport-level proof that the ASB emulator image
// (mcr.microsoft.com/azure-messaging/servicebus-emulator, same image Aspire's RunAsEmulator()
// uses per ADR-0002) honors ScheduledEnqueueTime — resolves RESEARCH.md Open Question 1.
// No saga, no HTTP, no auth, no AppHost. Well-known default emulator connection string/SAS key
// (local-only, never used against real Azure — see plan 04-02's threat model T-04-07).
const string connectionString =
    "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
const string queueName = "spike-queue";

await using var client = new ServiceBusClient(connectionString);
await using var sender = client.CreateSender(queueName);
await using var receiver = client.CreateReceiver(queueName);

var scheduledTime = DateTimeOffset.UtcNow.AddSeconds(10);
var message = new ServiceBusMessage("spike-scheduled-message")
{
    ScheduledEnqueueTime = scheduledTime
};

await sender.SendMessageAsync(message);

var receivedMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30));

if (receivedMessage is null)
{
    Console.WriteLine("SPIKE-RESULT: FAIL — no message received within 30 seconds; the ASB emulator does not appear to support scheduled delivery.");
    return 1;
}

var receivedAt = DateTimeOffset.UtcNow;
var graceWindow = scheduledTime.AddSeconds(-1);

if (receivedAt < graceWindow)
{
    Console.WriteLine("SPIKE-RESULT: FAIL — message was delivered early (before the scheduled time minus a 1-second clock-skew grace); the emulator ignored ScheduledEnqueueTime.");
    return 1;
}

await receiver.CompleteMessageAsync(receivedMessage);
Console.WriteLine("SPIKE-RESULT: PASS — message was delivered on or after the scheduled time; ASB emulator scheduled delivery is confirmed.");
return 0;
