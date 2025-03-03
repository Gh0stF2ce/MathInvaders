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

        //private void InitializeGame(int size)
        //{
        //    _gameState.Players.Add(new Player { Name = "Player1", X = 0, Y = 0 });
        //    _gameState.Players.Add(new Player { Name = "Player2", X = 0, Y = size - 1 });
        //    _gameState.Players.Add(new Player { Name = "Player3", X = size - 1, Y = 0 });
        //    _gameState.Players.Add(new Player { Name = "Player4", X = size - 1, Y = size - 1 });

        //    _gameState.Grid = new Cell[size, size];
        //    for (int x = 0; x < size; x++)
        //    {
        //        for (int y = 0; y < size; y++)
        //        {
        //            _gameState.Grid[x, y] = new Cell
        //            {
        //                X = x,
        //                Y = y,
        //                Task = $"{x} + {y} = ?",
        //                Answer = x + y
        //            };
        //        }
        //    }
        //}
        [HttpPost]
        public IActionResult Move(int playerId, string direction, int answer)
        {
            if (_gameState.GameOver)
            {
                return RedirectToAction("Index");
            }

            var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
            if (currentPlayer.Id != playerId) // Проверка очереди
            {
                TempData["Message"] = "Сейчас не ваш ход!";
                return RedirectToAction("Index");
            }

            if (!_gameState.CanMove(currentPlayer, direction))
            {
                TempData["Message"] = "Нельзя туда пойти!";
                return RedirectToAction("Index");
            }

            // Перемещение
            switch (direction.ToLower())
            {
                case "up": currentPlayer.Y--; break;
                case "down": currentPlayer.Y++; break;
                case "left": currentPlayer.X--; break;
                case "right": currentPlayer.X++; break;
            }

            // Захват клетки
            var cell = _gameState.Grid[currentPlayer.X, currentPlayer.Y];
            if (!cell.OwnerId.HasValue && currentPlayer.Coins > 0 && answer == cell.Answer)
            {
                cell.OwnerId = currentPlayer.Id;
                currentPlayer.Coins--;
                currentPlayer.CapturedCells++;
            }
            else if (answer != cell.Answer)
            {
                TempData["Message"] = "Неверный ответ!";
            }

            // Переход очереди
            _gameState.CurrentPlayerIndex = (_gameState.CurrentPlayerIndex + 1) % _gameState.Players.Count;

            // Проверка конца игры
            _gameState.CheckGameOver();

            return RedirectToAction("Index");
        }

        private void InitializeGame(int size)
        {
            _gameState.Players.Add(new Player { Id = 1, Name = "Player1", X = 0, Y = 0 });
            _gameState.Players.Add(new Player { Id = 2, Name = "Player2", X = 0, Y = size - 1 });
            _gameState.Players.Add(new Player { Id = 3, Name = "Player3", X = size - 1, Y = 0 });
            _gameState.Players.Add(new Player { Id = 4, Name = "Player4", X = size - 1, Y = size - 1 });

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
            _gameState.CurrentPlayerIndex = 0;
            _gameState.GameOver = false;
            _gameState.Winner = null;
        }
    }
}
