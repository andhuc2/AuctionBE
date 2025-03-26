using API.Models;
using API.Models.Context;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Utils
{
    public class ItemHub : Hub
    {
        private readonly DBContext _context;

        public ItemHub(DBContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        public async Task ItemUpdate(string id, string message)
        {
            await Clients.Group($"item-{id}").SendAsync("ItemUpdate", message);
        }

        public async Task ListenItem(string id)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"item-{id}");
        }
    }
}
