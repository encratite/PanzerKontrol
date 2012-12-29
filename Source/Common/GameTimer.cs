using System.Threading;

namespace PanzerKontrol
{
	class GameTimer
	{
		public delegate void GameTimerHandler();

		int Seconds;
		GameTimerHandler Handler;
		Thread TimerThread;
		ManualResetEvent Event;

		public GameTimer(int seconds, GameTimerHandler handler)
		{
			Seconds = seconds;
			Handler = handler;
			TimerThread = new Thread(Run);
			Event = new ManualResetEvent(false);
		}

		public void Start()
		{
			TimerThread.Start();
		}

		public void Stop()
		{
			Event.Set();
			TimerThread.Join();
		}

		void Run()
		{
			if (Event.WaitOne(Seconds * 1000))
				return;
			Handler();
		}
	}
}
