using System.Net;

namespace PanzerKontrol
{
	public class GameServerConfiguration
	{
		public string Address { get; set; }
		public int Port { get; set; }
		public string CertificatePath { get; set; }

		public string FactionsPath { get; set; }
		public string MapsPath { get; set; }

		// Logical length, not physical length
		public int MaximumNameLength { get; set; }

		public GameServerConfiguration()
		{
			Address = "127.0.0.1";
			Port = 45489;
			CertificatePath = "ServerCertificate.pk12";
			FactionsPath = "Factions.xml";
			MapsPath = "Maps";

			MaximumNameLength = 50;
		}
	}
}
