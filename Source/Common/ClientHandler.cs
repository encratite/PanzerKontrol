using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

using ProtoBuf;

namespace PanzerKontrol
{
	enum ClientState
	{
		Connected,
		LoggedIn,
		InLobby,
		InGame,
	}

	enum InGameState
	{
		// Open picking phase
		OpenPicks,
		// Hidden picking phase
		HiddenPicks,
		// It's the player's turn
		OwnMove,
		// An ally or an enemy is making a move
		OtherPlayersMove,
	}

	delegate void MessageHandler(ClientToServerMessage message);

	class ClientHandler
	{
		GameServer Server;
		Socket Socket;
		NetworkStream Stream;
		Thread Thread;

		ClientState ClientState;
		InGameState InGameState;

		ClientToServerMessageType[] ExpectedMessageTypes;
		Dictionary<ClientToServerMessageType, MessageHandler> MessageHandlerMap;

		Player ClientPlayer;

		public Player Player
		{
			get
			{
				return ClientPlayer;
			}
		}

		public ClientHandler(Socket socket, GameServer server)
		{
			Socket = socket;
			Server = server;
			Stream = new NetworkStream(Socket);
			ClientState = ClientState.Connected;

			ExpectedMessageTypes = null;
			MessageHandlerMap = new Dictionary<ClientToServerMessageType, MessageHandler>();

			ClientPlayer = null;

			InitialiseMessageHandlerMap();
		}

		public void Handle()
		{
			Thread = new Thread(Run);
			Thread.Start();
		}

		void InitialiseMessageHandlerMap()
		{
			MessageHandlerMap[ClientToServerMessageType.WelcomeRequest] = OnWelcomeRequest;
			MessageHandlerMap[ClientToServerMessageType.LoginRequest] = OnLoginRequest;
			MessageHandlerMap[ClientToServerMessageType.RegistrationRequest] = OnRegistrationRequest;
		}

		void SetExpectedMessageTypes(params ClientToServerMessageType[] expectedMessageTypes)
		{
			ExpectedMessageTypes = expectedMessageTypes;
		}

		bool IsExpectedMessageType(ClientToServerMessageType type)
		{
			if (ExpectedMessageTypes == null)
				return true;
			return Array.IndexOf(ExpectedMessageTypes, type) >= 0;
		}

		void Run()
		{
			SetExpectedMessageTypes(ClientToServerMessageType.WelcomeRequest);

			var enumerator = Serializer.DeserializeItems<ClientToServerMessage>(Stream, GameServer.Prefix, 0);
			foreach (var message in enumerator)
				ProcessMessage(message);
		}

		void SendMessage(ServerToClientMessage message)
		{
			Serializer.Serialize<ServerToClientMessage>(Stream, message);
		}

		void ProcessMessage(ClientToServerMessage message)
		{
			if (!IsExpectedMessageType(message.Type))
				throw new Exception(string.Format("Received an unexpected message type: {0}", message.Type));
			MessageHandler handler;
			if(!MessageHandlerMap.TryGetValue(message.Type, out handler))
				throw new Exception("Encountered an unknown server to client message type");
		}

		void OnWelcomeRequest(ClientToServerMessage message)
		{
			SetExpectedMessageTypes(ClientToServerMessageType.LoginRequest, ClientToServerMessageType.RegistrationRequest);
			var welcome = new ServerWelcome(Server.Version, Server.Salt);
			SendMessage(new ServerToClientMessage(welcome));
		}

		void LogIn(Player player)
		{
			ClientState = ClientState.LoggedIn;
			ClientPlayer = player;
			SetExpectedMessageTypes(ClientToServerMessageType.CustomGamesRequest, ClientToServerMessageType.CreateGameRequest, ClientToServerMessageType.JoinGameRequest);
		}

		void OnLoginRequest(ClientToServerMessage message)
		{
			if (message.Login == null)
				throw new ClientException("Invalid login request");
			LoginRequest login = message.Login;
			LoginReplyType result;
			Player player;
			if (login.IsGuestLogin)
			{
				GuestPlayer guestPlayer;
				result = Server.ProcessGuestPlayerLoginRequest(login, out guestPlayer);
				player = guestPlayer;
			}
			else
			{
				RegisteredPlayer registeredPlayer;
				result = Server.ProcessRegisteredPlayerLoginRequest(login, out registeredPlayer);
				player = registeredPlayer;
			}
			if (result == LoginReplyType.Success)
				LogIn(player);
			SendMessage(new ServerToClientMessage(result));
		}

		void OnRegistrationRequest(ClientToServerMessage message)
		{
			if (message.Registration == null)
				throw new ClientException("Invalid login request");
			RegistrationReplyType result = Server.RegisterPlayer(message.Registration);
			SendMessage(new ServerToClientMessage(result));
		}
	}
}
