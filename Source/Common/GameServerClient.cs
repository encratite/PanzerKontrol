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

	public class GameServerClient
	{
		GameServer Server;
		SslStream Stream;
		Thread Thread;

		ClientState ClientState;
		//InGameState InGameState;

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

		public GameServerClient(SslStream stream, GameServer server)
		{
			Stream = stream;
			Server = server;
			ClientState = ClientState.Connected;

			ExpectedMessageTypes = null;
			MessageHandlerMap = new Dictionary<ClientToServerMessageType, MessageHandler>();

			ClientPlayer = null;

			InitialiseMessageHandlerMap();
		}

		public void Process()
		{
			Thread = new Thread(Run);
			Thread.Start();
		}

		void InitialiseMessageHandlerMap()
		{
			MessageHandlerMap[ClientToServerMessageType.WelcomeRequest] = OnWelcomeRequest;
			MessageHandlerMap[ClientToServerMessageType.RegistrationRequest] = OnRegistrationRequest;
			MessageHandlerMap[ClientToServerMessageType.LoginRequest] = OnLoginRequest;
			MessageHandlerMap[ClientToServerMessageType.ViewLobbiesRequest] = OnViewLobbiesRequest;
			MessageHandlerMap[ClientToServerMessageType.CreateLobbyRequest] = OnCreateLobbyRequest;

			MessageHandlerMap[ClientToServerMessageType.JoinLobbyRequest] = OnJoinLobbyRequest;
			MessageHandlerMap[ClientToServerMessageType.JoinTeamRequest] = OnJoinTeamRequest;
			MessageHandlerMap[ClientToServerMessageType.SetFactionRequest] = OnSetFactionRequest;
			MessageHandlerMap[ClientToServerMessageType.StartGameRequest] = OnStartGameRequest;
			MessageHandlerMap[ClientToServerMessageType.PlayerInitialisationResult] = OnPlayerInitialisationResult;
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

		public void SendMessage(ServerToClientMessage message)
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

		void LogIn(Player player)
		{
			ClientState = ClientState.LoggedIn;
			ClientPlayer = player;
			SetExpectedMessageTypes(ClientToServerMessageType.ViewLobbiesRequest, ClientToServerMessageType.CreateLobbyRequest, ClientToServerMessageType.JoinLobbyRequest);
		}

		void InLobby(bool isOwner)
		{
			ClientState = ClientState.InLobby;
			if (isOwner)
				SetExpectedMessageTypes(ClientToServerMessageType.JoinTeamRequest, ClientToServerMessageType.SetFactionRequest, ClientToServerMessageType.StartGameRequest);
			else
				SetExpectedMessageTypes(ClientToServerMessageType.JoinTeamRequest, ClientToServerMessageType.SetFactionRequest);
		}

		void OnWelcomeRequest(ClientToServerMessage message)
		{
			SetExpectedMessageTypes(ClientToServerMessageType.LoginRequest, ClientToServerMessageType.RegistrationRequest);
			var welcome = new ServerWelcome(Server.Version, Server.Salt);
			SendMessage(new ServerToClientMessage(welcome));
		}

		void OnRegistrationRequest(ClientToServerMessage message)
		{
			if (message.RegistrationRequest == null)
				throw new ClientException("Invalid login request");
			RegistrationReplyType result = Server.RegisterPlayer(message.RegistrationRequest);
			SendMessage(new ServerToClientMessage(result));
		}	

		void OnLoginRequest(ClientToServerMessage message)
		{
			if (message.LoginRequest == null)
				throw new ClientException("Invalid login request");
			LoginRequest login = message.LoginRequest;
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

		void OnViewLobbiesRequest(ClientToServerMessage message)
		{
			ViewLobbiesReply reply = Server.ViewLobbies();
			SendMessage(new ServerToClientMessage(reply));
		}

		void OnCreateLobbyRequest(ClientToServerMessage message)
		{
			if (message.CreateLobbyRequest == null)
				throw new ClientException("Invalid lobby creation request");
			CreateLobbyReply reply = Server.CreateLobby(this, message.CreateLobbyRequest);
			if (reply.Type == CreateLobbyReplyType.Success)
				InLobby(true);
			SendMessage(new ServerToClientMessage(reply));
		}

		void OnJoinLobbyRequest(ClientToServerMessage message)
		{
			if (message.JoinLobbyRequest == null)
				throw new ClientException("Invalid join lobby request");
			JoinLobbyReply reply = Server.JoinLobby(this, message.JoinLobbyRequest);
			if (reply.Type == JoinLobbyReplyType.Success)
				InLobby(true);
			SendMessage(new ServerToClientMessage(reply));
		}

		void OnJoinTeamRequest(ClientToServerMessage message)
		{
			throw new Exception("Not implemented");
		}

		void OnSetFactionRequest(ClientToServerMessage message)
		{
			throw new Exception("Not implemented");
		}

		void OnStartGameRequest(ClientToServerMessage message)
		{
			throw new Exception("Not implemented");
		}

		void OnPlayerInitialisationResult(ClientToServerMessage message)
		{
			throw new Exception("Not implemented");
		}
	}
}
