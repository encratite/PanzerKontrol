using System.Net;

namespace PanzerKontrol
{
	class GameServerConfiguration
	{
		public IPEndPoint ServerEndpoint { get; set; }
		public string DatabasePath { get; set; }
		public byte[] Salt { get; set; }
		public bool EnableUserRegistration { get; set; }
		public bool EnableGuestLogin { get; set; }

		public int MaximumNameLength { get; set; }
	}
}
