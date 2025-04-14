using Microsoft.AspNetCore.Mvc;
using MathInvaders.Models;
using static MathInvaders.Models.GameRequest;
using System.Text.Json;

namespace MathInvaders.Controllers
{
    public class GameController : Controller
    {
        private static GameState _gameState = new GameState();
        private static Random _random = new Random();
        private static HardTask[] _hardTasks;
        private readonly IWebHostEnvironment _env;

        public GameController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            if (_gameState.Players.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }
            _gameState.ActivePlayerId = _gameState.Players[_gameState.CurrentPlayerIndex].Id;
            return View(new GameStateDto(_gameState));
        }

        [HttpPost]
        public IActionResult StartGame(int classLevel)
        {
            if (classLevel < 5 || classLevel > 7)
            {
                return RedirectToAction("Index", "Home");
            }

            _gameState = new GameState { ClassLevel = classLevel };
            LoadHardTasks(classLevel);
            InitializeGame(5);
            return RedirectToAction("Index");
        }

        private void LoadHardTasks(int classLevel)
        {
            string fileName = $"hard_tasks_{classLevel}.json";
            string filePath = Path.Combine(_env.WebRootPath, "data", fileName);
            string jsonString = System.IO.File.ReadAllText(filePath);
            _hardTasks = JsonSerializer.Deserialize<HardTask[]>(jsonString);
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

            if (!_gameState.CanMove(currentPlayer, request.NewX, request.NewY))
            {
                return Json(new { success = false, message = "Нельзя туда пойти!" });
            }

            _gameState.LastMovedCell = (currentPlayer.X, currentPlayer.Y);
            currentPlayer.X = request.NewX;
            currentPlayer.Y = request.NewY;

            var cell = _gameState.Grid[currentPlayer.X, currentPlayer.Y];
            if (cell.OwnerId.HasValue && cell.OwnerId != currentPlayer.Id)
            {
                int doubleCost = cell.OriginalCost * 2;
                return Json(new { success = true, isOccupied = true, doubleCost = doubleCost, originalCost = cell.OriginalCost });
            }
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
            if (!cell.OwnerId.HasValue && request.Spend)
            {
                if (currentPlayer.Coins >= cell.Cost)
                {
                    currentPlayer.Coins -= cell.Cost;
                    _gameState.CurrentAttemptCost = cell.Cost;
                    cell.IsRevealed = true;
                    _gameState.ShowTaskInput = true;
                    _gameState.TimerActive = true;
                    return Json(new { success = true, task = cell.Task, timeLimit = 30 });
                }
                return Json(new { success = false, message = "Недостаточно монет!" });
            }
            else
            {
                _gameState.ShowTaskInput = false;
                _gameState.TimerActive = false;
                _gameState.CurrentPlayerIndex = (_gameState.CurrentPlayerIndex + 1) % _gameState.Players.Count;
                _gameState.ActivePlayerId = _gameState.Players[_gameState.CurrentPlayerIndex].Id;
                return Json(new { success = true });
            }
        }

        [HttpPost]
        public IActionResult CaptureCell([FromBody] GameCaptureRequest request)
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
            if (!cell.OwnerId.HasValue || cell.OwnerId == currentPlayer.Id)
            {
                return Json(new { success = false, message = "Эта клетка не принадлежит другому игроку!" });
            }

            int cost = request.UseOriginalTask ? cell.OriginalCost * 2 : cell.OriginalCost;
            if (currentPlayer.Coins < cost)
            {
                return Json(new { success = false, message = "Недостаточно монет!" });
            }

            currentPlayer.Coins -= cost;
            _gameState.CurrentAttemptCost = cost;
            cell.IsRevealed = true;
            _gameState.ShowTaskInput = true;
            _gameState.TimerActive = true;

            if (!request.UseOriginalTask)
            {
                var (newTask, newAnswer) = GenerateTask(cell.Difficulty + 1);
                cell.Task = newTask;
                cell.Answer = newAnswer;
            }

            return Json(new { success = true, task = cell.Task, timeLimit = 30 });
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
            bool isCorrect = request.Answer == cell.Answer;

            if (isCorrect)
            {
                cell.OwnerId = currentPlayer.Id;
                currentPlayer.CapturedCells++;
                _gameState.LastMovedCell = (currentPlayer.X, currentPlayer.Y);
            }
            else
            {
                currentPlayer.Coins += _gameState.CurrentAttemptCost;
                currentPlayer.X = _gameState.LastMovedCell.Value.X;
                currentPlayer.Y = _gameState.LastMovedCell.Value.Y;
                cell.IsRevealed = false;
                var (newTask, newAnswer) = GenerateTask(cell.Difficulty);
                cell.Task = newTask;
                cell.Answer = newAnswer;
                _gameState.LastMovedCell = null;
            }

            _gameState.ShowTaskInput = false;
            _gameState.TimerActive = false;
            _gameState.CurrentAttemptCost = 0;
            _gameState.CurrentPlayerIndex = (_gameState.CurrentPlayerIndex + 1) % _gameState.Players.Count;
            _gameState.ActivePlayerId = _gameState.Players[_gameState.CurrentPlayerIndex].Id;
            _gameState.CheckGameOver();

