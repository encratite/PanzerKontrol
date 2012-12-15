using System.Collections.Generic;

using ProtoBuf;

namespace PanzerKontrol
{
	enum ClientToServerMessageType
	{
		WelcomeRequest,
		RegistrationRequest,
		LoginRequest,
		ViewLobbiesRequest,
		CreateLobbyRequest,
		JoinGameRequest,
		ChangeTeamRequest,
		StartGameRequest,
		PlayerInitialisationResult,
	}

	enum ServerToClientMessageType
	{
		WelcomeReply,
		RegistrationReply,
		LoginReply,
		ViewLobbiesReply,
		CreateLobbyReply,
		JoinGameReply,
		UpdateGameInformation,
		GameStart,
		GameInitialisationResult,
	}

	public enum RegistrationReplyType
	{
		Success,
		NameTaken,
		NameTooLong,
		WrongKeyHashSize,
		RegistrationDisabled,
	}

	public enum LoginReplyType
	{
		Success,
		NotFound,
		InvalidPassword,
		GuestLoginNotPermitted,
		GuestNameTooLong,
		GuestNameTaken,
		AlreadyLoggedIn,
	}

	public enum CreateLobbyReplyType
	{
		Success,
		// The player may not create a new game because they're already in a lobby or a game
		NotPermitted,
	}

	enum JoinLobbyReplyType
	{
		Success,
		LobbyIsFull,
		LobbyDoesNotExist,
		NeedInvitation,
	}

	enum PlayerInitialisationResultType
	{
		Success,
		MapNotFound,
		Error,
	}

	enum GameInitialisationResultType
	{
		Success,
		PlayerError,
		ServerError,
	}

	[ProtoContract]
	class ClientToServerMessage
	{
		[ProtoMember(1)]
		public ClientToServerMessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public RegistrationRequest RegistrationRequest { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public LoginRequest LoginRequest { get; set; }

		[ProtoMember(4, IsRequired = false)]
		public CreateLobbyRequest CreateLobbyRequest { get; set; }

		[ProtoMember(5, IsRequired = false)]
		public ChangeTeamRequest ChangeTeamRequest { get; set; }

		[ProtoMember(6, IsRequired = false)]
		public PlayerInitialisationResultType? PlayerInitialisationResult { get; set; }

		[ProtoMember(7, IsRequired = false)]
		public GameInitialisationResultType? GameInitialisationResult { get; set; }

		public ClientToServerMessage(ClientToServerMessageType type)
		{
			Type = type;
		}

		public static ClientToServerMessage WelcomeRequest()
		{
			return new ClientToServerMessage(ClientToServerMessageType.WelcomeRequest);
		}
	}

	[ProtoContract]
	class ServerToClientMessage
	{
		[ProtoMember(1)]
		public ServerToClientMessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public ServerWelcome ServerWelcome { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public RegistrationReplyType? RegistrationReply  { get; set; }

		[ProtoMember(4, IsRequired = false)]
		public LoginReplyType? LoginReply { get; set; }

		[ProtoMember(5, IsRequired = false)]
		public ViewLobbiesReply ViewLobbiesReply { get; set; }

		[ProtoMember(6, IsRequired = false)]
		public CreateLobbyReply CreateLobbyReply { get; set; }

		[ProtoMember(7, IsRequired = false)]
		public JoinLobbyReply JoinGameReply { get; set; }

		[ProtoMember(8, IsRequired = false)]
		public GameInformation GameInformationUpdate { get; set; }

		[ProtoMember(9, IsRequired = false)]
		public GameStart Start { get; set; }

		public ServerToClientMessage(ServerWelcome reply)
		{
			Type = ServerToClientMessageType.WelcomeReply;
			ServerWelcome = reply;
		}

		public ServerToClientMessage(LoginReplyType reply)
		{
			Type = ServerToClientMessageType.LoginReply;
			LoginReply = reply;
		}

		public ServerToClientMessage(RegistrationReplyType reply)
		{
			Type = ServerToClientMessageType.RegistrationReply;
			RegistrationReply = reply;
		}

		public ServerToClientMessage(CreateLobbyReply reply)
		{
			Type = ServerToClientMessageType.CreateLobbyReply;
			CreateLobbyReply = reply;
		}
	}

