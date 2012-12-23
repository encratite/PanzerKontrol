using System.Collections.Generic;

namespace PanzerKontrol
{
	public class Game
	{
		public readonly GameServerClient Owner;
		public GameServerClient Opponent;

		public readonly bool IsPrivate;
		public readonly string PrivateKey;

		public readonly MapConfiguration MapConfiguration;
		public readonly TimeConfiguration TimeConfiguration;

		public Game(GameServerClient owner, bool isPrivate, string privateKey, MapConfiguration mapConfiguration, TimeConfiguration timeConfiguration)
		{
			Owner = owner;
			Opponent = null;

			IsPrivate = isPrivate;
			PrivateKey = privateKey;

			MapConfiguration = mapConfiguration;
			TimeConfiguration = timeConfiguration;
		}

		public GameServerClient GetOtherClient(GameServerClient client)
		{
			if (object.ReferenceEquals(Owner, client))
				return Opponent;
			else
				return Owner;
		}
	}
}
