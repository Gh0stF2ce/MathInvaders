namespace MathInvaders.Models
{
    public class GameRequest
    {
        public class GameMoveRequest
        {
            public int MatchId { get; set; }
            public string PlayerId { get; set; }
            public int NewX { get; set; }
            public int NewY { get; set; }
        }

        public class GameSpendRequest
        {
            public int MatchId { get; set; }
            public string PlayerId { get; set; }
            public bool Spend { get; set; }
        }

        public class GameCaptureRequest
        {
            public int MatchId { get; set; }
            public string PlayerId { get; set; }
            public bool UseOriginalTask { get; set; }
        }

        public class GameAnswerRequest
        {
            public int MatchId { get; set; }
            public string PlayerId { get; set; }
            public int Answer { get; set; }
        }
    }
}
