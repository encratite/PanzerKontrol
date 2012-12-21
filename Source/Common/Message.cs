using System.Collections.Generic;

using ProtoBuf;

namespace PanzerKontrol
{
	public enum ClientToServerMessageType
	{
		// A fatal error occurred.
		Error,
		// Log on to the server.
		LoginRequest,
		// Create a game that may instantly be started by another player who accepts the challenge.
		CreateGameRequest,
		// Retrieve a list of public games.
		// Zero data message.
		ViewPublicGamesRequest,
		// Join a public or a private game.
		JoinGameRequest,
		// Cancel the offer for a game that was previously created with CreateGameRequest.
		// Zero data message.
		CancelGameRequest,
		// The client requests to leave a game it is currently in.
		// Zero data message.
		LeaveGameRequest,
	}

	public enum ServerToClientMessageType
	{
		// A fatal error occurred.
		Error,
		// Tells the client if the login succeeded.
		LoginReply,
		// Tells the client that the game offer has been created.
		// The key required to join private games is also transmitted with this reply.
		CreateGameReply,
		// A list of all public games.
		ViewPublicGamesReply,
		// The game has started.
		// This happens after successfully joining a game and also after an opponent joins your game.
		GameStart,
		// The game the player tried to join no longer exists.
		// Zero data message.
		NoSuchGame,
		// The servers confirms that the offer for a game that was previously created with CreateGameRequest, is now cancelled.
		// Zero data message.
		CancelGameConfirmation,
		// Confirm the LeaveGameRequest of the client.
		// Zero data message.
		LeaveGameConfirmation,
		// The opponent has left the game.
		// The game is cancelled.
		// Zero data message.
		OpponentLeftGame,
	}

	public enum LoginReplyType
	{
		Success,
		NameTooLong,
		NameInUse,
		IncompatibleVersion,
	}

	[ProtoContract]
	public class ClientToServerMessage
	{
		[ProtoMember(1)]
		public ClientToServerMessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public ErrorMessage ErrorMessage { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public LoginRequest LoginRequest { get; set; }

		[ProtoMember(4, IsRequired = false)]
		public CreateGameRequest CreateGameRequest { get; set; }

		[ProtoMember(5, IsRequired = false)]
		public JoinGameRequest JoinGameRequest { get; set; }

		public ClientToServerMessage(ClientToServerMessageType type)
		{
			Type = type;
		}

		public ClientToServerMessage(ErrorMessage message)
		{
			Type = ClientToServerMessageType.Error;
			ErrorMessage = message;
		}

		public ClientToServerMessage(LoginRequest request)
		{
			Type = ClientToServerMessageType.LoginRequest;
			LoginRequest = request;
		}

		public ClientToServerMessage(CreateGameRequest request)
		{
			Type = ClientToServerMessageType.CreateGameRequest;
			CreateGameRequest = request;
		}

		public ClientToServerMessage(JoinGameRequest request)
		{
			Type = ClientToServerMessageType.JoinGameRequest;
			JoinGameRequest = request;
		}
	}

	[ProtoContract]
	public class ServerToClientMessage
	{
		[ProtoMember(1)]
		public ServerToClientMessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public ErrorMessage ErrorMessage { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public LoginReply LoginReply { get; set; }

		[ProtoMember(4, IsRequired = false)]
		public CreateGameReply CreateGameReply { get; set; }

		[ProtoMember(5, IsRequired = false)]
		public ViewPublicGamesReply ViewPublicGamesReply { get; set; }

		[ProtoMember(6, IsRequired = false)]
		public GameStart GameStart { get; set; }

		public ServerToClientMessage(ServerToClientMessageType type)
		{
			Type = type;
		}

		public ServerToClientMessage(ErrorMessage message)
		{
			Type = ServerToClientMessageType.Error;
			ErrorMessage = message;
		}

		public ServerToClientMessage(LoginReply reply)
		{
			Type = ServerToClientMessageType.LoginReply;
			LoginReply = reply;
		}

		public ServerToClientMessage(CreateGameReply reply)
		{
			Type = ServerToClientMessageType.CreateGameReply;
			CreateGameReply = reply;
		}

		public ServerToClientMessage(ViewPublicGamesReply reply)
		{
			Type = ServerToClientMessageType.ViewPublicGamesReply;
			ViewPublicGamesReply = reply;
		}

		public ServerToClientMessage(GameStart reply)
		{
			Type = ServerToClientMessageType.GameStart;
			GameStart = reply;
		}
	}

