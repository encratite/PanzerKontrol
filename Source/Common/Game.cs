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

		public readonly Map Map;

		bool GameIsOver;

		GameTimerHandler TimerHandler;
		Timer ActiveTimer;

		int UnitIdCounter;

		Random Generator;
		PlayerIdentifier? CurrentMicroTurnPlayer;

		public PlayerIdentifier MicroTurnPlayer
		{
			get
			{
				return CurrentMicroTurnPlayer.Value;
			}
		}

		public Game(GameServerClient owner, bool isPrivate, string privateKey, MapConfiguration mapConfiguration, TimeConfiguration timeConfiguration, Map map)
		{
			Owner = owner;
			Opponent = null;

			IsPrivate = isPrivate;
			PrivateKey = privateKey;

			MapConfiguration = mapConfiguration;
			TimeConfiguration = timeConfiguration;

			Map = map;

			GameIsOver = false;
			TimerHandler = null;
			ActiveTimer = null;

			UnitIdCounter = 0;

			Generator = new Random();
			CurrentMicroTurnPlayer = null;
		}

		public void SetFirstTurn()
		{
			if (Owner.RequestedFirstTurn)
			{
				if (Opponent.RequestedFirstTurn)
					SetRandomFirstTurn();
				else
					CurrentMicroTurnPlayer = PlayerIdentifier.Player1;
			}
			else
			{
				if (Opponent.RequestedFirstTurn)
					CurrentMicroTurnPlayer = PlayerIdentifier.Player2;
				else
					SetRandomFirstTurn();
			}
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

		public int GetUnitId()
		{
			int output = UnitIdCounter;
			UnitIdCounter++;
			return output;
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

		void SetRandomFirstTurn()
		{
			CurrentMicroTurnPlayer = Generator.Next(0, 1) == 1 ? PlayerIdentifier.Player1 : PlayerIdentifier.Player2;
		}
	}
}
