using System;

namespace PanzerKontrol
{
	class GuestPlayer : Player
	{
		DateTime LoginTime;

		public GuestPlayer(string name)
		{
			PlayerName = name;
			LoginTime = DateTime.UtcNow;
		}
	}
}
