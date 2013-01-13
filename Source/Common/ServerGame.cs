using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace PanzerKontrol
{
	public class ServerGame : Game
	{
		Server Server;

		public readonly ServerClient Owner;
		ServerClient _Opponent;

		public readonly bool IsPrivate;
		public readonly string PrivateKey;

		int UnitIdCounter;

		#region Public read-only accessors

		public ServerClient Opponent
		{
			get
			{
				return _Opponent;
			}
		}

		#endregion

		public ServerGame(Server server, ServerClient owner, bool isPrivate, string privateKey, GameConfiguration gameConfiguration, Map map)
			: base(gameConfiguration, map)
		{
			Server = server;

			Owner = owner;

			IsPrivate = isPrivate;
			PrivateKey = privateKey;

			UnitIdCounter = 0;
		}

		#region Public functions

		public int GetUnitId()
		{
			int output = UnitIdCounter;
			UnitIdCounter++;
			return output;
		}

		public void OnOpponentFound(ServerClient opponent)
		{
			_Opponent = opponent;
			// Set the player states in the base game object so they can be used for turn state flipping
			Players[0] = Owner.PlayerState;
			Players[1] = Opponent.PlayerState;
			Owner.OnGameStart(this, _Opponent);
			_Opponent.OnGameStart(this, Owner);
			// This is used to connect the player states of both players
			// This isn't done in OnGameStart because the _Opponent fields aren't set at that point yet
			Owner.OnPostGameStart();
			_Opponent.OnPostGameStart();
			// There is a time limit on how long the deployment may take
			// If it expires without a player deploying any units they will likely quickly lose control of the map and can only deploy units later
			StartDeploymentTimer();
		}

		public void StartGame()
		{
			Owner.SendDeploymentInformation();
			Opponent.SendDeploymentInformation();
			SetFirstTurn();
			NewTurn();
		}

		public new void NewTurn()
		{
			base.NewTurn();
			if (SmallTurnCounter <= 2 * GameConfiguration.TurnLimit)
			{
				// The game continues, more turns have to be played
				ServerClient activeClient, inactiveClient;
				if (Owner.PlayerState.State == PlayerStateType.MyTurn)
				{
					activeClient = Owner;
					inactiveClient = Opponent;
				}
				else
				{
					activeClient = Opponent;
					inactiveClient = Owner;
				}
				activeClient.ResetUnits();
				inactiveClient.ResetUnits();
				var attritionUnits = EvaluateSupply();
				activeClient.NewTurn(true, attritionUnits);
				inactiveClient.NewTurn(false, attritionUnits);
				StartTurnTimer();
			}
			else
			{
				// The maximum number of turns have been played, it's time to evaluate the map control to determine the winner of the game
				Dictionary<PlayerIdentifier, int> mapControl = Map.GetMapControl();
				int captures1 = mapControl[PlayerIdentifier.Player1];
				int captures2 = mapControl[PlayerIdentifier.Player2];
				if (captures1 > captures2)
					Server.OnGameEnd(this, GameOutcomeType.Domination, GetClient(PlayerIdentifier.Player1));
				else if(captures2 > captures1)
					Server.OnGameEnd(this, GameOutcomeType.Domination, GetClient(PlayerIdentifier.Player2));
				else
					Server.OnGameEnd(this, GameOutcomeType.Draw);
			}
		}

		public void EndGame(GameEndBroadcast end)
		{
			GameIsOver = true;
			Owner.OnGameEnd(end);
			_Opponent.OnGameEnd(end);
		}

		#endregion

		#region Generic internal functions

		void SetFirstTurn()
		{
			if (Owner.RequestedFirstTurn)
			{
				if (Opponent.RequestedFirstTurn)
					SetRandomFirstTurn();
				else
					Owner.PlayerState.SetTurnStates();
			}
			else
			{
				if (Opponent.RequestedFirstTurn)
					Opponent.PlayerState.SetTurnStates();
				else
					SetRandomFirstTurn();
			}
		}

		void SetRandomFirstTurn()
		{
			Random generator = new Random();
			ServerClient[] players = { Owner, _Opponent };
			ServerClient winner = players[generator.Next(0, players.Length - 1)];
			winner.PlayerState.SetTurnStates();
		}

		#endregion

		#region Timer functions

		public void StartDeploymentTimer()
		{
			StartTimer(GameConfiguration.DeploymentTime, OnDeploymentTimerExpiration);
		}

		void StartTurnTimer()
		{
			StartTimer(GameConfiguration.TurnTime, () => OnTurnTimerExpiration(SmallTurnCounter));
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

		void OnDeploymentTimerExpiration()
		{
			if (Owner.IsDeploying() || _Opponent.IsDeploying())
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
			if (turnCounter == SmallTurnCounter)
				NewTurn();
		}

		ServerClient GetClient(PlayerIdentifier identifier)
		{
			ServerClient[] clients = { Owner, Opponent };
			ServerClient output = clients.First((ServerClient x) => x.Identifier == identifier);
			if (output == null)
				throw new Exception("Unable to find a client matching a numeric identifier");
			return output;
		}

		#endregion
	}
}
