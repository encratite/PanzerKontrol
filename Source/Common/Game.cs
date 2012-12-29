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
		int ManeuverCounter;

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
			ManeuverCounter = 0;
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

		public void StartManeuverTimer()
		{
			StartTimer(TimeConfiguration.ManeuverTime, () => OnManeuverTimerExpiration(ManeuverCounter));
		}

		public void NewTurn()
		{
			Owner.ResetUnitsForNewTurn();
			Opponent.ResetUnitsForNewTurn();
			NewManeuver();
		}

		public void NewManeuver()
		{
			if (ManeuverCounter > 0)
				CurrentManeuverPlayer = CurrentManeuverPlayer.Value == PlayerIdentifier.Player1 ? PlayerIdentifier.Player2 : PlayerIdentifier.Player1;
			ManeuverCounter++;
			GameServerClient activePlayer, otherPlayer;
			if (CurrentManeuverPlayer.Value == PlayerIdentifier.Player1)
			{
				activePlayer = Owner;
				otherPlayer = Opponent;
			}
			else
			{
				activePlayer = Opponent;
				otherPlayer = Owner;
			}
			var message = activePlayer.MyManeuver();
			otherPlayer.OpponentManeuver(message);
			StartManeuverTimer();
		}

		public void StartGame()
		{
			SendDeploymentInformation();
			NewTurn();
		}

		public void SendDeploymentInformation()
		{
			Owner.SendDeploymentInformation();
			Opponent.SendDeploymentInformation();
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
			if (!Owner.HasDeployedArmy || !Opponent.HasDeployedArmy)
			{
				// One of the players had not deployed their army yet
				// This means that the timer is responsible for starting the game
				StartGame();
			}
		}

		void OnManeuverTimerExpiration(int maneuverCounter)
		{
			if (maneuverCounter != ManeuverCounter)
			{
				// This was a timer for another maneuver, not the current one
				// Ignore its expiration
				return;
			}
			throw new NotImplementedException("OnManeuverTimerExpiration");
		}
	}
}
