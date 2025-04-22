using Microsoft.AspNetCore.Mvc;
using MathInvaders.Models;
using static MathInvaders.Models.GameRequest;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using MathInvaders.Services;

namespace MathInvaders.Controllers
{
    public class GameController : Controller
    {
        private static Dictionary<int, GameState> _games = new Dictionary<int, GameState>();
        private static Random _random = new Random();
        private static HardTask[] _hardTasks;
        private readonly IWebHostEnvironment _env;
        private static int _matchCounter = 0;
        private readonly GameService _gameService;
        private static readonly object _lock = new object();

        public GameController(GameService gameService, IWebHostEnvironment env)
        {
            _gameService = gameService;
            _env = env;
        }

        [HttpPost]
        public IActionResult CreateMatch([FromBody] GameStartRequest request)
        {
            string playerId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("PlayerId", playerId);

            var availableMatch = _gameService.Games.Values
                .FirstOrDefault(g => g.ClassLevel == request.ClassLevel &&
                                     g.Players.Count < 4 &&
                                     !g.Players.All(p => p.IsReady));

            int matchId;
            GameState gameState;

            (int X, int Y)[] startPositions = new (int, int)[]
            {
            (0, 0), (0, 4), (4, 0), (4, 4)
            };

            if (availableMatch != null)
            {
                matchId = availableMatch.MatchId;
                gameState = availableMatch;

                int playerIndex = gameState.Players.Count;
                var (startX, startY) = startPositions[playerIndex];

                gameState.Players.Add(new Player
                {
                    Id = playerId,
                    Name = $"Player_{playerId.Substring(0, 4)}",
                    X = startX,
                    Y = startY,
                    Coins = 10,
                    CapturedCells = 0,
                    IsReady = false
                });
            }
            else
            {
                matchId = Interlocked.Increment(ref _matchCounter);
                gameState = new GameState
                {
                    MatchId = matchId,
                    ClassLevel = request.ClassLevel,
                    Players = new List<Player>(),
                    Grid = new Cell[5, 5],
                    UsedHardTaskIndices = new List<int>(),
                    CurrentPlayerIndex = 0,
                    GameOver = false,
                    Winner = null,
                    ShowTaskInput = false,
                    TimerActive = false,
                    LastMovedCell = null,
                    CurrentAttemptCost = 0
                };

                gameState.Players.Add(new Player
                {
                    Id = playerId,
                    Name = $"Player_{playerId.Substring(0, 4)}",
                    X = startPositions[0].X,
                    Y = startPositions[0].Y,
                    Coins = 10,
                    CapturedCells = 0,
                    IsReady = false
                });

                LoadHardTasks(gameState.ClassLevel);
                InitializeGrid(gameState);

                _gameService.Games[matchId] = gameState;
            }

            return Json(new { success = true, matchId = matchId, playerId = playerId });
        }

        [HttpGet]
        public IActionResult GetMatchPlayers(int matchId)
        {
            if (!_gameService.Games.ContainsKey(matchId))
            {
                return Json(new { success = false, message = "Match not found" });
            }

            var gameState = _gameService.Games[matchId];
            return Json(new { success = true, players = gameState.Players.Select(p => new { id = p.Id, isReady = p.IsReady }) });
        }
        [HttpGet]
        public IActionResult CheckAllReady(int matchId)
        {
            if (!_gameService.Games.ContainsKey(matchId))
            {
                Console.WriteLine($"CheckAllReady: Match {matchId} not found.");
                return Json(new { allReady = false });
            }

            var gameState = _gameService.Games[matchId];
            bool allReady = gameState.Players.Count >= 2 && gameState.Players.All(p => p.IsReady);

            if (allReady && string.IsNullOrEmpty(gameState.ActivePlayerId))
            {
                // Устанавливаем первого активного игрока
                gameState.ActivePlayerId = gameState.Players[0].Id;
                gameState.CurrentPlayerIndex = 0;

                // Инициализируем начальные клетки для всех игроков
                foreach (var player in gameState.Players)
                {
                    var startCell = gameState.Grid[player.X, player.Y];
                    startCell.OwnerId = player.Id;
                    startCell.IsRevealed = true;
                    player.CapturedCells = 1;
                }

                Console.WriteLine($"CheckAllReady: Game started for match {matchId}. ActivePlayerId set to {gameState.ActivePlayerId}.");
            }

            Console.WriteLine($"CheckAllReady: Match {matchId}, Players: {gameState.Players.Count}, AllReady: {allReady}, Player statuses: {string.Join(", ", gameState.Players.Select(p => $"{p.Id}: {p.IsReady}"))}");
            return Json(new { allReady = allReady });
        }

