using System;
using System.Collections.Generic;
using System.Timers;

namespace PanzerKontrol
{
	public class Game
	{
		public delegate void GameTimerHandler(Game game);

		public readonly GameServerClient Owner;
		public GameServerClient Opponent;

		public readonly bool IsPrivate;
		public readonly string PrivateKey;

		public readonly MapConfiguration MapConfiguration;
		public readonly TimeConfiguration TimeConfiguration;

		bool GameIsOver;

		GameTimerHandler TimerHandler;
		Timer ActiveTimer;

		public Game(GameServerClient owner, bool isPrivate, string privateKey, MapConfiguration mapConfiguration, TimeConfiguration timeConfiguration)
		{
			Owner = owner;
			Opponent = null;

			IsPrivate = isPrivate;
			PrivateKey = privateKey;

			MapConfiguration = mapConfiguration;
			TimeConfiguration = timeConfiguration;

			GameIsOver = false;
			TimerHandler = null;
			ActiveTimer = null;
		}

		public GameServerClient GetOtherClient(GameServerClient client)
		{
			if (object.ReferenceEquals(Owner, client))
				return Opponent;
			else
				return Owner;
		}

		public void GameOver()
		{
			lock (this)
			{
				GameIsOver = true;
				if (ActiveTimer != null)
					ActiveTimer.Stop();
			}
		}

		public void StartTimer(int seconds, GameTimerHandler timerHandler)
		{
			lock (this)
			{
				if (ActiveTimer != null)
					throw new Exception("Unable to start a new game timer because the old one has not expired yet");
				TimerHandler = timerHandler;

				ActiveTimer = new Timer(1000 * seconds);
				ActiveTimer.Elapsed += new ElapsedEventHandler(OnTimerExpiration);
				ActiveTimer.AutoReset = false;
				ActiveTimer.Enabled = true;
			}
		}

		void OnTimerExpiration(object source, ElapsedEventArgs arguments)
		{
			lock (this)
			{
				ActiveTimer = null;

				if (GameIsOver)
					TimerHandler = null;
				else
					TimerHandler(this);
			}
		}
	}
}
