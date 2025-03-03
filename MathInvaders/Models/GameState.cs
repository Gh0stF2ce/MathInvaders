namespace MathInvaders.Models
{
    public class GameState
    {
        public List<Player> Players { get; set; } = new List<Player>();
        public Cell[,] Grid { get; set; }
        public int CurrentPlayerIndex { get; set; } = 0; // Индекс текущего игрока
        public bool GameOver { get; set; } = false;
        public string Winner { get; set; }

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
            // Проверка границ поля
            if (newX < 0 || newX >= Grid.GetLength(0) || newY < 0 || newY >= Grid.GetLength(1))
                return false;
            // Проверка, не занят ли клетка другим игроком
            return !Players.Any(p => p.X == newX && p.Y == newY && p.Id != player.Id);
        }

        public void CheckGameOver()
        {
            // Игра заканчивается, если у всех игроков закончились монеты или все клетки захвачены
            bool allCoinsSpent = Players.All(p => p.Coins == 0);
            bool allCellsCaptured = true;
            for (int x = 0; x < Grid.GetLength(0); x++)
            {
                for (int y = 0; y < Grid.GetLength(1); y++)
                {
                    if (!Grid[x, y].OwnerId.HasValue)
                    {
                        allCellsCaptured = false;
                        break;
                    }
                }
            }

            if (allCoinsSpent || allCellsCaptured)
            {
                GameOver = true;
                var winner = Players.OrderByDescending(p => p.CapturedCells).First();
                Winner = $"{winner.Name} победил с {winner.CapturedCells} клетками!";
            }
        }
    }
}
