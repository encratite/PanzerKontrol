using System.Collections.Generic;

using ProtoBuf;

namespace PanzerKontrol
{
	enum ClientToServerMessageType
	{
		WelcomeRequest,
		RegistrationRequest,
		LoginRequest,
		CustomGamesRequest,
		CreateGameRequest,
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
		CustomGamesReply,
		CreateGameReply,
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

	enum CreateGameReplyType
	{
		Success,
		NameTaken,
		InvalidName,
	}

	enum JoinGameReplyType
	{
		Success,
		GameIsFull,
		GameDoesNotExist,
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
		public RegistrationRequest Registration { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public LoginRequest Login { get; set; }

		[ProtoMember(4, IsRequired = false)]
		public CreateGameRequest CreateGame { get; set; }

		[ProtoMember(5, IsRequired = false)]
		public ChangeTeamRequest ChangeTeam { get; set; }

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
		public CustomGamesReply CustomGames { get; set; }

		[ProtoMember(6, IsRequired = false)]
		public CreateGameReplyType? CreateGameReply { get; set; }

		[ProtoMember(7, IsRequired = false)]
		public JoinGameReply JoinGameReply { get; set; }

		[ProtoMember(8, IsRequired = false)]
		public GameInformation GameInformationUpdate { get; set; }

		[ProtoMember(9, IsRequired = false)]
		public GameStart Start { get; set; }

		public ServerToClientMessage(ServerWelcome serverWelcome)
		{
			Type = ServerToClientMessageType.WelcomeReply;
			ServerWelcome = serverWelcome;
		}

		public ServerToClientMessage(LoginReplyType loginReply)
		{
			Type = ServerToClientMessageType.LoginReply;
			LoginReply = loginReply;
		}

		public ServerToClientMessage(RegistrationReplyType registrationReply)
		{
			Type = ServerToClientMessageType.RegistrationReply;
			RegistrationReply = registrationReply;
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
	class CustomGamesReply
	{
		[ProtoMember(1)]
		public List<CustomGameEntry> Games { get; set; }
	}

	[ProtoContract]
	class CustomGameEntry
	{
		[ProtoMember(1)]
		public long GameId { get; set; }

		[ProtoMember(2)]
		public string Description { get; set; }

		[ProtoMember(3)]
		public string CreatorName { get; set; }
	}

	[ProtoContract]
	class CreateGameRequest
	{
		// The description is only specified for public games
		[ProtoMember(1, IsRequired = false)]
		public string Description { get; set; }

		[ProtoMember(2)]
		public bool IsPrivate { get; set; }
	}

	[ProtoContract]
	class JoinGameRequest
	{
		[ProtoMember(1)]
		public long GameId { get; set; }
	}

	[ProtoContract]
	class JoinGameReply
	{
		[ProtoMember(1)]
		public JoinGameReplyType Type { get; set; }

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
