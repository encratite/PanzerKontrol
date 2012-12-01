using System.IO;

namespace WeWhoDieLikeCattle
{
	class SerialisedStream
	{
		Stream Stream;

		public SerialisedStream(Stream stream)
		{
			Stream = stream;
		}
	}
}
