﻿using System.Collections.Generic;

namespace PanzerKontrol
{
	public class Game
	{
		public readonly GameServerClient Owner;
		public GameServerClient Opponent;

		public readonly bool IsPrivate;
		public readonly string PrivateKey;

		public readonly string Map;
		public readonly int Points;

		public Game(GameServerClient owner, bool isPrivate, string privateKey, MapConfiguration mapConfiguration)
		{
			Owner = owner;
			Opponent = null;

			IsPrivate = isPrivate;
			PrivateKey = privateKey;

			Map = mapConfiguration.Map;
			Points = mapConfiguration.Points;
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