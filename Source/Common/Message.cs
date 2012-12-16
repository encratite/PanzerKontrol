using System.Collections.Generic;

using ProtoBuf;

namespace PanzerKontrol
{
	public enum ClientToServerMessageType
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

	public enum ServerToClientMessageType
	{
		WelcomeReply,
		RegistrationReply,
		LoginReply,
		ViewLobbiesReply,
		CreateLobbyReply,
		JoinLobbyReply,
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

	public enum JoinLobbyReplyType
	{
		Success,
		LobbyDoesNotExist,
		NeedInvitation,
	}

	public enum PlayerInitialisationResultType
	{
		Success,
		MapNotFound,
		Error,
	}

	public enum GameInitialisationResultType
	{
		Success,
		PlayerError,
		ServerError,
	}

	[ProtoContract]
	public class ClientToServerMessage
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
	public class ServerToClientMessage
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
		public DetailedGameInformation GameInformationUpdate { get; set; }

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

		public ServerToClientMessage(ViewLobbiesReply reply)
		{
			Type = ServerToClientMessageType.ViewLobbiesReply;
			ViewLobbiesReply = reply;
		}

		public ServerToClientMessage(DetailedGameInformation information)
		{
			Type = ServerToClientMessageType.UpdateGameInformation;
			GameInformationUpdate = information;
		}

		public ServerToClientMessage(JoinLobbyReply reply)
		{
			Type = ServerToClientMessageType.JoinLobbyReply;
			JoinLobbyReply = reply;
		}
	}

	[ProtoContract]
	public class ServerWelcome
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
	public class ViewLobbiesReply
	{
		// These are only the public lobbies
		[ProtoMember(1)]
		public List<GameInformation> Lobbies { get; set; }
	}

	[ProtoContract]
	public class GameInformation
	{
		[ProtoMember(1)]
		public long GameId { get; set; }

		[ProtoMember(2)]
		public string CreatorName { get; set; }

		[ProtoMember(3)]
		public string Description { get; set; }

		// This is only set once the owner of the lobby has chosen a map.
		[ProtoMember(4, IsRequired = false)]
		public string Map { get; set; }

		// This is only set once the owner of the lobby has chosen a number of points that may be spent during the picking phase.
		[ProtoMember(5, IsRequired = false)]
		public int? Points { get; set; }

		public GameInformation(Lobby lobby)
		{
			GameId = lobby.GameId;
			CreatorName = lobby.Owner.Player.Name;
			Description = lobby.Description;
		}
	}

	[ProtoContract]
	public class CreateLobbyRequest
	{
		[ProtoMember(1)]
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
	public class JoinLobbyRequest
	{
		[ProtoMember(1)]
		public long GameId { get; set; }
	}

	[ProtoContract]
	public class JoinLobbyReply
	{
		[ProtoMember(1)]
		public JoinLobbyReplyType Type { get; set; }

		[ProtoMember(2)]
		public DetailedGameInformation Game { get; set; }

		public JoinLobbyReply(JoinLobbyReplyType type)
		{
			Type = type;
		}

		public JoinLobbyReply(Lobby lobby)
		{
			Type = JoinLobbyReplyType.Success;
			Game = new DetailedGameInformation(lobby);
		}
	}

	[ProtoContract]
	public class PlayerInformation
	{
		[ProtoMember(1)]
		public long PlayerId { get; set; }

		[ProtoMember(2)]
		public string Name { get; set; }

		[ProtoMember(3)]
		public bool IsGuest { get; set; }

		public PlayerInformation(Player player)
		{
			PlayerId = player.Id;
			Name = player.Name;
			IsGuest = player.GetType() == typeof(GuestPlayer);
		}
	}

	[ProtoContract]
	public class TeamPlayerInformation
	{
		[ProtoMember(1)]
		public PlayerInformation Player { get; set; }

		// This flag is set to true if the player created the lobby and is granted special privileges, including changing the map, kicking players, rearranging teams.
		[ProtoMember(2)]
		public bool IsOwner { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public int? Faction { get; set; }

		public TeamPlayerInformation(TeamPlayer player, bool isOwner)
		{
			Player = new PlayerInformation(player.Player.Player);
			IsOwner = isOwner;
			Faction = player.Faction.Id;
		}
	}

	[ProtoContract]
	public class DetailedGameInformation
	{
		[ProtoMember(1)]
		public GameInformation Game { get; set; }

		[ProtoMember(2)]
		public List<PlayerInformation> UnassignedPlayers { get; set; }

		[ProtoMember(3)]
		public List<TeamInformation> Teams { get; set; }

		public DetailedGameInformation(Lobby lobby)
		{
			Game = new GameInformation(lobby);
			UnassignedPlayers = new List<PlayerInformation>();
			foreach (GameServerClient client in lobby.Players)
			{
				if (!lobby.IsOnATeam(client.Player))
				{
					PlayerInformation player = new PlayerInformation(client.Player);
					UnassignedPlayers.Add(player);
				}
			}
			Teams = new List<TeamInformation>();
			foreach (Team team in lobby.Teams)
			{
				TeamInformation teamInformation = new TeamInformation(lobby.Owner.Player, team);
				Teams.Add(teamInformation);
			}
		}
	}

	[ProtoContract]
	public class TeamInformation
	{
		[ProtoMember(1)]
		public List<TeamPlayerInformation> Players { get; set; }

		public TeamInformation(Player owner, Team team)
		{
			Players = new List<TeamPlayerInformation>();
			foreach (TeamPlayer player in team.Players)
			{
				TeamPlayerInformation information = new TeamPlayerInformation(player, object.ReferenceEquals(player.Player, owner));
				Players.Add(information);
			}
		}
	}

	// The change team request contains a player ID because this class is actually also used to move other players forcefully (given sufficient privileges).
	[ProtoContract]
	public class JoinTeamRequest
	{
		[ProtoMember(1)]
		public long PlayerId { get; set; }

		[ProtoMember(2)]
		public int NewTeamId { get; set; }
	}

	[ProtoContract]
	public class SetFactionRequest
	{
		[ProtoMember(1)]
		public int Faction { get; set; }
	}

	[ProtoContract]
	public class GameStart
	{
		[ProtoMember(1)]
		public long GameId { get; set; }

		[ProtoMember(2)]
		public DetailedGameInformation Game { get; set; }
	}
}
