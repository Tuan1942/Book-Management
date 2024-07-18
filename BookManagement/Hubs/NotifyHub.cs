using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BookManagement.Hubs
{
    public class NotifyHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var bookId = httpContext.Request.Query["bookId"].ToString();

            if (!string.IsNullOrEmpty(bookId))
            {
                // Add connection to the group based on the bookId
                await Groups.AddToGroupAsync(Context.ConnectionId, bookId);
                // Optionally send a message back to the client
                await Clients.Caller.SendAsync("ReceiveMessage", $"Connected to book {bookId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            var bookId = httpContext.Request.Query["bookId"].ToString();

            if (!string.IsNullOrEmpty(bookId))
            {
                // Remove connection from the group based on the bookId
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, bookId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Static method to notify all clients in a specific book group
        public static Task NotifyBookUpdate(string bookId, IHubContext<NotifyHub> hubContext)
        {
            return hubContext.Clients.Group(bookId).SendAsync("ReceiveMessage", "Updated");
        }
    }
}
