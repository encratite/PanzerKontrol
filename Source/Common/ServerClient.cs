﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using ProtoBuf;

namespace PanzerKontrol
{
	public enum ClientState
	{
		// The client connected to the server but hasn't logged in yet
		Connected,
		// The client is logged in and has chosen a name that is currently not being used by another player
		LoggedIn,
		// The client created a game and is currently waiting for an opponent to accept the challenge
		WaitingForOpponent,
		// The client is currently in a game
		InGame,
	}

	delegate void MessageHandler(ClientToServerMessage message);

	delegate void GameTimerHandler();

	public class ServerClient
	{
		Server Server;
		Stream Stream;

		Thread ReceivingThread;
		Thread SendingThread;

		ManualResetEvent SendEvent;
		List<ServerToClientMessage> SendQueue;

		bool ShuttingDown;

		ClientState _State;

		HashSet<ClientToServerMessageType> ExpectedMessageTypes;
		Dictionary<ClientToServerMessageType, MessageHandler> MessageHandlerMap;

		// The name chosen by the player when they logged onto the server
		string _Name;

		// The state of a client within a game
		PlayerState _PlayerState;

		// The current game
		ServerGame _Game;

		// The opponent in the current game
		ServerClient _Opponent;

		#region Read-only accessors

		public string Name
		{
			get
			{
				return _Name;
			}
		}

		public ClientState State
		{
			get
			{
				return _State;
			}
		}

		public PlayerIdentifier Identifier
		{
			get
			{
				return _PlayerState.Identifier;
			}
		}

		public PlayerState PlayerState
		{
			get
			{
				return _PlayerState;
			}
		}

		public ServerGame Game
		{
			get
			{
				return _Game;
			}
		}

		public ServerClient Opponent
		{
			get
			{
				return _Opponent;
			}
		}

		#endregion

		#region Construction and startup

		public ServerClient(Stream stream, Server server)
		{
			Stream = stream;
			Server = server;

			SendEvent = new ManualResetEvent(false);
			SendQueue = new List<ServerToClientMessage>();

			ShuttingDown = false;

			MessageHandlerMap = new Dictionary<ClientToServerMessageType, MessageHandler>();

			InitialiseMessageHandlerMap();

			_Name = null;

			_PlayerState = null;
			ConnectedState();
		}

		public void Run()
		{
			ReceivingThread = new Thread(ReceiveMessages);
			ReceivingThread.Start();
			SendingThread = new Thread(SendMessages);
			SendingThread.Start();
		}

		#endregion

		#region External events

		public void OnGameStart(ServerGame game, ServerClient opponent)
		{
			GameStart start = new GameStart(game.GameConfiguration, PlayerState.GetBaseArmy(), Opponent.PlayerState.GetBaseArmy(), Opponent.Name, PlayerState.ReinforcementPoints);
			QueueMessage(new ServerToClientMessage(start));
			_Opponent = opponent;
		}

		public void OnPostGameStart()
		{
			PlayerState.Opponent = Opponent.PlayerState;
		}

		public void OnOpponentLeftGame()
		{
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.OpponentLeftGame);
			QueueMessage(reply);
		}

		public void OnGameEnd(GameEnd end)
		{
			LoggedInState();
			ServerToClientMessage message = new ServerToClientMessage(end);
			QueueMessage(message);
		}

		#endregion

		#region Public utility functions

		public void MyTurn()
		{
			PlayerState.ResetUnitsForNewTurn();
			NewTurn newTurn = new NewTurn(Identifier);
			ServerToClientMessage message = new ServerToClientMessage(newTurn);
			QueueMessage(message);
			InGameState(PlayerStateType.MyTurn, ClientToServerMessageType.MoveUnit, ClientToServerMessageType.AttackUnit, ClientToServerMessageType.DeployUnit, ClientToServerMessageType.EndTurn);
		}

		public void OpponentTurn()
		{
			PlayerState.ResetUnitsForNewTurn();
			NewTurn newTurn = new NewTurn(Opponent.Identifier);
			ServerToClientMessage message = new ServerToClientMessage(newTurn);
			QueueMessage(message);
			InGameState(PlayerStateType.OpponentTurn);
		}

		public void SendDeploymentInformation()
		{
			InitialDeployment deployment = new InitialDeployment(PlayerState.GetDeployment(), Opponent.PlayerState.GetDeployment());
			QueueMessage(new ServerToClientMessage(deployment));
		}

