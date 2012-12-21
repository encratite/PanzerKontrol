using System;

namespace PanzerKontrol
{
	class ClientException : Exception
	{
		public ClientException(string message) :
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
