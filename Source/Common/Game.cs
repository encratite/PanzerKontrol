namespace PanzerKontrol
{
	public class Game
	{
		const int PlayerCount = 2;

		public readonly GameConfiguration GameConfiguration;
		public readonly Map Map;

		protected PlayerState[] Players;
		protected bool GameIsOver;
		protected int TurnCounter;

		public Game(GameConfiguration gameConfiguration, Map map)
		{
			GameConfiguration = gameConfiguration;
			Map = map;
			Players = new PlayerState[PlayerCount];
			GameIsOver = false;
			TurnCounter = 0;
		}

		public void NewTurn()
		{
			// Only change the active player if it's not the first turn
			if (TurnCounter > 0)
			{
				foreach (var player in Players)
					player.FlipTurnState();
			}
			TurnCounter++;
		}
	}
}
