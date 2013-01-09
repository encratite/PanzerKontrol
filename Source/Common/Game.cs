using System.Collections.Generic;

namespace PanzerKontrol
{
	public class Game
	{
		const int PlayerCount = 2;

		public readonly GameConfiguration GameConfiguration;
		public readonly Map Map;

		protected PlayerState[] Players;
		protected bool GameIsOver;
		protected int SmallTurnCounter;

		public Game(GameConfiguration gameConfiguration, Map map)
		{
			GameConfiguration = gameConfiguration;
			Map = map;
			Players = new PlayerState[PlayerCount];
			GameIsOver = false;
			SmallTurnCounter = 0;
		}

		public void NewTurn()
		{
			// Only change the active player if it's not the first micro turn
			if (SmallTurnCounter > 0)
			{
				foreach (var player in Players)
					player.FlipTurnState();
			}
			SmallTurnCounter++;
		}

		public List<Unit> EvaluateSupply()
		{
			var attritionUnits = new List<Unit>();
			foreach (var player in Players)
			{
				var supplyMap = Map.GetSupplyMap(player.Identifier);
				player.EvaluateSupply(supplyMap, attritionUnits);
			}
			return attritionUnits;
		}
	}
}
