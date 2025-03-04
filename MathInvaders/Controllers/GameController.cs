using Microsoft.AspNetCore.Mvc;
using MathInvaders.Models;
using static MathInvaders.Models.GameRequest;

namespace MathInvaders.Controllers
{
    public class GameController : Controller
    {
        private static GameState _gameState = new GameState();
        private static Random _random = new Random();

        public IActionResult Index()
        {
            if (_gameState.Players.Count == 0)
            {
                InitializeGame(5);
            }
            _gameState.ActivePlayerId = _gameState.Players[_gameState.CurrentPlayerIndex].Id; // Устанавливаем активного игрока
            return View(_gameState);
        }

        [HttpPost]
        public IActionResult Move([FromBody] GameMoveRequest request)
        {
            if (_gameState.GameOver || _gameState.ShowTaskInput)
            {
                return Json(new { success = false, message = "Игра окончена или сейчас не время ходить!" });
            }

            var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
            if (currentPlayer.Id != request.PlayerId)
            {
                return Json(new { success = false, message = "Сейчас не ваш ход!" });
            }

            if (!_gameState.CanMove(currentPlayer, request.Direction))
            {
                return Json(new { success = false, message = "Нельзя туда пойти!" });
            }

            int oldX = currentPlayer.X, oldY = currentPlayer.Y;
            switch (request.Direction.ToLower())
            {
                case "up": currentPlayer.Y--; break;
                case "down": currentPlayer.Y++; break;
                case "left": currentPlayer.X--; break;
                case "right": currentPlayer.X++; break;
            }
            _gameState.LastMovedCell = (currentPlayer.X, currentPlayer.Y);
            var cell = _gameState.Grid[currentPlayer.X, currentPlayer.Y];
            return Json(new { success = true, cost = cell.Cost });
        }

        [HttpPost]
        public IActionResult SpendCoins([FromBody] GameSpendRequest request)
        {
            if (_gameState.GameOver || _gameState.ShowTaskInput)
            {
                return Json(new { success = false, message = "Игра окончена или сейчас не время!" });
            }

            var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
            if (currentPlayer.Id != request.PlayerId)
            {
                return Json(new { success = false, message = "Сейчас не ваш ход!" });
            }

            var cell = _gameState.Grid[currentPlayer.X, currentPlayer.Y];
            if (cell.OwnerId.HasValue)
            {
                return Json(new { success = false, message = "Эта клетка уже принадлежит игроку!" });
            }

            if (request.Spend)
            {
                if (currentPlayer.Coins >= cell.Cost)
                {
                    currentPlayer.Coins -= cell.Cost;
                    cell.IsRevealed = true;
                    _gameState.ShowTaskInput = true;
                    _gameState.LastMovedCell = null;
                    return Json(new { success = true, task = cell.Task });
                }
                else
                {
                    return Json(new { success = false, message = "Недостаточно монет!" });
                }
            }
            else
            {
                _gameState.ShowTaskInput = false;
                _gameState.CurrentPlayerIndex = (_gameState.CurrentPlayerIndex + 1) % _gameState.Players.Count;
                _gameState.ActivePlayerId = _gameState.Players[_gameState.CurrentPlayerIndex].Id;
                return Json(new { success = true });
            }
        }

        [HttpPost]
        public IActionResult SubmitAnswer([FromBody] GameAnswerRequest request)
        {
            if (_gameState.GameOver || !_gameState.ShowTaskInput)
            {
                return Json(new { success = false, message = "Игра окончена или сейчас не время!" });
            }

            var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
            if (currentPlayer.Id != request.PlayerId)
            {
                return Json(new { success = false, message = "Сейчас не ваш ход!" });
            }

            var cell = _gameState.Grid[currentPlayer.X, currentPlayer.Y];
            if (request.Answer == cell.Answer)
            {
                cell.OwnerId = currentPlayer.Id;
                currentPlayer.CapturedCells++;
                _gameState.LastMovedCell = (currentPlayer.X, currentPlayer.Y);
            }
            else
            {
                return Json(new { success = false, message = "Неверный ответ!" });
            }

            _gameState.ShowTaskInput = false;
            _gameState.CurrentPlayerIndex = (_gameState.CurrentPlayerIndex + 1) % _gameState.Players.Count;
            _gameState.ActivePlayerId = _gameState.Players[_gameState.CurrentPlayerIndex].Id;
            _gameState.CheckGameOver();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Reset()
        {
            _gameState = new GameState();
            InitializeGame(5);
            return RedirectToAction("Index");
        }

        private void InitializeGame(int size)
        {
            var players = new List<Player>
            {
                new Player { Id = 1, Name = "Player1", X = 0, Y = 0 },
                new Player { Id = 2, Name = "Player2", X = 0, Y = size - 1 },
                new Player { Id = 3, Name = "Player3", X = size - 1, Y = 0 },
                new Player { Id = 4, Name = "Player4", X = size - 1, Y = size - 1 }
            };
            _gameState.Players.AddRange(players);

            _gameState.Grid = new Cell[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int difficulty = _random.Next(1, 4);
                    _gameState.Grid[x, y] = new Cell
                    {
                        X = x,
                        Y = y,
                        Task = $"{x} + {y} = ?",
                        Answer = x + y,
                        Difficulty = difficulty,
                        Cost = difficulty
                    };
                }
            }

            foreach (var player in _gameState.Players)
            {
                var startCell = _gameState.Grid[player.X, player.Y];
                startCell.OwnerId = player.Id;
                startCell.IsRevealed = true;
                player.CapturedCells++;
            }

            _gameState.CurrentPlayerIndex = 0;
            _gameState.ActivePlayerId = _gameState.Players[0].Id; // Начинаем с "Player1"
            _gameState.GameOver = false;
            _gameState.Winner = null;
            _gameState.ShowTaskInput = false;
            _gameState.LastMovedCell = null;
        }
    }
}