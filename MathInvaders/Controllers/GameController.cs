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
            // Создаём игроков
            var players = new List<Player>
            {
                new Player { Id = 1, Name = "Player1", X = 0, Y = 0 },
                new Player { Id = 2, Name = "Player2", X = 0, Y = size - 1 },
                new Player { Id = 3, Name = "Player3", X = size - 1, Y = 0 },
                new Player { Id = 4, Name = "Player4", X = size - 1, Y = size - 1 }
            };
            _gameState.Players.AddRange(players);

            // Инициализируем поле
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

            // Назначаем стартовые клетки игрокам
            foreach (var player in _gameState.Players)
            {
                var startCell = _gameState.Grid[player.X, player.Y];
                startCell.OwnerId = player.Id;
                player.CapturedCells++; // Увеличиваем счётчик захваченных клеток
            }

            _gameState.CurrentPlayerIndex = 0;
            _gameState.GameOver = false;
            _gameState.Winner = null;
        }
    }
}
