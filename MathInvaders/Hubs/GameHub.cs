using MathInvaders.Models;
using Microsoft.AspNetCore.SignalR;

namespace MathInvaders.Hubs
{
    public class GameHub : Hub
    {
        public async Task SendGameUpdate(GameStateDto gameState)
        {
            await Clients.All.SendAsync("UpdateGame", gameState);
        }
    }
}