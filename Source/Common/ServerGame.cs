using System;
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

		public ServerClient Opponent
		{
			get
			{
				return _Opponent;
			}
		}

		public ServerGame(Server server, ServerClient owner, bool isPrivate, string privateKey, GameConfiguration gameConfiguration, Map map)
			: base(gameConfiguration, map)
		{
			Server = server;

			Owner = owner;

			UnitIdCounter = 0;

			throw new NotImplementedException("ActivePlayer.MyTurn(); otherPlayer.OpponenTurn(); StartTurnTimer();");
		}

		public void OnOpponentFound(ServerClient opponent)
		{
			_Opponent = opponent;
			SetFirstTurn();
			Owner.OnGameStart(this, _Opponent);
			_Opponent.OnGameStart(this, Owner);
			Owner.OnPostGameStart();
			_Opponent.OnPostGameStart();
		}

		public void EndGame(GameEnd end)
		{
			GameIsOver = true;
			Owner.OnGameEnd(end);
			_Opponent.OnGameEnd(end);
		}

		public void SetFirstTurn()
		{
			if (Owner.PlayerState.RequestedFirstTurn)
			{
				if (_Opponent.PlayerState.RequestedFirstTurn)
					SetRandomFirstTurn();
				else
					Owner.PlayerState.SetTurnStates();
			}
			else
			{
				if (_Opponent.PlayerState.RequestedFirstTurn)
					_Opponent.PlayerState.SetTurnStates();
				else
					SetRandomFirstTurn();
			}
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

		public void StartGame()
		{
			throw new NotImplementedException("Send deployment information, start new turn");
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
			if (turnCounter == TurnCounter)
				NewTurn();
		}

		void SetRandomFirstTurn()
		{
			Random generator = new Random();
			ServerClient[] players = { Owner, _Opponent };
			ServerClient winner = players[generator.Next(0, players.Length - 1)];
			winner.PlayerState.SetTurnStates();
		}
	}
}
