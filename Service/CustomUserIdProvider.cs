using Microsoft.AspNetCore.SignalR;

namespace TaskManagerApi.Service
{
    public class CustomUserIdProvider: IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        }
    }
}
