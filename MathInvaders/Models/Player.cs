namespace MathInvaders.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Coins { get; set; } = 10;
        public int CapturedCells { get; set; }
    }
}
