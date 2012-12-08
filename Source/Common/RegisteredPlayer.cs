using System;

namespace PanzerKontrol
{
	class RegisteredPlayer : Player
	{
		byte[] PlayerKeyHash;
		DateTime CreationTime;

		public byte[] KeyHash
		{
			get
			{
				return PlayerKeyHash;
			}
		}

		public RegisteredPlayer(long id, string name, byte[] keyHash)
			: base(id, name)
		{
			PlayerName = name;
			PlayerKeyHash = keyHash;
			CreationTime = DateTime.UtcNow;
		}
	}
}
