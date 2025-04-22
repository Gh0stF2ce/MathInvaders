using MathInvaders.Models;

namespace MathInvaders.Services
{
    public class GameService
    {
        private readonly Dictionary<int, GameState> _games;

        public GameService()
        {
            _games = new Dictionary<int, GameState>();
        }

        public Dictionary<int, GameState> Games => _games;
    }
}
