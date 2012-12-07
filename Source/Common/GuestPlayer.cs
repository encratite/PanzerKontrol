using System;

namespace PanzerKontrol
{
	class GuestPlayer : Player
	{
		DateTime LoginTime;

		public GuestPlayer(string name)
		{
			Name = name;
			LoginTime = DateTime.UtcNow;
		}
	}
}
