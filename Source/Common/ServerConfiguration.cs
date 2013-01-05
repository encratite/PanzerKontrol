using System.Net;

namespace PanzerKontrol
{
	public class ServerConfiguration
	{
		public string Address { get; set; }
		public int Port { get; set; }
		// Optional
		public string CertificatePath { get; set; }

		public string FactionsPath { get; set; }
		public string MapsPath { get; set; }

		// Logical length, not physical length
		public int MaximumNameLength { get; set; }

		public ServerConfiguration()
		{
			Address = "127.0.0.1";
			Port = 45489;
			CertificatePath = null;
			FactionsPath = "Factions.xml";
			MapsPath = "Maps";

			MaximumNameLength = 50;
		}
	}
}
