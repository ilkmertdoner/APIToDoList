using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace TaskManagerApi.Service
{
    public class MicrosoftCalendarService
    {
        private GraphServiceClient GetGraphClient()
        {
            var json = File.ReadAllText("microsoft-credentials.json");
            var config = JsonSerializer.Deserialize<MicrosoftCredentials>(json);

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var clientSecretCredential = new ClientSecretCredential(
                config.TenantId, config.ClientId, config.ClientSecret, options);

            return new GraphServiceClient(clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });
        }

        public async Task<string> AddTaskToUserCalendarAsync(string userEmail, string title, string description, 
            DateTime dueDate)
        {
            var graphClient = GetGraphClient();

            string startStr = dueDate.ToString("yyyy-MM-ddTHH:mm:ss");
            string endStr = dueDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss");

            var requestBody = new Event
            {
                Subject = title,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = description
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = startStr,
                    TimeZone = "Europe/Istanbul"
                },
                End = new DateTimeTimeZone
                {
                    DateTime = endStr,
                    TimeZone = "Europe/Istanbul"
                }
            };

            var createdEvent = await graphClient.Users[userEmail].Calendar.Events.PostAsync(requestBody);
            return createdEvent.Id;
        }

        public async Task UpdateTaskInUserCalendarAsync(string userEmail, string eventId, string title, 
            string description, DateTime dueDate)
        {
            var graphClient = GetGraphClient();

            string startStr = dueDate.ToString("yyyy-MM-ddTHH:mm:ss");
            string endStr = dueDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss");

            var requestBody = new Event
            {
                Subject = title,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = description
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = startStr,
                    TimeZone = "Europe/Istanbul"
                },
                End = new DateTimeTimeZone
                {
                    DateTime = endStr,
                    TimeZone = "Europe/Istanbul"
                }
            };

            await graphClient.Users[userEmail].Events[eventId].PatchAsync(requestBody);
        }

        public async Task DeleteTaskFromUserCalendarAsync(string userEmail, string eventId)
        {
            var graphClient = GetGraphClient();
            await graphClient.Users[userEmail].Events[eventId].DeleteAsync();
        }
    }

    public class MicrosoftCredentials
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}