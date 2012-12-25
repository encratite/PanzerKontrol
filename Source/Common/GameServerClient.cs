using System;
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
		OpenPicks,
		HiddenPicks,
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

		// Units purchased during the picking phase, also deployed units
		List<Unit> Units;

		int? PointsAvailable;

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
			PlayerFaction = null;
			ActiveGame = null;

			Units = null;
			PointsAvailable = null;

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

		#region Public functions for events caused by external events

		public void OnGameStart(Game game)
		{
			ActiveGame = game;
			PointsAvailable = (int)(1.0 - HiddenPickPointRatio) * game.MapConfiguration.Points;
			string opponentName = object.ReferenceEquals(game.Opponent, this) ? game.Owner.Name : game.Opponent.Name;
			GameStart start = new GameStart(game.MapConfiguration, game.TimeConfiguration, opponentName, PointsAvailable.Value);
			QueueMessage(new ServerToClientMessage(start));
		}

		public void OnOpponentLeftGame()
		{
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.OpponentLeftGame);
			QueueMessage(reply);
		}

		public void OnOpenPickingTimer()
		{
			throw new NotImplementedException("Screw this");
		}

		public void OnOpenPickSubmissionCheck()
		{
			if (GameState == GameStateType.HiddenPicks)
			{
				throw new NotImplementedException("Screw this");
			}
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
			MessageHandlerMap[ClientToServerMessageType.SubmitOpenPicks] = OnSubmitOpenPicks;
			MessageHandlerMap[ClientToServerMessageType.SubmitHiddenPicks] = OnSubmitHiddenPicks;
			MessageHandlerMap[ClientToServerMessageType.SubmitDeploymentPlan] = OnSubmitDeploymentPlan;
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
			while (!ShuttingDown)
			{
				SendEvent.WaitOne();
				if (!ShuttingDown)
					break;
				lock (SendQueue)
				{
					foreach (var message in SendQueue)
						Serializer.SerializeWithLengthPrefix<ServerToClientMessage>(Stream, message, GameServer.Prefix);
					SendQueue.Clear();
					SendEvent.Reset();
				}
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

			PlayerFaction = null;
			ActiveGame = null;

			Units = new List<Unit>();
			PointsAvailable = null;
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
			CreateGameReply reply = Server.OnCreateGameRequest(this, request, out PlayerFaction, out ActiveGame);
			QueueMessage(new ServerToClientMessage(reply));
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
			bool success = Server.OnJoinGameRequest(this, request);
			if (success)
				InGameState(GameStateType.OpenPicks, ClientToServerMessageType.SubmitOpenPicks);
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

		void OnSubmitOpenPicks(ClientToServerMessage message)
		{
			PickSubmission picks = message.Picks;
			if (picks == null)
				throw new ClientException("Invalid open pick submission");
			int pointsSpent = 0;
			foreach (var pick in picks.Units)
			{
				Unit unit = new Unit(pick, Server);
				pointsSpent += unit.Points;
				Units.Add(unit);
			}
			if (pointsSpent > PointsAvailable)
				throw new ClientException("You have spent too many points");
			InGameState(GameStateType.HiddenPicks, ClientToServerMessageType.SubmitHiddenPicks);
			GameServerClient opponent = ActiveGame.GetOtherClient(this);
			opponent.OnOpenPickSubmissionCheck();
		}

		void OnSubmitHiddenPicks(ClientToServerMessage message)
		{
			PickSubmission picks = message.Picks;
			if (picks == null)
				throw new ClientException("Invalid hidden pick submission");
			throw new NotImplementedException("OnSubmitHiddenPicks");
		}

		void OnSubmitDeploymentPlan(ClientToServerMessage message)
		{
			DeploymentPlan plan = message.DeploymentPlan;
			if (plan == null)
				throw new ClientException("Invalid deployment plan submission");
			throw new NotImplementedException("OnSubmitDeploymentPlan");
		}

		#endregion
	}
}
