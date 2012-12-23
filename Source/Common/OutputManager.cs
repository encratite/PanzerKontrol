namespace PanzerKontrol
{
	abstract public class OutputManager
	{
		public void Message(string message, params object[] arguments)
		{
			lock (this)
				WriteMessage(string.Format(message, arguments));
		}

		abstract protected void WriteMessage(string message);
	}
}
