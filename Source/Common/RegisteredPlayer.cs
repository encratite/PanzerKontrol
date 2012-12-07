using System;

namespace PanzerKontrol
{
	class RegisteredPlayer : Player
	{
		byte[] PlayerPasswordHash;
		DateTime CreationTime;

		public byte[] PasswordHash
		{
			get
			{
				return PlayerPasswordHash;
			}
		}

		public RegisteredPlayer(string name, byte[] passwordHash)
		{
			PlayerName = name;
			PlayerPasswordHash = passwordHash;
			CreationTime = DateTime.UtcNow;
		}
	}
}
