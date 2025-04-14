namespace MathInvaders.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Coins { get; set; }
        public int CapturedCells { get; set; }
    }
}