		public bool IsDeploying()
		{
			return _PlayerState.State == PlayerStateType.DeployingUnits;
		}

		#endregion

		#region Generic internal functions

		void WriteLine(string line, params object[] arguments)
		{
			Server.OutputManager.Message(string.Format(line, arguments));
		}

		void InitialiseMessageHandlerMap()
		{
			MessageHandlerMap[ClientToServerMessageType.Error] = OnError;
			MessageHandlerMap[ClientToServerMessageType.LoginRequest] = OnLoginRequest;
			MessageHandlerMap[ClientToServerMessageType.CreateGameRequest] = OnCreateGameRequest;
			MessageHandlerMap[ClientToServerMessageType.ViewPublicGamesRequest] = OnViewPublicGamesRequest;
			MessageHandlerMap[ClientToServerMessageType.JoinGameRequest] = OnJoinGameRequest;
			MessageHandlerMap[ClientToServerMessageType.CancelGameRequest] = OnCancelGameRequest;
			MessageHandlerMap[ClientToServerMessageType.SubmitInitialDeployment] = OnSubmitInitialDeployment;
			MessageHandlerMap[ClientToServerMessageType.MoveUnit] = OnMoveUnit;
			MessageHandlerMap[ClientToServerMessageType.EntrenchUnit] = OnEntrenchUnit;
			MessageHandlerMap[ClientToServerMessageType.AttackUnit] = OnAttackUnit;
			MessageHandlerMap[ClientToServerMessageType.EndTurn] = OnEndTurn;
			MessageHandlerMap[ClientToServerMessageType.Surrender] = OnSurrender;
		}

		void QueueMessage(ServerToClientMessage message)
		{
			lock (SendQueue)
			{
				SendQueue.Add(message);
				SendEvent.Set();
			}
		}

		void BroadcastMessage(ServerToClientMessage message)
		{
			QueueMessage(message);
			Opponent.QueueMessage(message);
		}

		void SetExpectedMessageTypes(HashSet<ClientToServerMessageType> expectedMessageTypes)
		{
			ExpectedMessageTypes = expectedMessageTypes;
			// Errors are always expected
			ExpectedMessageTypes.Add(ClientToServerMessageType.Error);
		}

		void SetExpectedMessageTypes(params ClientToServerMessageType[] expectedMessageTypes)
		{
			SetExpectedMessageTypes(new HashSet<ClientToServerMessageType>(expectedMessageTypes));
		}

		bool IsExpectedMessageType(ClientToServerMessageType type)
		{
			return ExpectedMessageTypes.Contains(type);
		}

		void ReceiveMessages()
		{
			var enumerator = Serializer.DeserializeItems<ClientToServerMessage>(Stream, Server.Prefix, 0);
			foreach (var message in enumerator)
			{
				try
				{
					lock(Server)
						ProcessMessage(message);
				}
				catch (ClientException exception)
				{
					ErrorMessage error = new ErrorMessage(exception.Message);
					QueueMessage(new ServerToClientMessage(error));
					ShuttingDown = true;
				}
				if (ShuttingDown)
					break;
			}
			lock (Server)
			{
				ShuttingDown = true;
				Server.OnClientTermination(this);
				Stream.Close();
			}
			// Set the send event so the sending thread will terminate
			lock (SendQueue)
				SendEvent.Set();
		}

		void SendMessages()
		{
			while (Stream.CanWrite)
			{
				SendEvent.WaitOne();
				List<ServerToClientMessage> queue;
				lock (SendQueue)
				{
					queue = new List<ServerToClientMessage>(SendQueue);
					SendQueue.Clear();
					SendEvent.Reset();
				}
				foreach (var message in queue)
					Serializer.SerializeWithLengthPrefix<ServerToClientMessage>(Stream, message, Server.Prefix);
			}
		}

		void ProcessMessage(ClientToServerMessage message)
		{
			if (!IsExpectedMessageType(message.Type))
			{
				// Ignore unexpected messages
				// These are usually the result of network delay
				// However, they could also be the result of broken client implementations so log it anyways
				WriteLine("Encountered an unexpected message type: {0}", message.Type);
				return;
			}
			MessageHandler handler;
			if(!MessageHandlerMap.TryGetValue(message.Type, out handler))
				throw new Exception("Encountered an unknown server to client message type");
		}

