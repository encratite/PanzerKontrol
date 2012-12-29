using System;
using System.Collections.Generic;
using System.Timers;

namespace PanzerKontrol
{
	public class Game
	{
		delegate void GameTimerHandler();
		public readonly GameServerClient Owner;
		public GameServerClient Opponent;

		public readonly bool IsPrivate;
		public readonly string PrivateKey;

		public readonly MapConfiguration MapConfiguration;
		public readonly TimeConfiguration TimeConfiguration;

		public readonly Map Map;

		GameServer Server;

		bool GameIsOver;

		int UnitIdCounter;

		Random Generator;
		PlayerIdentifier? CurrentManeuverPlayer;

		public PlayerIdentifier ManeuverPlayer
		{
			get
			{
				return CurrentManeuverPlayer.Value;
			}
		}

		public Game(GameServer server, GameServerClient owner, bool isPrivate, string privateKey, MapConfiguration mapConfiguration, TimeConfiguration timeConfiguration, Map map)
		{
			Server = server;

			Owner = owner;
			Opponent = null;

			IsPrivate = isPrivate;
			PrivateKey = privateKey;

			MapConfiguration = mapConfiguration;
			TimeConfiguration = timeConfiguration;

			Map = map;

			GameIsOver = false;

			UnitIdCounter = 0;

			Generator = new Random();
			CurrentManeuverPlayer = null;
		}

		public void SetFirstTurn()
		{
			if (Owner.RequestedFirstTurn)
			{
				if (Opponent.RequestedFirstTurn)
					SetRandomFirstTurn();
				else
					CurrentManeuverPlayer = PlayerIdentifier.Player1;
			}
			else
			{
				if (Opponent.RequestedFirstTurn)
					CurrentManeuverPlayer = PlayerIdentifier.Player2;
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
			GameIsOver = true;
		}

		public int GetUnitId()
		{
			int output = UnitIdCounter;
			UnitIdCounter++;
			return output;
		}

		public void StartDeploymentTimer()
		{
			StartTimer(TimeConfiguration.DeploymentTime, OnDeploymentTimerExpiration);
		}

		public void StartTurnTimer()
		{
			StartTimer(TimeConfiguration.DeploymentTime, OnManeuverTimerExpiration);
		}

		void StartTimer(int seconds, GameTimerHandler timerHandler)
		{
			Timer timer = new Timer(1000 * seconds);
			ElapsedEventHandler eventHandler = (object source, ElapsedEventArgs arguments) => OnTimerExpiration(timerHandler);
			timer.Elapsed += eventHandler;
			timer.AutoReset = false;
			timer.Enabled = true;
		}

		void OnTimerExpiration(GameTimerHandler timerHandler)
		{
			lock (Server)
			{
				if (!GameIsOver)
					timerHandler();
			}
		}

		void SetRandomFirstTurn()
		{
			CurrentManeuverPlayer = Generator.Next(0, 1) == 1 ? PlayerIdentifier.Player1 : PlayerIdentifier.Player2;
		}

		void OnDeploymentTimerExpiration()
		{
			throw new NotImplementedException("OnDeploymentTimerExpiration");
		}

		void OnManeuverTimerExpiration()
		{
			throw new NotImplementedException("OnManeuverTimerExpiration");
		}
	}
}