        [HttpGet]
        public IActionResult Index(int matchId)
        {
            if (!_gameService.Games.ContainsKey(matchId))
            {
                Console.WriteLine($"Index: Match {matchId} not found.");
                return RedirectToAction("Index", "Home");
            }

            var gameState = _gameService.Games[matchId];
            var playerId = HttpContext.Session.GetString("PlayerId");
            if (string.IsNullOrEmpty(playerId) || !gameState.Players.Any(p => p.Id == playerId))
            {
                Console.WriteLine($"Index: Player {playerId} not found in match {matchId}.");
                return RedirectToAction("Index", "Home");
            }

            var model = new GameStateDto(gameState, playerId);
            Console.WriteLine($"Index: Returning GameStateDto for match {matchId}, player {playerId}.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Move([FromBody] GameMoveRequest request)
        {
            if (!_gameService.Games.ContainsKey(request.MatchId))
            {
                return Json(new { success = false, message = "Match not found" });
            }

            var gameState = _gameService.Games[request.MatchId];
            if (gameState.GameOver)
            {
                return Json(new { success = false, message = "Игра окончена!" });
            }

            var currentPlayer = gameState.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (currentPlayer == null)
            {
                return Json(new { success = false, message = "Игрок не найден!" });
            }

            if (!gameState.CanMove(currentPlayer, request.NewX, request.NewY))
            {
                return Json(new { success = false, message = "Нельзя туда пойти!" });
            }

            JsonResult result;
            lock (_gameService.Games) // Синхронизация для изменения состояния
            {
                gameState.LastMovedCell = (currentPlayer.X, currentPlayer.Y);
                currentPlayer.X = request.NewX;
                currentPlayer.Y = request.NewY;

                var cell = gameState.Grid[currentPlayer.X, currentPlayer.Y];
                if (!string.IsNullOrEmpty(cell.OwnerId) && cell.OwnerId != currentPlayer.Id)
                {
                    int doubleCost = cell.OriginalCost * 2;
                    result = Json(new { success = true, isOccupied = true, doubleCost = doubleCost, originalCost = cell.OriginalCost });
                }
                else
                {
                    result = Json(new { success = true, cost = cell.Cost });
                }
            }

            // Вызываем SendGameStateUpdate после выхода из lock
            await SendGameStateUpdate(request.MatchId, gameState, request.PlayerId);
            return result;
        }

        [HttpPost]
        public async Task<IActionResult> SpendCoins([FromBody] GameSpendRequest request)
        {
            if (!_gameService.Games.ContainsKey(request.MatchId))
            {
                return Json(new { success = false, message = "Match not found" });
            }

            var gameState = _gameService.Games[request.MatchId];
            if (gameState.GameOver)
            {
                return Json(new { success = false, message = "Игра окончена!" });
            }

            var currentPlayer = gameState.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (currentPlayer == null)
            {
                return Json(new { success = false, message = "Игрок не найден!" });
            }

            var cell = gameState.Grid[currentPlayer.X, currentPlayer.Y];
            if (string.IsNullOrEmpty(cell.OwnerId) && request.Spend)
            {
                if (currentPlayer.Coins < cell.Cost)
                {
                    return Json(new { success = false, message = "Недостаточно монет!" });
                }

                lock (_gameService.Games) // Синхронизация для изменения состояния
                {
                    cell.IsRevealed = true;
                }

                // Вызываем SendGameStateUpdate после выхода из lock
                await SendGameStateUpdate(request.MatchId, gameState, request.PlayerId);
                return Json(new { success = true, task = cell.Task, timeLimit = 30 });
            }
            else
            {
                await SendGameStateUpdate(request.MatchId, gameState, request.PlayerId);
                return Json(new { success = true });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CaptureCell([FromBody] GameRequest.GameCaptureRequest request)
        {
            if (!_gameService.Games.ContainsKey(request.MatchId))
            {
                return Json(new { success = false, message = "Match not found" });
            }

            var gameState = _gameService.Games[request.MatchId];
            if (gameState.GameOver)
            {
                return Json(new { success = false, message = "Игра окончена!" });
            }

            var currentPlayer = gameState.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (currentPlayer == null)
            {
                return Json(new { success = false, message = "Игрок не найден!" });
            }

            var cell = gameState.Grid[currentPlayer.X, currentPlayer.Y];
            if (string.IsNullOrEmpty(cell.OwnerId) || cell.OwnerId == currentPlayer.Id)
            {
                return Json(new { success = false, message = "Эта клетка не принадлежит другому игроку!" });
            }

            int cost = request.UseOriginalTask ? cell.OriginalCost * 2 : cell.OriginalCost;
            if (currentPlayer.Coins < cost)
            {
                return Json(new { success = false, message = "Недостаточно монет!" });
            }

            lock (_gameService.Games) // Синхронизация для изменения состояния
            {
                currentPlayer.Coins -= cost;
                cell.IsRevealed = true;

                if (!request.UseOriginalTask)
                {
                    var (newTask, newAnswer) = GenerateTask(cell.Difficulty + 1, gameState);
                    cell.Task = newTask;
                    cell.Answer = newAnswer;
                }
            }

            // Вызываем SendGameStateUpdate после выхода из lock
            await SendGameStateUpdate(request.MatchId, gameState, request.PlayerId);
            return Json(new { success = true, task = cell.Task, timeLimit = 30 });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAnswer([FromBody] GameAnswerRequest request)
        {
            if (!_gameService.Games.ContainsKey(request.MatchId))
            {
                return Json(new { success = false, message = "Match not found" });
            }

            var gameState = _gameService.Games[request.MatchId];
            if (gameState.GameOver)
            {
                return Json(new { success = false, message = "Игра окончена!" });
            }

            var currentPlayer = gameState.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (currentPlayer == null)
            {
                return Json(new { success = false, message = "Игрок не найден!" });
            }

            var cell = gameState.Grid[currentPlayer.X, currentPlayer.Y];
            bool isCorrect = request.Answer == cell.Answer;

            lock (_gameService.Games) // Синхронизация для изменения состояния
            {
                if (isCorrect)
                {
                    cell.OwnerId = currentPlayer.Id;
                    currentPlayer.CapturedCells++;
                    gameState.LastMovedCell = (currentPlayer.X, currentPlayer.Y);
                }
                else
                {
                    currentPlayer.X = gameState.LastMovedCell.Value.X;
                    currentPlayer.Y = gameState.LastMovedCell.Value.Y;
                    cell.IsRevealed = false;
                    var (newTask, newAnswer) = GenerateTask(cell.Difficulty, gameState);
                    cell.Task = newTask;
                    cell.Answer = newAnswer;
                    gameState.LastMovedCell = null;
                }

                gameState.CheckGameOver();
            }

            // Вызываем SendGameStateUpdate после выхода из lock
            await SendGameStateUpdate(request.MatchId, gameState, request.PlayerId);
            return Json(new { success = true, wasCorrect = isCorrect });
        }

        [HttpPost]
        public async Task<IActionResult> Timeout([FromBody] GameSpendRequest request)
        {
            if (!_gameService.Games.ContainsKey(request.MatchId))
            {
                return Json(new { success = false, message = "Match not found" });
            }

            var gameState = _gameService.Games[request.MatchId];
            if (gameState.GameOver)
            {
                return Json(new { success = false, message = "Игра окончена!" });
            }

            var currentPlayer = gameState.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (currentPlayer == null)
            {
                return Json(new { success = false, message = "Игрок не найден!" });
            }

            var cell = gameState.Grid[currentPlayer.X, currentPlayer.Y];

            lock (_gameService.Games) // Синхронизация для изменения состояния
            {
                currentPlayer.X = gameState.LastMovedCell.Value.X;
                currentPlayer.Y = gameState.LastMovedCell.Value.Y;
                cell.IsRevealed = false;
                var (newTask, newAnswer) = GenerateTask(cell.Difficulty, gameState);
                cell.Task = newTask;
                cell.Answer = newAnswer;
                gameState.LastMovedCell = null;
            }

            // Вызываем SendGameStateUpdate после выхода из lock
            await SendGameStateUpdate(request.MatchId, gameState, request.PlayerId);
            return Json(new { success = true, message = "Время вышло!" });
        }

        [HttpPost]
        public IActionResult Reset()
        {
            Console.WriteLine("Reset called: Clearing all games.");
            _gameService.Games.Clear();
            return Json(new { success = true });
        }

        private void InitializeGrid(GameState gameState)
        {
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    int difficulty = _random.Next(1, 4);
                    var (task, answer) = GenerateTask(difficulty, gameState);
                    gameState.Grid[x, y] = new Cell
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
        }

        private void LoadHardTasks(int classLevel)
        {
            try
            {
                string fileName = $"hard_tasks_{classLevel}.json";
                string filePath = Path.Combine(_env.WebRootPath, "data", fileName);
                string jsonString = System.IO.File.ReadAllText(filePath);
                _hardTasks = JsonSerializer.Deserialize<HardTask[]>(jsonString);
                if (_hardTasks == null || !_hardTasks.Any())
                {
                    throw new Exception("Hard tasks file is empty or invalid.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading hard tasks: {ex.Message}");
                throw;
            }
        }

        private (string task, int answer) GenerateTask(int difficulty, GameState gameState)
        {
            if (difficulty == 3)
            {
                var availableIndices = Enumerable.Range(0, _hardTasks.Length)
                    .Where(i => !gameState.UsedHardTaskIndices.Contains(i))
                    .ToList();

                if (!availableIndices.Any())
                {
                    gameState.UsedHardTaskIndices.Clear();
                    availableIndices = Enumerable.Range(0, _hardTasks.Length).ToList();
                }

                int index = availableIndices[_random.Next(availableIndices.Count)];
                gameState.UsedHardTaskIndices.Add(index); // Используем UsedHardTaskIndices конкретного gameState
                return (_hardTasks[index].Task, _hardTasks[index].Answer);
            }
            else
            {
                switch (gameState.ClassLevel)
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

        private async Task SendGameStateUpdate(int matchId, GameState gameState, string playerId)
        {
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<MathInvaders.Hubs.GameHub>>();
            var gameStateDto = new GameStateDto(gameState, playerId);
            var serializedState = JsonSerializer.Serialize(gameStateDto);
            Console.WriteLine($"SendGameStateUpdate: Sending state for match {matchId}, player {playerId}: {serializedState}");
            await hubContext.Clients.Group(matchId.ToString()).SendAsync("ReceiveGameState", serializedState);
            Console.WriteLine($"SendGameStateUpdate: Sent update for match {matchId}, player {playerId}.");
        }
    }

    public class GameStartRequest
    {
        public int ClassLevel { get; set; }
    }
}