using MathInvaders.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace MathInvaders.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameService _gameService;

        public GameHub(GameService gameService)
        {
            _gameService = gameService;
        }

        public async Task JoinGame(string matchId, string playerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
            Console.WriteLine($"JoinGame: Player {playerId} joined group {matchId}. ConnectionId: {Context.ConnectionId}");
            await Clients.Group(matchId).SendAsync("PlayerJoined", playerId);
        }

        public async Task PlayerReady(string matchId, string playerId, bool isReady)
        {
            if (_gameService.Games.TryGetValue(Convert.ToInt32(matchId), out var gameState))
            {
                var player = gameState.Players.FirstOrDefault(p => p.Id == playerId);
                if (player != null)
                {
                    player.IsReady = isReady;
                    Console.WriteLine($"PlayerReady: Player {playerId} set to {isReady} in match {matchId}");
                    await Clients.Group(matchId).SendAsync("PlayerReadyStatus", playerId, isReady);
                }
                else
                {
                    Console.WriteLine($"PlayerReady: Player {playerId} not found in match {matchId}");
                }
            }
            else
            {
                Console.WriteLine($"PlayerReady: Match {matchId} not found");
            }
        }

        public async Task LeaveGame(string matchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, matchId);
        }
    }
}