	[ProtoContract]
	class ServerWelcome
	{
		[ProtoMember(1)]
		public int Version { get; set; }

		[ProtoMember(2)]
		public byte[] Salt { get; set; }

		public ServerWelcome(int version, byte[] salt)
		{
			Version = version;
			Salt = salt;
		}
	}

	[ProtoContract]
	public class RegistrationRequest
	{
		[ProtoMember(1)]
		public string Name { get; set; }

		[ProtoMember(2)]
		public byte[] KeyHash { get; set; }
	}

	[ProtoContract]
	public class LoginRequest
	{
		[ProtoMember(1)]
		public string Name { get; set; }

		// This flag is set to true if no password is provided and the user attempts to log in as a guest without having access to the persistent statistics of a registered account.
		[ProtoMember(2)]
		public bool IsGuestLogin { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public byte[] KeyHash { get; set; }
	}

	[ProtoContract]
	class ViewLobbiesReply
	{
		// These are only the public lobbies
		[ProtoMember(1)]
		public List<LobbyInformation> Lobbies { get; set; }
	}

	[ProtoContract]
	class LobbyInformation
	{
		[ProtoMember(1)]
		public long GameId { get; set; }

		[ProtoMember(2)]
		public string Description { get; set; }

		[ProtoMember(3)]
		public string CreatorName { get; set; }
	}

	[ProtoContract]
	public class CreateLobbyRequest
	{
		// The description is only specified for public games
		[ProtoMember(1, IsRequired = false)]
		public string Description { get; set; }

		[ProtoMember(2)]
		public bool IsPrivate { get; set; }
	}

	[ProtoContract]
	public class CreateLobbyReply
	{
		[ProtoMember(1)]
		public CreateLobbyReplyType Type { get; set; }

		// The game ID is only transmitted if the lobby was created successfully
		[ProtoMember(2, IsRequired = false)]
		public int? GameId { get; set; }
	}

	[ProtoContract]
	class JoinGameRequest
	{
		[ProtoMember(1)]
		public long GameId { get; set; }
	}

	[ProtoContract]
	class JoinLobbyReply
	{
		[ProtoMember(1)]
		public JoinLobbyReplyType Type { get; set; }

		[ProtoMember(2)]
		public GameInformation Game { get; set; }
	}

	[ProtoContract]
	class PlayerInformation
	{
		[ProtoMember(1)]
		public long PlayerId { get; set; }

		[ProtoMember(2)]
		public bool IsGuest { get; set; }

		[ProtoMember(3)]
		public string Name { get; set; }

		// This flag is set to true if the player created the lobby and has privileges, including changing the map, kicking players, rearranging teams
		[ProtoMember(4)]
		public bool IsPrivileged { get; set; }

		[ProtoMember(5, IsRequired = false)]
		public int? FactionId { get; set; }
	}

	[ProtoContract]
	class GameInformation
	{
		[ProtoMember(1)]
		public List<TeamInformation> Teams { get; set; }

		[ProtoMember(2)]
		public string Map { get; set; }

		[ProtoMember(3)]
		public int Points { get; set; }
	}

	[ProtoContract]
	class TeamInformation
	{
		[ProtoMember(1)]
		public List<PlayerInformation> Players { get; set; }
	}

	// The change team request contains a player ID because this class is actually also used to move other players forcefully (given sufficient privileges)
	[ProtoContract]
	class ChangeTeamRequest
	{
		[ProtoMember(1)]
		public long PlayerId { get; set; }

		[ProtoMember(2)]
		public int NewTeamId { get; set; }
	}

	[ProtoContract]
	class GameStart
	{
		[ProtoMember(1)]
		public long GameId { get; set; }

		[ProtoMember(2)]
		public GameInformation Game { get; set; }
	}
}