		// Converts unit data from messages to actual units for the game state
		// Also generates IDs for units
		// The reinforcement point calculation is also justified as it is only used in multiplayer and is not used in singleplayer games
		void InitialiseArmy(BaseArmy army)
		{
			List<Unit> units = new List<Unit>();
			if (army.Units.Count == 0)
				throw new ClientException("You cannot play a game with an empty base army");

			int pointsSpent = 0;
			foreach (var unitConfiguration in army.Units)
			{
				Unit unit = new Unit(PlayerState, Game.GetUnitId(), unitConfiguration, Server);
				pointsSpent += unit.Points;
				units.Add(unit);
			}
			int pointsAvailable = Game.GameConfiguration.Points;
			if (pointsSpent > pointsAvailable)
				throw new ClientException("You have spent too many points");
			int reinforcementPoints = (int)(GameConstants.ReinforcementPointsPenaltyFactor * (pointsAvailable - pointsSpent) + GameConstants.ReinforcementPointsBaseRatio * pointsAvailable);
			PlayerState.InitialiseArmy(units, reinforcementPoints);
		}

		#endregion

		#region Client/game state modifiers and expected message type modifiers

		void ConnectedState()
		{
			_State = ClientState.Connected;
			SetExpectedMessageTypes(ClientToServerMessageType.LoginRequest);
		}

		void LoggedInState()
		{
			_State = ClientState.LoggedIn;
			SetExpectedMessageTypes(ClientToServerMessageType.CreateGameRequest, ClientToServerMessageType.ViewPublicGamesRequest, ClientToServerMessageType.JoinGameRequest);

			_PlayerState = null;
		}

		void WaitingForOpponentState()
		{
			_State = ClientState.WaitingForOpponent;
			SetExpectedMessageTypes(ClientToServerMessageType.CancelGameRequest);
		}

		void InGameState(PlayerStateType gameState, params ClientToServerMessageType[] gameMessageTypes)
		{
			_State = ClientState.InGame;
			_PlayerState.State = gameState;
			var expectedMessageTypes = new HashSet<ClientToServerMessageType>(gameMessageTypes);
			expectedMessageTypes.Add(ClientToServerMessageType.Surrender);
			SetExpectedMessageTypes(expectedMessageTypes);
		}

		#endregion

		#region Message handlers

		void OnError(ClientToServerMessage message)
		{
			ShuttingDown = true;
			WriteLine("A client experienced an error: {0}", message.ErrorMessage.Message);
		}

		void OnLoginRequest(ClientToServerMessage message)
		{
			LoginRequest request = message.LoginRequest;
			if (request == null)
				throw new ClientException("Invalid login request");
			LoginReply reply = Server.OnLoginRequest(request);
			if (reply.Type == LoginReplyType.Success)
			{
				_Name = request.Name;
				LoggedInState();
			}
			QueueMessage(new ServerToClientMessage(reply));
		}

		void OnCreateGameRequest(ClientToServerMessage message)
		{
			CreateGameRequest request = message.CreateGameRequest;
			if (request == null)
				throw new ClientException("Invalid game creation request");
			InitialiseArmy(request.Army);
			Faction faction = Server.GetFaction(request.Army.FactionId);
			CreateGameReply reply = Server.OnCreateGameRequest(this, request, out _Game);
			QueueMessage(new ServerToClientMessage(reply));
			_PlayerState = new PanzerKontrol.PlayerState(Game, faction, PlayerIdentifier.Player1);
			WaitingForOpponentState();
		}

		void OnViewPublicGamesRequest(ClientToServerMessage message)
		{
			ViewPublicGamesReply reply = Server.OnViewPublicGamesRequest();
			QueueMessage(new ServerToClientMessage(reply));
		}

