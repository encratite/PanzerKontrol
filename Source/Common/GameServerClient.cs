using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

using ProtoBuf;

namespace PanzerKontrol
{
	enum ClientState
	{
		Connected,
		LoggedIn,
		WaitingForOpponent,
		InGame,
	}

	delegate void MessageHandler(ClientToServerMessage message);

	public class GameServerClient
	{
		GameServer Server;
		SslStream Stream;
		Thread Thread;

		bool ShuttingDown;

		ClientState State;

		List<ClientToServerMessageType> ExpectedMessageTypes;
		Dictionary<ClientToServerMessageType, MessageHandler> MessageHandlerMap;

		// The name chosen by the player when he logged into the server
		string PlayerName;

		// The faction chosen by this player, for the current lobby or game
		Faction PlayerFaction;

		// The game the player is currently in
		Game ActiveGame;

		// Read-only accessors
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

		public GameServerClient(SslStream stream, GameServer server)
		{
			Stream = stream;
			Server = server;
			State = ClientState.Connected;

			ShuttingDown = false;

			ExpectedMessageTypes = null;
			MessageHandlerMap = new Dictionary<ClientToServerMessageType, MessageHandler>();

			InitialiseMessageHandlerMap();

			PlayerName = null;

			PlayerFaction = null;

			ActiveGame = null;
		}

		public void Process()
		{
			Thread = new Thread(Run);
			Thread.Start();
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
		}

		void SetExpectedMessageTypes(params ClientToServerMessageType[] expectedMessageTypes)
		{
			ExpectedMessageTypes = new List<ClientToServerMessageType>(expectedMessageTypes);
			// Errors are always expected
			ExpectedMessageTypes.Add(ClientToServerMessageType.Error);
		}

		bool IsExpectedMessageType(ClientToServerMessageType type)
		{
			if (ExpectedMessageTypes == null)
				return true;
			return ExpectedMessageTypes.IndexOf(type) >= 0;
		}

		void Run()
		{
			SetExpectedMessageTypes(ClientToServerMessageType.LoginRequest);

			var enumerator = Serializer.DeserializeItems<ClientToServerMessage>(Stream, GameServer.Prefix, 0);
			foreach (var message in enumerator)
			{
				try
				{
					ProcessMessage(message);
				}
				catch (ClientException exception)
				{
					ErrorMessage error = new ErrorMessage(exception.Message);
					SendMessage(new ServerToClientMessage(error));
					ShuttingDown = true;
				}
				if (ShuttingDown)
					break;
			}
			Stream.Close();
			Server.OnClientTermination(this);
		}

		public void SendMessage(ServerToClientMessage message)
		{
			Serializer.Serialize<ServerToClientMessage>(Stream, message);
		}

		void ProcessMessage(ClientToServerMessage message)
		{
			if (!IsExpectedMessageType(message.Type))
				throw new ClientException("Received an unexpected message type");
			MessageHandler handler;
			if(!MessageHandlerMap.TryGetValue(message.Type, out handler))
				throw new Exception("Encountered an unknown server to client message type");
		}

		void SetLoggedInState()
		{
			State = ClientState.LoggedIn;
			SetExpectedMessageTypes(ClientToServerMessageType.CreateGameRequest, ClientToServerMessageType.ViewPublicGamesRequest, ClientToServerMessageType.JoinGameRequest);
			PlayerFaction = null;
			ActiveGame = null;
		}

		void OnError(ClientToServerMessage message)
		{
			ShuttingDown = true;
			throw new MissingFeatureException("The server output handler needs to see the error message");
		}

		void OnLoginRequest(ClientToServerMessage message)
		{
			LoginRequest request = message.LoginRequest;
			if (request == null)
				throw new ClientException("Invalid login request");
			LoginReply reply = Server.Login(request);
			if (reply.Type == LoginReplyType.Success)
			{
				PlayerName = request.Name;
				SetLoggedInState();
			}
			SendMessage(new ServerToClientMessage(reply));
		}

		void OnCreateGameRequest(ClientToServerMessage message)
		{
			CreateGameRequest request = message.CreateGameRequest;
			if (request == null)
				throw new ClientException("Invalid game creation request");
			CreateGameReply reply = Server.CreateGame(this, request, out PlayerFaction, out ActiveGame);
			SendMessage(new ServerToClientMessage(reply));
			State = ClientState.WaitingForOpponent;
			SetExpectedMessageTypes(ClientToServerMessageType.CancelGameRequest);
		}

		void OnViewPublicGamesRequest(ClientToServerMessage message)
		{
			ViewPublicGamesReply reply = Server.ViewPublicGames();
			SendMessage(new ServerToClientMessage(reply));
		}

		void OnJoinGameRequest(ClientToServerMessage message)
		{
			throw new MissingFeatureException("OnJoinGameRequest");
		}

		void OnCancelGameRequest(ClientToServerMessage message)
		{
			if (State == ClientState.InGame)
			{
				// The client sent a cancel game request after a game had already been started
				// This is expected, just ignore it
				return;
			}

			SetLoggedInState();
			SendMessage(new ServerToClientMessage(ServerToClientMessageType.CancelGameConfirmation));
		}

		void OnLeaveGameRequest(ClientToServerMessage message)
		{
			throw new MissingFeatureException("OnLeaveGameRequest");
		}
	}
}
