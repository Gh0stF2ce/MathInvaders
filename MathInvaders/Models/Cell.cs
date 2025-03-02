namespace MathInvaders.Models
{
    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Task { get; set; } // Например, "2 + 3 = ?"
        public int Answer { get; set; } // Правильный ответ
        public int? OwnerId { get; set; } // ID игрока, захватившего клетку
    }
}