		void OnJoinGameRequest(ClientToServerMessage message)
		{
			JoinGameRequest request = message.JoinGameRequest;
			if (request == null)
				throw new ClientException("Invalid join game request");
			InitialiseArmy(request.Army);
			Faction faction = Server.GetFaction(request.Army.FactionId);
			bool success = Server.OnJoinGameRequest(this, request, out _Game);
			if (success)
			{
				_PlayerState = new PanzerKontrol.PlayerState(Game, faction, PlayerIdentifier.Player2);
				InGameState(PlayerStateType.DeployingUnits, ClientToServerMessageType.SubmitInitialDeployment);
			}
			else
			{
				ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.NoSuchGame);
				QueueMessage(reply);
			}
		}

		void OnCancelGameRequest(ClientToServerMessage message)
		{
			Server.OnCancelGameRequest(this);
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.CancelGameConfirmation);
			QueueMessage(reply);
		}

		void OnSubmitInitialDeployment(ClientToServerMessage message)
		{
			Map map = Game.Map;
			InitialDeploymentSubmission deployment = message.InitialDeploymentSubmission;
			if (deployment == null)
				throw new ClientException("Invalid initial deployment");
			foreach (var unitPosition in deployment.Units)
			{
				Unit unit = PlayerState.GetUnit(unitPosition.UnitId);
				if (unit == null)
					throw new ClientException("Encountered an invalid unit ID in the initial deployment");
				PlayerState.InitialUnitDeployment(unit, unitPosition.Position);
			}
			PlayerState.RequestedFirstTurn = deployment.RequestedFirstTurn;
			PlayerState.State = PlayerStateType.HasDeployedUnits;
			if (!Opponent.IsDeploying())
			{
				// The opponent has already submitted their deployment
				// Both players are ready, start the game
				Game.StartGame();
			}
		}

		void OnMoveUnit(ClientToServerMessage message)
		{
			MoveUnitRequest request = message.MoveUnitRequest;
			if (request == null)
				throw new ClientException("Invalid move unit request");
			Unit unit = PlayerState.GetUnit(request.UnitId);
			if (unit == null)
				throw new ClientException("Encountered an invalid unit ID in a move request");
			int movementPointsLeft;
			List<Hex> captures;
			PlayerState.MoveUnit(unit, request.NewPosition, out movementPointsLeft, out captures);
			UnitMove move = new UnitMove(unit.Id, movementPointsLeft);
			foreach (var hex in captures)
				move.Captures.Add(hex.Position);
			ServerToClientMessage confirmation = new ServerToClientMessage(move);
			BroadcastMessage(confirmation);
		}

		void OnEntrenchUnit(ClientToServerMessage message)
		{
			EntrenchUnit entrenchUnit = message.EntrenchUnit;
			if (entrenchUnit == null)
				throw new ClientException("Invalid entrench unit request");
			Unit unit = PlayerState.GetUnit(entrenchUnit.UnitId);
			if (unit == null)
				throw new ClientException("Encountered an invalid unit ID in a move request");
			PlayerState.EntrenchUnit(unit);
			ServerToClientMessage confirmation = new ServerToClientMessage(entrenchUnit);
			BroadcastMessage(confirmation);
		}

		void OnAttackUnit(ClientToServerMessage message)
		{
			AttackUnitRequest request = message.AttackUnitRequest;
			if (request == null)
				throw new ClientException("Invalid attack unit request");
			Unit attacker = PlayerState.GetUnit(request.AttackerUnitId);
			if (attacker == null)
				throw new ClientException("Encountered an invalid attacking unit ID in an attack request");
			Unit defender = Opponent.PlayerState.GetUnit(request.DefenderUnitId);
			if (defender == null)
				throw new ClientException("Encountered an invalid target unit ID in an attack request");
			PlayerState.AttackUnit(attacker, defender);
			UnitCasualties attackerCasualties = new UnitCasualties(attacker.Id, attacker.Strength);
			UnitCasualties defenderCasualties = new UnitCasualties(defender.Id, defender.Strength);
			UnitAttack casualties = new UnitAttack(attackerCasualties, defenderCasualties);
			ServerToClientMessage casualtyMessage = new ServerToClientMessage(casualties);
			BroadcastMessage(casualtyMessage);
		}

		void OnDeployUnit(ClientToServerMessage message)
		{
			UnitDeployment deployment = message.UnitDeployment;
			if (deployment == null)
				throw new ClientException("Invalid unit deployment");
			Unit unit = PlayerState.GetUnit(deployment.Unit.UnitId);
			if (unit == null)
				throw new ClientException("Encountered an invalid unit ID in a deployment request");
			PlayerState.DeployUnit(unit, deployment.Unit.Position);
			ServerToClientMessage confirmation = new ServerToClientMessage(deployment);
			BroadcastMessage(confirmation);
		}

		void OnEndTurn(ClientToServerMessage message)
		{
			Game.NewTurn();
		}

		void OnSurrender(ClientToServerMessage message)
		{
			Server.OnGameEnd(Game, GameOutcomeType.Surrender, Opponent);
		}

		#endregion
	}
}