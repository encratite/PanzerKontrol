namespace PanzerKontrol
{
	class GameServerState
	{
		long PlayerId;
		long GameId;

		public GameServerState()
		{
			PlayerId = 0;
			GameId = 0;
		}

		public long GetPlayerId()
		{
			PlayerId++;
			return PlayerId;
		}

		public long GetGameId()
		{
			GameId++;
			return GameId;
		}
	}
}
