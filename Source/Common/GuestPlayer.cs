using System;

namespace PanzerKontrol
{
	public class GuestPlayer : Player
	{
		DateTime LoginTime;

		public GuestPlayer(long id, string name)
			: base(id, name)
		{
			LoginTime = DateTime.UtcNow;
		}
	}
}
