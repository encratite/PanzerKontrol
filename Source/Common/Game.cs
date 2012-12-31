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

		public readonly GameConfiguration GameConfiguration;

		public readonly Map Map;

		GameServer Server;

		bool GameIsOver;

		int UnitIdCounter;
		int TurnCounter;

		Random Generator;
		GameServerClient ActivePlayer;

		public Game(GameServer server, GameServerClient owner, bool isPrivate, string privateKey, GameConfiguration gameConfiguration, Map map)
		{
			Server = server;

			Owner = owner;
			Opponent = null;

			IsPrivate = isPrivate;
			PrivateKey = privateKey;

			GameConfiguration = gameConfiguration;

			Map = map;

			GameIsOver = false;

			UnitIdCounter = 0;
			TurnCounter = 0;

			Generator = new Random();
			ActivePlayer = null;
		}

		public void SetFirstTurn()
		{
			if (Owner.RequestedFirstTurn)
			{
				if (Opponent.RequestedFirstTurn)
					SetRandomFirstTurn();
				else
					ActivePlayer = Owner;
			}
			else
			{
				if (Opponent.RequestedFirstTurn)
					ActivePlayer = Opponent;
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
			StartTimer(GameConfiguration.DeploymentTime, OnDeploymentTimerExpiration);
		}

		public void StartTurnTimer()
		{
			StartTimer(GameConfiguration.TurnTime, () => OnTurnTimerExpiration(TurnCounter));
		}

		public void NewTurn()
		{
			// Only change the active player if it's not the first turn
			if (TurnCounter > 0)
				ActivePlayer = object.ReferenceEquals(Owner, ActivePlayer) ? Opponent : Owner;
			TurnCounter++;
			GameServerClient otherPlayer = GetOtherClient(ActivePlayer);
			ActivePlayer.MyTurn();
			otherPlayer.OpponenTurn();
			StartTurnTimer();
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
			GameServerClient[] players = { Owner, Opponent };
			ActivePlayer = players[Generator.Next(0, players.Length - 1)];
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

		void OnTurnTimerExpiration(int turnCounter)
		{
			// Check if the timer that expired was the one for the current turn
			// Ignore it otherwise
			if (turnCounter == TurnCounter)
				NewTurn();
			
		}
	}
}
