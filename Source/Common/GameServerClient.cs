﻿using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

using ProtoBuf;

namespace PanzerKontrol
{
	public enum ClientStateType
	{
		Connected,
		LoggedIn,
		WaitingForOpponent,
		InGame,
	}

	public enum GameStateType
	{
		Deployment,
		MyTurn,
		OpponentTurn,
	}

	delegate void MessageHandler(ClientToServerMessage message);

	public class GameServerClient
	{
		// The ratio of points that are reserved for the hidden picking phase
		const double HiddenPickPointRatio = 0.25;

		// The ratio of unspent points lost upon entering the hidden picking phase
		const double HiddenPickPointLossRatio = 0.5;

		// The number of units that may be used by a player during a microturn
		const int MicroTurnLimit = 3;

		GameServer Server;
		SslStream Stream;

		Thread ReceivingThread;
		Thread SendingThread;

		ManualResetEvent SendEvent;
		List<ServerToClientMessage> SendQueue;

		bool ShuttingDown;

		ClientStateType ClientState;
		GameStateType GameState;

		HashSet<ClientToServerMessageType> ExpectedMessageTypes;
		Dictionary<ClientToServerMessageType, MessageHandler> MessageHandlerMap;

		// The name chosen by the player when he logged into the server
		string PlayerName;

		// The faction chosen by this player, for the current lobby or game
		Faction PlayerFaction;

		// The game the player is currently in
		Game ActiveGame;
		
		// Reinforcement points remaining for the current game
		int? ReinforcementPoints;

		// True if the player has submitted a deployment plan
		bool? PlayerHasDeployedArmy;

		// True if the player requested the first turn privilege
		bool? PlayerRequestedFirstTurn;

		// Indicates the starting position of the player on the map
		PlayerIdentifier? Identifier;

		// The opponent in the active game
		GameServerClient PlayerOpponent;

		// Units remaining
		List<Unit> Units;

		// Units that were used in the current turn
		HashSet<Unit> CurrentTurnUnits;

		// This is a list of units used during the current maneuver
		HashSet<Unit> CurrentManeuverUnits;

		#region Read-only accessors

		public string Name
		{
			get
			{
				return PlayerName;
			}
		}

		public Faction Faction
		{
			get
			{
				return PlayerFaction;
			}
		}

		public Game Game
		{
			get
			{
				return ActiveGame;
			}
		}

		public ClientStateType State
		{
			get
			{
				return ClientState;
			}
		}

		public bool HasDeployedArmy
		{
			get
			{
				return PlayerHasDeployedArmy.Value;
			}
		}

		public bool RequestedFirstTurn
		{
			get
			{
				return PlayerRequestedFirstTurn.Value;
			}
		}

		public GameServerClient Opponent
		{
			get
			{
				return PlayerOpponent;
			}
		}

		#endregion

		#region Construction and startup

		public GameServerClient(SslStream stream, GameServer server)
		{
			Stream = stream;
			Server = server;

			SendEvent = new ManualResetEvent(false);
			SendQueue = new List<ServerToClientMessage>();

			ShuttingDown = false;

			MessageHandlerMap = new Dictionary<ClientToServerMessageType, MessageHandler>();

			InitialiseMessageHandlerMap();

			PlayerName = null;

			ResetGameState();
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

		public void OnGameStart(Game game)
		{
			ActiveGame = game;
			PlayerHasDeployedArmy = false;
			GameStart start = new GameStart(game.MapConfiguration, game.TimeConfiguration, GetBaseArmy(), Opponent.GetBaseArmy(), Opponent.Name, ReinforcementPoints.Value);
			QueueMessage(new ServerToClientMessage(start));
			PlayerOpponent = game.GetOtherClient(this);
		}

		public void OnOpponentLeftGame()
		{
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.OpponentLeftGame);
			QueueMessage(reply);
		}

		#endregion

		#region Public utility functions

		public BaseArmy GetBaseArmy()
		{
			return new BaseArmy(PlayerFaction, Units);
		}

		public Unit GetUnit(int id)
		{
			return Units.Find((Unit x) => x.Id == id);
		}

		public void ResetUnitsForNewTurn()
		{
			CurrentTurnUnits.Clear();
			foreach (var unit in Units)
				unit.ResetUnitForNewTurn();
		}

