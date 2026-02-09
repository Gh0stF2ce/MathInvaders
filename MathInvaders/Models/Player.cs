namespace MathInvaders.Models
{
    public class Player
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Coins { get; set; }
        public int CapturedCells { get; set; }
        public bool IsReady { get; set; }
    }
}