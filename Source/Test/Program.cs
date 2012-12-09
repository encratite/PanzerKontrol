using System;
using System.Net;

using PanzerKontrol;

namespace Test
{
	class Program
	{
		static void GenerateConfiguration()
		{
			var configuration = new GameServerConfiguration();
			configuration.Address = "127.0.0.1";
			configuration.Port = 45489;
			configuration.DatabasePath = "ServerDatabase.db4o";
			configuration.CertificatePath = "ServerCertificate.pk12";
			configuration.Salt = new byte[GameServer.SaltSize];
			Random random = new Random();
			random.NextBytes(configuration.Salt);
			//for(int i = 0; i < GameServer.SaltSize; i++)
				//configuration.Salt[i] = 
			configuration.EnableUserRegistration = true;
			configuration.EnableGuestLogin = true;
			configuration.MaximumNameLength = 50;
			var serialiser = new Nil.Serialiser<GameServerConfiguration>("Configuration.xml");
			serialiser.Store(configuration);
		}

		static void Main(string[] arguments)
		{
			GenerateConfiguration();
		}
	}
}
