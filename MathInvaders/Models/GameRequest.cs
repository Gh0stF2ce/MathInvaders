namespace MathInvaders.Models
{
    public class GameRequest
    {
        public class GameMoveRequest
        {
            public int PlayerId { get; set; }
            public int NewX { get; set; }
            public int NewY { get; set; }
        }

        public class GameSpendRequest
        {
            public int PlayerId { get; set; }
            public bool Spend { get; set; }
        }

        public class GameAnswerRequest
        {
            public int PlayerId { get; set; }
            public int Answer { get; set; }
        }
    }
}
