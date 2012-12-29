using System.Timers;

namespace PanzerKontrol
{
	class GameTimer
	{
		public delegate void GameTimerHandler();

		Timer Timer;
		bool Cancelled;

		GameTimerHandler Handler;

		public GameTimer(int seconds, GameTimerHandler handler)
		{
			Timer = new Timer();
			Timer.Elapsed += new ElapsedEventHandler(ElapsedEvent);
			Timer.AutoReset = false;
			Cancelled = false;
			Handler = handler;
		}

		public void Start()
		{
			Timer.Start();
		}

		public void Stop()
		{
			lock (this)
			{
				Timer.Stop();
				Cancelled = true;
			}
		}

		void ElapsedEvent(object source, ElapsedEventArgs arguments)
		{
			lock (this)
			{
				if (Cancelled)
					return;
			}
			Handler();
		}
	}
}
