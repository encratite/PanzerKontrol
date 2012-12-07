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

		public RegisteredPlayer(long id, string name, byte[] passwordHash)
			: base(id, name)
		{
			PlayerName = name;
			PlayerPasswordHash = passwordHash;
			CreationTime = DateTime.UtcNow;
		}
	}
}
