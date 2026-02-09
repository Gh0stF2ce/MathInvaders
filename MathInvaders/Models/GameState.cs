namespace MathInvaders.Models
{
    public class GameState
    {
        public int MatchId { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public Cell[,] Grid { get; set; }
        public int ClassLevel { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public string ActivePlayerId { get; set; }
        public bool GameOver { get; set; }
        public string Winner { get; set; }
        public bool ShowTaskInput { get; set; }
        public bool TimerActive { get; set; }
        public (int X, int Y)? LastMovedCell { get; set; }
        public int CurrentAttemptCost { get; set; }
        public List<int> UsedHardTaskIndices { get; set; }

        public bool CanMove(Player player, int newX, int newY)
        {
            if (newX < 0 || newX >= Grid.GetLength(0) || newY < 0 || newY >= Grid.GetLength(1))
                return false;
            int dx = Math.Abs(player.X - newX);
            int dy = Math.Abs(player.Y - newY);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        public void CheckGameOver()
        {
            int totalCells = Grid.GetLength(0) * Grid.GetLength(1);
            foreach (var player in Players)
            {
                if (player.CapturedCells >= totalCells / 2)
                {
                    GameOver = true;
                    Winner = $"{player.Name} победил!";
                    break;
                }
            }

            if (!GameOver)
            {
                bool allCellsCaptured = true;
                for (int i = 0; i < Grid.GetLength(0); i++)
                {
                    for (int j = 0; j < Grid.GetLength(1); j++)
                    {
                        // Исправляем проверку
                        if (string.IsNullOrEmpty(Grid[i, j].OwnerId))
                        {
                            allCellsCaptured = false;
                            break;
                        }
                    }
                    if (!allCellsCaptured) break;
                }

                if (allCellsCaptured)
                {
                    GameOver = true;
                    var winner = Players.OrderByDescending(p => p.CapturedCells).First();
                    Winner = $"{winner.Name} победил с {winner.CapturedCells} клетками!";
                }
            }
        }
    }
}