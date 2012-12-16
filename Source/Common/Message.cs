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
		JoinLobbyRequest,
		JoinTeamRequest,
		SetFactionRequest,
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
		// I can't think of any useful error states right now, just leaving this for now
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
		public JoinLobbyRequest JoinLobbyRequest { get; set; }

		[ProtoMember(6, IsRequired = false)]
		public JoinTeamRequest JoinTeamRequest { get; set; }

		[ProtoMember(7, IsRequired = false)]
		public PlayerInitialisationResultType? PlayerInitialisationResult { get; set; }

		[ProtoMember(8, IsRequired = false)]
		public GameInitialisationResultType? GameInitialisationResult { get; set; }

		[ProtoMember(9, IsRequired = false)]
		public SetFactionRequest SetFactionRequest { get; set; }

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
		public JoinLobbyReply JoinLobbyReply { get; set; }

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
		// The description is only specified for public games.
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

		// The game ID is only transmitted if the lobby was created successfully.
		[ProtoMember(2, IsRequired = false)]
		public long? GameId { get; set; }

		public CreateLobbyReply(long gameId)
		{
			Type = CreateLobbyReplyType.Success;
			GameId = gameId;
		}
	}

	[ProtoContract]
	class JoinLobbyRequest
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
	}

	[ProtoContract]
	class TeamPlayerInformation
	{
		[ProtoMember(1)]
		public PlayerInformation Player { get; set; }

		// This flag is set to true if the player created the lobby and is granted special privileges, including changing the map, kicking players, rearranging teams.
		[ProtoMember(2)]
		public bool IsOwner { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public int? FactionId { get; set; }
	}

	[ProtoContract]
	class GameInformation
	{
		[ProtoMember(1)]
		public List<TeamInformation> Teams { get; set; }

		// This is only set once the owner of the lobby has chosen a map.
		[ProtoMember(2, IsRequired = false)]
		public string Map { get; set; }

		// This is only set once the owner of the lobby has chosen a number of points that may be spent during the picking phase.
		[ProtoMember(3, IsRequired = false)]
		public int? Points { get; set; }
	}

	[ProtoContract]
	class TeamInformation
	{
		[ProtoMember(1)]
		public List<TeamPlayerInformation> Players { get; set; }
	}

	// The change team request contains a player ID because this class is actually also used to move other players forcefully (given sufficient privileges).
	[ProtoContract]
	class JoinTeamRequest
	{
		[ProtoMember(1)]
		public long PlayerId { get; set; }

		[ProtoMember(2)]
		public int NewTeamId { get; set; }
	}

	[ProtoContract]
	class SetFactionRequest
	{
		[ProtoMember(1)]
		public int Faction { get; set; }
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