            return Json(new { success = true, wasCorrect = isCorrect });
        }

        [HttpPost]
        public IActionResult Timeout([FromBody] GameSpendRequest request)
        {
            if (_gameState.GameOver || !_gameState.ShowTaskInput || !_gameState.TimerActive)
            {
                return Json(new { success = false, message = "Таймер не активен!" });
            }

            var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
            if (currentPlayer.Id != request.PlayerId)
            {
                return Json(new { success = false, message = "Сейчас не ваш ход!" });
            }

            var cell = _gameState.Grid[currentPlayer.X, currentPlayer.Y];
            currentPlayer.Coins += _gameState.CurrentAttemptCost;
            currentPlayer.X = _gameState.LastMovedCell.Value.X;
            currentPlayer.Y = _gameState.LastMovedCell.Value.Y;
            cell.IsRevealed = false;
            var (newTask, newAnswer) = GenerateTask(cell.Difficulty);
            cell.Task = newTask;
            cell.Answer = newAnswer;
            _gameState.LastMovedCell = null;

            _gameState.ShowTaskInput = false;
            _gameState.TimerActive = false;
            _gameState.CurrentAttemptCost = 0;
            _gameState.CurrentPlayerIndex = (_gameState.CurrentPlayerIndex + 1) % _gameState.Players.Count;
            _gameState.ActivePlayerId = _gameState.Players[_gameState.CurrentPlayerIndex].Id;

            return Json(new { success = true, message = "Время вышло!" });
        }

        [HttpPost]
        public IActionResult Reset()
        {
            _gameState = new GameState();
            return RedirectToAction("Index", "Home");
        }

        private void InitializeGame(int size)
        {
            var players = new List<Player>
            {
                new Player { Id = 1, Name = "Player1", X = 0, Y = 0, Coins = 10 },
                new Player { Id = 2, Name = "Player2", X = 0, Y = size - 1, Coins = 10 },
                new Player { Id = 3, Name = "Player3", X = size - 1, Y = 0, Coins = 10 },
                new Player { Id = 4, Name = "Player4", X = size - 1, Y = size - 1, Coins = 10 }
            };
            _gameState.Players.Clear();
            _gameState.Players.AddRange(players);

            _gameState.Grid = new Cell[size, size];
            _gameState.UsedHardTaskIndices.Clear();
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int difficulty = _random.Next(1, 4);
                    var (task, answer) = GenerateTask(difficulty);
                    _gameState.Grid[x, y] = new Cell
                    {
                        X = x,
                        Y = y,
                        Task = task,
                        Answer = answer,
                        Difficulty = difficulty,
                        Cost = difficulty,
                        OriginalCost = difficulty
                    };
                }
            }

            foreach (var player in _gameState.Players)
            {
                var startCell = _gameState.Grid[player.X, player.Y];
                startCell.OwnerId = player.Id;
                startCell.IsRevealed = true;
                player.CapturedCells = 1;
            }

            _gameState.CurrentPlayerIndex = 0;
            _gameState.ActivePlayerId = _gameState.Players[0].Id;
            _gameState.GameOver = false;
            _gameState.Winner = null;
            _gameState.ShowTaskInput = false;
            _gameState.LastMovedCell = null;
            _gameState.CurrentAttemptCost = 0;
        }

        private (string task, int answer) GenerateTask(int difficulty)
        {
            if (difficulty == 3)
            {
                var availableIndices = Enumerable.Range(0, _hardTasks.Length)
                    .Where(i => !_gameState.UsedHardTaskIndices.Contains(i))
                    .ToList();

                if (!availableIndices.Any())
                {
                    _gameState.UsedHardTaskIndices.Clear();
                    availableIndices = Enumerable.Range(0, _hardTasks.Length).ToList();
                }

                int index = availableIndices[_random.Next(availableIndices.Count)];
                _gameState.UsedHardTaskIndices.Add(index);
                return (_hardTasks[index].Task, _hardTasks[index].Answer);
            }
            else
            {
                switch (_gameState.ClassLevel)
                {
                    case 5:
                        int a5 = _random.Next(1, 20);
                        int b5 = _random.Next(1, 10);
                        switch (_random.Next(0, 4))
                        {
                            case 0: return ($"{a5} + {b5} = ?", a5 + b5);
                            case 1: return ($"{a5} - {b5} = ?", a5 - b5);
                            case 2: return ($"{a5} * {b5} = ?", a5 * b5);
                            case 3:
                                int product5 = a5 * b5;
                                return ($"{product5} / {b5} = ?", a5);
                            default: return ($"{a5} + {b5} = ?", a5 + b5);
                        }

                    case 6:
                        int a6 = _random.Next(10, 50);
                        int b6 = _random.Next(5, 20);
                        switch (_random.Next(0, 4))
                        {
                            case 0: return ($"{a6} + {b6} = ?", a6 + b6);
                            case 1: return ($"{a6} - {b6} = ?", a6 - b6);
                            case 2: return ($"{a6} * {b6} = ?", a6 * b6);
                            case 3:
                                int c6 = _random.Next(2, 10);
                                return ($"{a6} * {c6} / {c6} = ?", a6);
                            default: return ($"{a6} + {b6} = ?", a6 + b6);
                        }

                    case 7:
                        int a7 = _random.Next(20, 100);
                        int b7 = _random.Next(10, 50);
                        int c7 = _random.Next(2, 10);
                        switch (_random.Next(0, 4))
                        {
                            case 0: return ($"{a7} + {b7} - {c7} = ?", a7 + b7 - c7);
                            case 1: return ($"{a7} - {b7} + {c7} = ?", a7 - b7 + c7);
                            case 2: return ($"{a7} * {c7} + {b7} = ?", a7 * c7 + b7);
                            case 3:
                                int product7 = a7 * c7;
                                return ($"{product7} / {c7} - {b7} = ?", a7 - b7);
                            default: return ($"{a7} + {b7} = ?", a7 + b7);
                        }

                    default:
                        int a = _random.Next(1, 10 * difficulty);
                        int b = _random.Next(1, 10 * difficulty);
                        return ($"{a} + {b} = ?", a + b);
                }
            }
        }
    }
}