		public ServerToClientMessage MyManeuver()
		{
			CurrentManeuverUnits.Clear();
			ManeuverStart maneuverStart = new ManeuverStart(Identifier.Value, MicroTurnLimit);
			foreach (var unit in Units)
			{
				if (unit.Deployed && !CurrentTurnUnits.Contains(unit))
					maneuverStart.UnitsAvailable.Add(unit.Id);
			}
			ServerToClientMessage message = new ServerToClientMessage(maneuverStart);
			QueueMessage(message);
			Opponent.QueueMessage(message);
			InGameState(GameStateType.MyTurn);
			throw new NotImplementedException("Need appropriate message types");
			return message;
		}

		public void OpponentManeuver(ServerToClientMessage message)
		{
			CurrentManeuverUnits.Clear();
			QueueMessage(message);
			InGameState(GameStateType.OpponentTurn);
		}

		public void SendDeploymentInformation()
		{
			DeploymentPlan plan = new DeploymentPlan(RequestedFirstTurn);
			foreach (var unit in Units)
			{
				if (!unit.Deployed)
					continue;
				UnitPosition unitPosition = new UnitPosition(unit.Id, unit.Hex.Position);
				plan.Units.Add(unitPosition);
			}
			Opponent.QueueMessage(new ServerToClientMessage(plan));
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
			MessageHandlerMap[ClientToServerMessageType.LeaveGameRequest] = OnLeaveGameRequest;
			MessageHandlerMap[ClientToServerMessageType.SubmitDeploymentPlan] = OnSubmitDeploymentPlan;
			MessageHandlerMap[ClientToServerMessageType.MoveUnitRequest] = OnMoveUnitRequest;
			MessageHandlerMap[ClientToServerMessageType.AttackUnitRequest] = OnAttackUnitRequest;
			MessageHandlerMap[ClientToServerMessageType.EndManeuverRequest] = OnEndManeuverRequest;
		}

