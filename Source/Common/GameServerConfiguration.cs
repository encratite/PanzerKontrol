using System.Net;

namespace PanzerKontrol
{
	public class GameServerConfiguration
	{
		public string Address { get; set; }
		public int Port { get; set; }
		public string DatabasePath { get; set; }
		public string CertificatePath { get; set; }
		public string FactionsPath { get; set; }
		public byte[] Salt { get; set; }
		public bool EnableUserRegistration { get; set; }
		public bool EnableGuestLogin { get; set; }

		public int MaximumNameLength { get; set; }
	}
}
