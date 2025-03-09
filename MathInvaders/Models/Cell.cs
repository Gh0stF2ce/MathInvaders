namespace MathInvaders.Models
{
    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Task { get; set; } // Например, "2 + 3 = ?"
        public int Answer { get; set; } // Правильный ответ
        public int? OwnerId { get; set; } // ID игрока, захватившего клетку
        public int Difficulty { get; set; } // Сложность (1-3)
        public int Cost { get; set; } // Стоимость в монетах
        public int OriginalCost { get; set; }
        public bool IsRevealed { get; set; } = false; // Открыта ли клетка
    }
}
