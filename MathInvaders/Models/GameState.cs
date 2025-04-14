namespace MathInvaders.Models
{
    public class GameState
    {
        public List<Player> Players { get; set; } = new List<Player>();
        public Cell[,] Grid { get; set; }
        public int ClassLevel { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public int ActivePlayerId { get; set; }
        public bool GameOver { get; set; }
        public string? Winner { get; set; }
        public bool ShowTaskInput { get; set; }
        public bool TimerActive { get; set; }
        public (int X, int Y)? LastMovedCell { get; set; }
        public int CurrentAttemptCost { get; set; }
        public List<int> UsedHardTaskIndices { get; set; } = new List<int>();

        public void CheckGameOver()
        {
            int totalCells = 25;
            foreach (var player in Players)
            {
                if (player.CapturedCells > totalCells / 2)
                {
                    GameOver = true;
                    Winner = $"Игрок {player.Name} победил!";
                    return;
                }
            }

            bool allCellsCaptured = true;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (!Grid[i, j].OwnerId.HasValue)
                    {
                        allCellsCaptured = false;
                        break;
                    }
                }
            }

            if (allCellsCaptured)
            {
                var winner = Players.OrderByDescending(p => p.CapturedCells).First();
                GameOver = true;
                Winner = $"Игрок {winner.Name} победил с {winner.CapturedCells} клетками!";
            }
        }
        public bool CanMove(Player player, int newX, int newY)
        {
            if (newX < 0 || newX >= 5 || newY < 0 || newY >= 5)
                return false;

            int dx = Math.Abs(newX - player.X);
            int dy = Math.Abs(newY - player.Y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1); // Только соседние клетки
        }
    }
}