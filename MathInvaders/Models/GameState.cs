namespace MathInvaders.Models
{
    public class GameState
    {
        public List<Player> Players { get; set; } = new List<Player>();
        public Cell[,] Grid { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public int ActivePlayerId { get; set; }
        public bool GameOver { get; set; }
        public string Winner { get; set; }
        public bool ShowTaskInput { get; set; }
        public (int X, int Y)? LastMovedCell { get; set; }
        public int CurrentAttemptCost { get; set; }
        public bool TimerActive { get; set; }
        public List<int> UsedHardTaskIndices { get; set; } = new List<int>(); 

        public void CheckGameOver()
        {
            
            bool allCellsCaptured = true;
            foreach (var cell in Grid)
            {
                if (!cell.OwnerId.HasValue)
                {
                    allCellsCaptured = false;
                    break;
                }
            }
            if (allCellsCaptured)
            {
                GameOver = true;
                var winner = Players.OrderByDescending(p => p.CapturedCells).First();
                Winner = $"Игра окончена! Победитель: {winner.Name} с {winner.CapturedCells} клетками!";
            }
        }

        public bool CanMove(Player player, string direction)
        {
            int newX = player.X, newY = player.Y;
            switch (direction.ToLower())
            {
                case "up": newY--; break;
                case "down": newY++; break;
                case "left": newX--; break;
                case "right": newX++; break;
                default: return false;
            }
            return newX >= 0 && newX < Grid.GetLength(0) && newY >= 0 && newY < Grid.GetLength(1);
        }
    }
}