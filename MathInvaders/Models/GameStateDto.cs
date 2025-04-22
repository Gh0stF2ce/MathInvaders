namespace MathInvaders.Models
{
    public class GameStateDto
    {
        public int MatchId { get; set; }
        public List<Player> Players { get; set; }
        public List<List<Cell>> Grid { get; set; }
        public int ClassLevel { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public string ActivePlayerId { get; set; }
        public bool GameOver { get; set; }
        public string Winner { get; set; }
        public bool ShowTaskInput { get; set; }
        public bool TimerActive { get; set; }
        public (int X, int Y)? LastMovedCell { get; set; }
        public int CurrentAttemptCost { get; set; }
        public List<int> UsedHardTaskIndices { get; set; }
        public string PlayerId { get; set; }

        public GameStateDto(GameState gameState, string playerId)
        {
            MatchId = gameState.MatchId;
            Players = gameState.Players;
            PlayerId = playerId;
            ClassLevel = gameState.ClassLevel;
            CurrentPlayerIndex = gameState.CurrentPlayerIndex;
            ActivePlayerId = gameState.ActivePlayerId;
            GameOver = gameState.GameOver;
            Winner = gameState.Winner;
            ShowTaskInput = gameState.ShowTaskInput;
            TimerActive = gameState.TimerActive;
            LastMovedCell = gameState.LastMovedCell;
            CurrentAttemptCost = gameState.CurrentAttemptCost;
            UsedHardTaskIndices = gameState.UsedHardTaskIndices;

            Grid = new List<List<Cell>>();
            for (int i = 0; i < 5; i++)
            {
                var row = new List<Cell>();
                for (int j = 0; j < 5; j++)
                {
                    row.Add(gameState.Grid[i, j]);
                }
                Grid.Add(row);
            }
        }
    }
}
