using System;

namespace PanzerKontrol
{
	public class Player
	{
		protected long PlayerId { get; set; }
		protected string PlayerName { get; set; }

		public long Id
		{
			get
			{
				return PlayerId;
			}
		}

		public string Name
		{
			get
			{
				return PlayerName;
			}
		}

		public Player(long id, string name)
		{
			PlayerId = id;
			PlayerName = name;
		}
	}
}
