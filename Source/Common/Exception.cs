using System;

namespace WeWhoDieLikeCattle
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
		public MissingFeatureException() :
			base("Missing feature")
		{
		}
	}
}
