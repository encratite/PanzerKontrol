using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		// True if the player requested the first turn privilege
		public bool _RequestedFirstTurn;

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

		public bool RequestedFirstTurn
		{
			get
			{
				return _RequestedFirstTurn;
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
			_Opponent = opponent;
			GameStart start = new GameStart(game.GameConfiguration, PlayerState.GetBaseArmy(), Opponent.PlayerState.GetBaseArmy(), Opponent.Name, PlayerState.ReinforcementPoints);
			QueueMessage(new ServerToClientMessage(start));
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

		public void OnGameEnd(GameEndBroadcast broadcast)
		{
			LoggedInState();
			ServerToClientMessage message = new ServerToClientMessage(broadcast);
			QueueMessage(message);
		}

		#endregion

		#region Public utility functions

		public void ResetUnits()
		{
			PlayerState.ResetUnits();
		}

		public void NewTurn(bool isMyTurn, List<Unit> attritionUnits)
		{
			var attritionCasualties = attritionUnits.Select((Unit x) => new UnitCasualties(x.Id, x.Strength, !x.CanPerformAction)).ToList();
			var identifier = isMyTurn ? Identifier : Opponent.Identifier;
			NewTurnBroadcast newTurn = new NewTurnBroadcast(identifier, attritionCasualties);
			ServerToClientMessage message = new ServerToClientMessage(newTurn);
			QueueMessage(message);
			if(isMyTurn)
				InGameState(PlayerStateType.MyTurn, ClientToServerMessageType.MoveUnit, ClientToServerMessageType.EntrenchUnit, ClientToServerMessageType.AttackUnit, ClientToServerMessageType.DeployUnit, ClientToServerMessageType.ReinforceUnit, ClientToServerMessageType.PurchaseUnit, ClientToServerMessageType.UpgradeUnit, ClientToServerMessageType.EndTurn);
			else
				InGameState(PlayerStateType.OpponentTurn);
		}

		public void SendDeploymentInformation()
		{
			InitialDeployment deployment = new InitialDeployment(PlayerState.GetDeployment(), Opponent.PlayerState.GetDeployment());
			QueueMessage(new ServerToClientMessage(deployment));
		}

		public bool HasDeployed()
		{
			return _PlayerState.State == PlayerStateType.HasDeployedUnits;
		}

		public bool IsMyTurn()
		{
			return _PlayerState.State == PlayerStateType.MyTurn;
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
			MessageHandlerMap[ClientToServerMessageType.InitialDeployment] = OnInitialDeployment;
			MessageHandlerMap[ClientToServerMessageType.MoveUnit] = OnMoveUnit;
			MessageHandlerMap[ClientToServerMessageType.EntrenchUnit] = OnEntrenchUnit;
			MessageHandlerMap[ClientToServerMessageType.AttackUnit] = OnAttackUnit;
			MessageHandlerMap[ClientToServerMessageType.DeployUnit] = OnDeployUnit;
			MessageHandlerMap[ClientToServerMessageType.ReinforceUnit] = OnReinforceUnit;
			MessageHandlerMap[ClientToServerMessageType.PurchaseUnit] = OnPurchaseUnit;
			MessageHandlerMap[ClientToServerMessageType.UpgradeUnit] = OnUpgradeUnit;
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

		void HandleException(Exception exception)
		{
			ErrorMessage error = new ErrorMessage(exception.Message);
			QueueMessage(new ServerToClientMessage(error));
			ShuttingDown = true;
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
				catch (ServerClientException exception)
				{
					HandleException(exception);
				}
				catch (GameException exception)
				{
					HandleException(exception);
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
				throw new ServerClientException("You cannot play a game with an empty base army");

			int pointsSpent = 0;
			foreach (var unitConfiguration in army.Units)
			{
				if (unitConfiguration.FactionId != PlayerState.Faction.Id)
					throw new ServerClientException("Tried to deploy an army with units from another faction");
				Unit unit = new Unit(PlayerState, Game.GetUnitId(), unitConfiguration, Server);
				pointsSpent += unit.Points;
				units.Add(unit);
			}
			int pointsAvailable = Game.GameConfiguration.Points;
			if (pointsSpent > pointsAvailable)
				throw new ServerClientException("You have spent too many points");
			int reinforcementPoints = (int)(GameConstants.ReinforcementPointsPenaltyFactor * (pointsAvailable - pointsSpent) + GameConstants.ReinforcementPointsBaseRatio * pointsAvailable);
			PlayerState.InitialiseArmy(units, reinforcementPoints);
		}

		bool HasUnitsLeft()
		{
			return PlayerState.HasUnitsLeft();
		}

		// Check if one or both armies have been fully destroyed
		void AnnihilationCheck()
		{
			if (HasUnitsLeft() && !Opponent.HasUnitsLeft())
				Server.OnGameEnd(Game, GameOutcomeType.Annihilation, this);
			else if (HasUnitsLeft() && !Opponent.HasUnitsLeft())
				Server.OnGameEnd(Game, GameOutcomeType.Annihilation, Opponent);
			else
				Server.OnGameEnd(Game, GameOutcomeType.MutualAnnihilation);
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
				throw new ServerClientException("Invalid login request");
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
				throw new ServerClientException("Invalid game creation request");
			// Defaults to false so lazy/afk players lose the first turn privilege
			_RequestedFirstTurn = false;
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
				throw new ServerClientException("Invalid join game request");
			// Defaults to false so lazy/afk players lose the first turn privilege
			_RequestedFirstTurn = false;
			InitialiseArmy(request.Army);
			Faction faction = Server.GetFaction(request.Army.FactionId);
			bool success = Server.OnJoinGameRequest(this, request, out _Game);
			if (success)
			{
				_PlayerState = new PanzerKontrol.PlayerState(Game, faction, PlayerIdentifier.Player2);
				InGameState(PlayerStateType.DeployingUnits, ClientToServerMessageType.InitialDeployment);
			}
			else
			{
				ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.NoSuchGame);
				QueueMessage(reply);
			}
			Game.OnOpponentFound(this);
		}

		void OnCancelGameRequest(ClientToServerMessage message)
		{
			Server.OnCancelGameRequest(this);
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.CancelGameConfirmation);
			QueueMessage(reply);
		}

		void OnInitialDeployment(ClientToServerMessage message)
		{
			Map map = Game.Map;
			InitialDeploymentRequest deployment = message.InitialDeploymentSubmission;
			if (deployment == null)
				throw new ServerClientException("Invalid initial deployment");
			// Reset the deployment state/position of all units to enable players to re-submit their initial deployment
			PlayerState.ResetUnitDeploymentState();
			foreach (var unitPosition in deployment.Units)
			{
				Unit unit = PlayerState.GetUnit(unitPosition.UnitId);
				if (unit == null)
					throw new ServerClientException("Encountered an invalid unit ID in the initial deployment");
				PlayerState.InitialUnitDeployment(unit, unitPosition.Position);
			}
			_RequestedFirstTurn = deployment.RequestedFirstTurn;
			PlayerState.State = PlayerStateType.HasDeployedUnits;
			if (Opponent.HasDeployed())
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
				throw new ServerClientException("Invalid move unit request");
			Unit unit = PlayerState.GetUnit(request.UnitId);
			if (unit == null)
				throw new ServerClientException("Encountered an invalid unit ID in a move request");
			int movementPointsLeft;
			List<Hex> captures;
			PlayerState.MoveUnit(unit, request.NewPosition, out movementPointsLeft, out captures);
			UnitMoveBroadcast move = new UnitMoveBroadcast(unit.Id, movementPointsLeft);
			foreach (var hex in captures)
				move.Captures.Add(hex.Position);
			ServerToClientMessage broadcast = new ServerToClientMessage(move);
			BroadcastMessage(broadcast);
		}

		void OnEntrenchUnit(ClientToServerMessage message)
		{
			UnitEntrenched entrenchUnit = message.EntrenchUnit;
			if (entrenchUnit == null)
				throw new ServerClientException("Invalid entrench unit request");
			Unit unit = PlayerState.GetUnit(entrenchUnit.UnitId);
			if (unit == null)
				throw new ServerClientException("Encountered an invalid unit ID in a move request");
			PlayerState.EntrenchUnit(unit);
			ServerToClientMessage broadcast = new ServerToClientMessage(entrenchUnit);
			BroadcastMessage(broadcast);
		}

		void OnAttackUnit(ClientToServerMessage message)
		{
			AttackUnitRequest request = message.AttackUnitRequest;
			if (request == null)
				throw new ServerClientException("Invalid attack unit request");
			Unit attacker = PlayerState.GetUnit(request.AttackerUnitId);
			if (attacker == null)
				throw new ServerClientException("Encountered an invalid attacking unit ID in an attack request");
			Unit defender = Opponent.PlayerState.GetUnit(request.DefenderUnitId);
			if (defender == null)
				throw new ServerClientException("Encountered an invalid target unit ID in an attack request");
			PlayerState.AttackUnit(attacker, defender);
			UnitCasualties attackerCasualties = new UnitCasualties(attacker.Id, attacker.Strength);
			UnitCasualties defenderCasualties = new UnitCasualties(defender.Id, defender.Strength);
			UnitCombatBroadcast casualties = new UnitCombatBroadcast(attackerCasualties, defenderCasualties);
			ServerToClientMessage broadcast = new ServerToClientMessage(casualties);
			BroadcastMessage(broadcast);
			AnnihilationCheck();
		}

		void OnDeployUnit(ClientToServerMessage message)
		{
			UnitDeployment deployment = message.UnitDeployment;
			if (deployment == null)
				throw new ServerClientException("Invalid unit deployment");
			Unit unit = PlayerState.GetUnit(deployment.Unit.UnitId);
			if (unit == null)
				throw new ServerClientException("Encountered an invalid unit ID in a deployment request");
			PlayerState.DeployUnit(unit, deployment.Unit.Position);
			ServerToClientMessage broadcast = new ServerToClientMessage(deployment);
			BroadcastMessage(broadcast);
		}

		void OnReinforceUnit(ClientToServerMessage message)
		{
			ReinforceUnitRequest request = message.ReinforceUnitRequest;
			if (request == null)
				throw new ServerClientException("Invalid unit reinforcement request");
			Unit unit = PlayerState.GetUnit(request.UnitId);
			if (unit == null)
				throw new ServerClientException("Encountered an invalid unit ID in a reinforcement request");
			PlayerState.ReinforceUnit(unit);
			UnitReinforcementBroadcast unitReinforced = new UnitReinforcementBroadcast(new ReinforcementState(PlayerState), unit.Id, unit.Strength);
			ServerToClientMessage broadcast = new ServerToClientMessage(unitReinforced);
			BroadcastMessage(broadcast);
		}

		void OnPurchaseUnit(ClientToServerMessage message)
		{
			PurchaseUnitRequest request = message.PurchaseUnitRequest;
			if (request == null)
				throw new ServerClientException("Invalid purchase request");
			if (request.Unit.FactionId != PlayerState.Faction.Id)
				throw new ServerClientException("Tried to purchase a unit from another faction");
			Unit unit = new Unit(PlayerState, Game.GetUnitId(), request.Unit, Server);
			PlayerState.PurchaseUnit(unit);
			// Update the unit ID prior to broadcasting the purchase information
			UnitConfiguration unitConfiguration = request.Unit;
			unitConfiguration.UnitId = unit.Id;
			UnitPurchasedBroadcast unitPurchased = new UnitPurchasedBroadcast(new ReinforcementState(PlayerState), request.Unit);
			ServerToClientMessage broadcast = new ServerToClientMessage(unitPurchased);
			BroadcastMessage(broadcast);
		}

		void OnUpgradeUnit(ClientToServerMessage message)
		{
			UpgradeUnitRequest request = message.UpgradeUnitRequest;
			if (request == null)
				throw new ServerClientException("Invalid upgrade unit request");
			Unit unit = PlayerState.GetUnit(request.UnitId);
			if (unit == null)
				throw new ServerClientException("Encountered an invalid unit ID in an upgrade request");
			PlayerState.UpgradeUnit(unit, unit.GetUpgrade(request.UpgradeId));
			UnitUpgradedBroadcast unitUpgraded = new UnitUpgradedBroadcast(new ReinforcementState(PlayerState), unit.Id);
			ServerToClientMessage broadcast = new ServerToClientMessage(unitUpgraded);
			BroadcastMessage(broadcast);
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
