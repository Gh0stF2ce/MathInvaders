namespace MathInvaders.Models
{
    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Task { get; set; }
        public int Answer { get; set; }
        public int Difficulty { get; set; }
        public int Cost { get; set; }
        public int OriginalCost { get; set; }
        public string OwnerId { get; set; }
        public bool IsRevealed { get; set; }
    }
}