		void QueueMessage(ServerToClientMessage message)
		{
			lock (SendQueue)
			{
				SendQueue.Add(message);
				SendEvent.Set();
			}
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
			var enumerator = Serializer.DeserializeItems<ClientToServerMessage>(Stream, GameServer.Prefix, 0);
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
					Serializer.SerializeWithLengthPrefix<ServerToClientMessage>(Stream, message, GameServer.Prefix);
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

		void InitialiseArmy(BaseArmy army)
		{
			const double reinforcementPointsPenaltyFactor = 0.5;
			const double reinforcementPointsBaseRatio = 0.3;

			Units = new List<Unit>();

			int pointsSpent = 0;
			foreach (var unitConfiguration in army.Units)
			{
				Unit unit = new Unit(Identifier.Value, ActiveGame.GetUnitId(), unitConfiguration, Server);
				pointsSpent += unit.Points;
				Units.Add(unit);
			}
			int pointsAvailable = ActiveGame.MapConfiguration.Points;
			if (pointsSpent > pointsAvailable)
				throw new ClientException("You have spent too many points");
			ReinforcementPoints = (int)(reinforcementPointsPenaltyFactor * (pointsAvailable - pointsSpent) + reinforcementPointsBaseRatio * pointsAvailable);
		}

		void ResetGameState()
		{
			PlayerFaction = null;
			ActiveGame = null;

			ReinforcementPoints = null;
			PlayerHasDeployedArmy = null;
			PlayerRequestedFirstTurn = null;
			Identifier = null;
			PlayerOpponent = null;

			Units = null;
			CurrentTurnUnits = null;
			CurrentManeuverUnits = null;
		}

		#endregion

		#region Client/game state modifiers and expected message type modifiers

		void ConnectedState()
		{
			ClientState = ClientStateType.Connected;
			SetExpectedMessageTypes(ClientToServerMessageType.LoginRequest);
		}

		void LoggedInState()
		{
			ClientState = ClientStateType.LoggedIn;
			SetExpectedMessageTypes(ClientToServerMessageType.CreateGameRequest, ClientToServerMessageType.ViewPublicGamesRequest, ClientToServerMessageType.JoinGameRequest);

			ResetGameState();
		}

		void WaitingForOpponentState()
		{
			ClientState = ClientStateType.WaitingForOpponent;
			SetExpectedMessageTypes(ClientToServerMessageType.CancelGameRequest);
		}

		void InGameState(GameStateType gameState, params ClientToServerMessageType[] gameMessageTypes)
		{
			ClientState = ClientStateType.InGame;
			GameState = gameState;
			var expectedMessageTypes = new HashSet<ClientToServerMessageType>(gameMessageTypes);
			expectedMessageTypes.Add(ClientToServerMessageType.LeaveGameRequest);
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
				PlayerName = request.Name;
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
			CreateGameReply reply = Server.OnCreateGameRequest(this, request, out PlayerFaction, out ActiveGame);
			QueueMessage(new ServerToClientMessage(reply));
			Identifier = PlayerIdentifier.Player1;
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
			bool success = Server.OnJoinGameRequest(this, request);
			if (success)
			{
				Identifier = PlayerIdentifier.Player2;
				InGameState(GameStateType.Deployment, ClientToServerMessageType.SubmitDeploymentPlan);
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

		void OnLeaveGameRequest(ClientToServerMessage message)
		{
			Server.OnLeaveGameRequest(this);
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.LeaveGameConfirmation);
			QueueMessage(reply);
		}

		void OnSubmitDeploymentPlan(ClientToServerMessage message)
		{
			Map map = ActiveGame.Map;
			DeploymentPlan plan = message.DeploymentPlan;
			if (plan == null)
				throw new ClientException("Invalid deployment plan submission");
			foreach (var unitPosition in plan.Units)
			{
				var position = unitPosition.Position;
				Unit unit = GetUnit(unitPosition.UnitId);
				if (unit == null)
					throw new ClientException("Encountered an invalid unit ID in the deployment plan");
				if (unit.Deployed)
					throw new ClientException("Tried to specify the position of a unit that has already been deployed");
				if (!map.IsDeploymentZone(Identifier.Value, position))
					throw new ClientException("Tried to deploy units outside the player's deployment zone");
				Hex hex = map.GetHex(unitPosition.Position);
				if(hex.Unit != null)
					throw new ClientException("Tried to deploy a unit on a hex that is already occupied");
				unit.Deployed = true;
				unit.MoveToHex(hex);
			}
			PlayerRequestedFirstTurn = plan.RequestedFirstTurn;
			PlayerHasDeployedArmy = true;
			if (Opponent.HasDeployedArmy)
				Game.StartGame();
		}

		void OnMoveUnitRequest(ClientToServerMessage message)
		{
			MoveUnitRequest request = message.MoveUnitRequest;
			if (request == null)
				throw new ClientException("Invalid move unit request");
			if (Game.ManeuverPlayer != Identifier)
			{
				// It's not the current player's maneuver, ignore the request
				return;
			}
			Unit unit = GetUnit(request.UnitId);
			if (unit == null)
				throw new ClientException("Invalid unit ID in a move unit request");
			if (CurrentTurnUnits.Contains(unit))
				throw new ClientException("Attempted to use the same unit in more than one maneuver per turn");
			if (CurrentManeuverUnits.Count >= MicroTurnLimit && !CurrentManeuverUnits.Contains(unit))
				throw new ClientException("Attempted to use more units in a maneuver than is permitted");
			var map = ActiveGame.Map;
			var movementMap = map.CreateMovementMap(unit);
			int newMovementPoints;
			if (!movementMap.TryGetValue(request.NewPosition, out newMovementPoints))
				throw new ClientException("The unit can't reach the specified hex");
			CurrentManeuverUnits.Add(unit);
			unit.MovementPoints = newMovementPoints;
			Hex hex = map.GetHex(request.NewPosition);
			unit.MoveToHex(hex);
			UnitMove move = new UnitMove(unit.Id, newMovementPoints);
			ServerToClientMessage moveMessage = new ServerToClientMessage(move);
			QueueMessage(moveMessage);
			Opponent.QueueMessage(moveMessage);
		}

		void OnAttackUnitRequest(ClientToServerMessage message)
		{
			AttackUnitRequest request = message.AttackUnitRequest;
			if (request == null)
				throw new ClientException("Invalid attack unit request");
			throw new NotImplementedException("OnAttackUnitRequest");
		}

		void OnEndManeuverRequest(ClientToServerMessage message)
		{
			throw new NotImplementedException("OnEndMicroTurnRequest");
		}

		#endregion
	}
}
