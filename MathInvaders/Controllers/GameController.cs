using MathInvaders.Models;
using Microsoft.AspNetCore.Mvc;

public class GameController : Controller
{
    private static GameState _gameState = new GameState();
    private static Random _random = new Random();
    private static (int X, int Y)? _lastMovedCell = null; // Последняя перемещённая клетка

    public IActionResult Index()
    {
        if (_gameState.Players.Count == 0)
        {
            InitializeGame(5);
        }
        ViewBag.LastMovedCell = _lastMovedCell; // Передаём в представление
        return View(_gameState);
    }

    [HttpPost]
    public IActionResult Move(int playerId, string direction)
    {
        if (_gameState.GameOver || _gameState.ShowTaskInput)
        {
            return RedirectToAction("Index");
        }

        var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
        if (currentPlayer.Id != playerId)
        {
            TempData["Message"] = "Сейчас не ваш ход!";
            return RedirectToAction("Index");
        }

        if (!_gameState.CanMove(currentPlayer, direction))
        {
            TempData["Message"] = "Нельзя туда пойти!";
            return RedirectToAction("Index");
        }

        int oldX = currentPlayer.X, oldY = currentPlayer.Y;
        switch (direction.ToLower())
        {
            case "up": currentPlayer.Y--; break;
            case "down": currentPlayer.Y++; break;
            case "left": currentPlayer.X--; break;
            case "right": currentPlayer.X++; break;
        }
        _lastMovedCell = (currentPlayer.X, currentPlayer.Y); // Сохраняем новую позицию

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult SpendCoins(int playerId, bool spend)
    {
        if (_gameState.GameOver || _gameState.ShowTaskInput)
        {
            return RedirectToAction("Index");
        }

        var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
        if (currentPlayer.Id != playerId)
        {
            TempData["Message"] = "Сейчас не ваш ход!";
            return RedirectToAction("Index");
        }

        var cell = _gameState.Grid[currentPlayer.X, currentPlayer.Y];
        if (cell.OwnerId.HasValue)
        {
            TempData["Message"] = "Эта клетка уже принадлежит игроку!";
            return RedirectToAction("Index");
        }

        if (spend)
        {
            if (currentPlayer.Coins >= cell.Cost)
            {
                currentPlayer.Coins -= cell.Cost;
                cell.IsRevealed = true;
                _gameState.ShowTaskInput = true;
                _lastMovedCell = null; // Сбрасываем после траты
            }
            else
            {
                TempData["Message"] = "Недостаточно монет!";
            }
        }
        else
        {
            _gameState.CurrentPlayerIndex = (_gameState.CurrentPlayerIndex + 1) % _gameState.Players.Count;
            _lastMovedCell = null;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult SubmitAnswer(int playerId, int answer)
    {
        if (_gameState.GameOver || !_gameState.ShowTaskInput)
        {
            return RedirectToAction("Index");
        }

        var currentPlayer = _gameState.Players[_gameState.CurrentPlayerIndex];
        if (currentPlayer.Id != playerId)
        {
            TempData["Message"] = "Сейчас не ваш ход!";
            return RedirectToAction("Index");
        }

        var cell = _gameState.Grid[currentPlayer.X, currentPlayer.Y];
        if (answer == cell.Answer)
        {
            cell.OwnerId = currentPlayer.Id;
            currentPlayer.CapturedCells++;
            _lastMovedCell = (currentPlayer.X, currentPlayer.Y); // Подсветка при захвате
        }
        else
        {
            TempData["Message"] = "Неверный ответ!";
        }

        _gameState.ShowTaskInput = false;
        _gameState.CurrentPlayerIndex = (_gameState.CurrentPlayerIndex + 1) % _gameState.Players.Count;
        _gameState.CheckGameOver();

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
        _gameState.GameOver = false;
        _gameState.Winner = null;
        _gameState.ShowTaskInput = false;
        _lastMovedCell = null;
    }
}