	[ProtoContract]
	public class ErrorMessage
	{
		[ProtoMember(1)]
		public string Message { get; set; }

		public ErrorMessage(string message)
		{
			Message = message;
		}
	}

	[ProtoContract]
	public class LoginRequest
	{
		[ProtoMember(1)]
		public string Name { get; set; }

		[ProtoMember(2)]
		public int ClientVersion { get; set; }

		public LoginRequest(string name, int version)
		{
			Name = name;
			ClientVersion = version;
		}
	}

	[ProtoContract]
	public class LoginReply
	{
		[ProtoMember(1)]
		public LoginReplyType Type { get; set; }

		[ProtoMember(2)]
		public int ServerVersion { get; set; }

		public LoginReply(LoginReplyType type, int version)
		{
			Type = type;
			ServerVersion = version;
		}
	}

	[ProtoContract]
	public class CreateGameRequest
	{
		// The faction of the owner of the game
		[ProtoMember(1)]
		public int FactionId { get; set; }

		[ProtoMember(2)]
		public bool IsPrivate { get; set; }

		[ProtoMember(3)]
		public MapConfiguration MapConfiguration { get; set; }

		public CreateGameRequest(int factionId, bool isPrivate, MapConfiguration mapConfiguration)
		{
			FactionId = factionId;
			IsPrivate = isPrivate;
			MapConfiguration = mapConfiguration;
		}
	}

	[ProtoContract]
	public class MapConfiguration
	{
		// This is a part of the filename of the map
		[ProtoMember(1)]
		public string Map { get; set; }

		// The number of points that may be spent during the picking phase
		[ProtoMember(2)]
		public int Points { get; set; }

		public MapConfiguration(string map, int points)
		{
			Map = map;
			Points = points;
		}
	}

	[ProtoContract]
	public class CreateGameReply
	{
		[ProtoMember(1, IsRequired = false)]
		public string PrivateKey { get; set; }

		public CreateGameReply()
		{
			PrivateKey = null;
		}

		public CreateGameReply(string privateKey)
		{
			PrivateKey = privateKey;
		}
	}

	[ProtoContract]
	public class PublicGameInformation
	{
		[ProtoMember(1)]
		public string Owner { get; set; }

		[ProtoMember(2)]
		public MapConfiguration MapConfiguration { get; set; }

		public PublicGameInformation(string owner, MapConfiguration mapConfiguration)
		{
			Owner = owner;
			MapConfiguration = mapConfiguration;
		}
	}

	[ProtoContract]
	public class ViewPublicGamesReply
	{
		[ProtoMember(1)]
		public List<PublicGameInformation> Games { get; set; }

		public ViewPublicGamesReply()
		{
			Games = new List<PublicGameInformation>();
		}
	}

	[ProtoContract]
	public class JoinGameRequest
	{
		[ProtoMember(1)]
		public int FactionId { get; set; }

		[ProtoMember(2)]
		public bool IsPrivate { get; set; }

		// Public games are joined based on the name of the owner
		[ProtoMember(3, IsRequired = false)]
		public string Owner;

		// Private games joined using the private key that was shared
		[ProtoMember(4, IsRequired = false)]
		public string PrivateKey;

		private JoinGameRequest(int factionId, bool isPrivate, string owner, string privateKey)
		{
			FactionId = factionId;
			IsPrivate = isPrivate;
			Owner = owner;
			PrivateKey = privateKey;
		}

		public static JoinGameRequest JoinPublicGame(int factionId, string owner)
		{
			return new JoinGameRequest(factionId, false, owner, null);
		}

		public static JoinGameRequest JoinPrivateGame(int factionId, string privateKey)
		{
			return new JoinGameRequest(factionId, false, null, privateKey);
		}
	}

	[ProtoContract]
	public class GameStart
	{
		[ProtoMember(1)]
		public MapConfiguration MapConfiguration { get; set; }

		[ProtoMember(2)]
		public string Opponent;

		private GameStart( MapConfiguration mapConfiguration, string opponent)
		{
			MapConfiguration = mapConfiguration;
			Opponent = opponent;
		}
	}
}
