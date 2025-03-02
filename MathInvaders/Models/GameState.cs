namespace MathInvaders.Models
{
    public class GameState
    {
        public List<Player> Players { get; set; } = new List<Player>();
        public Cell[,] Grid { get; set; }
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
