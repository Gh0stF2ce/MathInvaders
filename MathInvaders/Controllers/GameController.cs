using MathInvaders.Models;
using Microsoft.AspNetCore.Mvc;

namespace MathInvaders.Controllers
{
    public class GameController : Controller
    {
        private static GameState _gameState = new GameState();

        public IActionResult Index()
        {
            if (_gameState.Players.Count == 0)
            {
                InitializeGame(5); // Инициализация поля 5x5
            }
            return View(_gameState);
        }

        private void InitializeGame(int size)
        {
            _gameState.Players.Add(new Player { Name = "Player1", X = 0, Y = 0 });
            _gameState.Players.Add(new Player { Name = "Player2", X = 0, Y = size - 1 });
            _gameState.Players.Add(new Player { Name = "Player3", X = size - 1, Y = 0 });
            _gameState.Players.Add(new Player { Name = "Player4", X = size - 1, Y = size - 1 });

            _gameState.Grid = new Cell[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    _gameState.Grid[x, y] = new Cell
                    {
                        X = x,
                        Y = y,
                        Task = $"{x} + {y} = ?",
                        Answer = x + y
                    };
                }
            }
        }
    }
}
