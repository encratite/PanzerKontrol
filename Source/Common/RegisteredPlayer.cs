using System;

namespace PanzerKontrol
{
	class RegisteredPlayer : Player
	{
		byte[] PasswordHash;
		DateTime CreationTime;

		public RegisteredPlayer(string name, byte[] passwordHash)
		{
			Name = name;
			PasswordHash = passwordHash;
			CreationTime = DateTime.UtcNow;
		}
	}
}
