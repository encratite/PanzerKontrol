using System;

namespace PanzerKontrol
{
	class GameException : Exception
	{
		public GameException(string message) :
			base(message)
		{
		}
	}

	class ServerClientException : Exception
	{
		public ServerClientException(string message) :
			base(message)
		{
		}
	}

	class MissingFeatureException : Exception
	{
		public MissingFeatureException(string message) :
			base(string.Format("Missing feature: {0}", message))
		{
		}
	}
}
