using System.Collections.Generic;

using ProtoBuf;

namespace PanzerKontrol
{
	public enum ClientToServerMessageType
	{
		// A fatal error occurred
		Error,
		// Log on to the server
		LoginRequest,
		// Create a game that may instantly be started by another player who accepts the challenge
		CreateGameRequest,
		// Retrieve a list of public games
		// Zero data message
		ViewPublicGamesRequest,
		// Join a public or a private game
		JoinGameRequest,
		// Cancel the offer for a game that was previously created with CreateGameRequest
		// Zero data message
		CancelGameRequest,
		// The client requests to leave a game it is currently in
		// Zero data message
		LeaveGameRequest,
		// Submit units purchased during the open picking phase		
		SubmitOpenPicks,
		// Submit units purchased during the hidden picking phase
		SubmitHiddenPicks,
		// Submit the deployment plan
		SubmitDeploymentPlan,
	}

	public enum ServerToClientMessageType
	{
		// A fatal error occurred
		Error,
		// Tells the client if the login succeeded
		LoginReply,
		// Tells the client that the game offer has been created
		// The key required to join private games is also transmitted with this reply
		CreateGameReply,
		// A list of all public games
		ViewPublicGamesReply,
		// The game has started
		// This happens after successfully joining a game and also after an opponent joins your game
		GameStart,
		// The game the player tried to join no longer exists
		// Zero data message
		NoSuchGame,
		// The servers confirms that the offer for a game that was previously created with CreateGameRequest, is now cancelled
		// Zero data message
		CancelGameConfirmation,
		// Confirm the LeaveGameRequest of the client
		// Zero data message
		LeaveGameConfirmation,
		// The opponent has left the game
		// The game is cancelled
		// Zero data message
		OpponentLeftGame,
		// The hidden picking phase commences
		HiddenPicks,
		// Deployment commences
		DeploymentStart,
		// The deployment phase is over, the enemy deployment plan is revealed
		EnemyDeployment,
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

		[ProtoMember(6, IsRequired = false)]
		public PickSubmission Picks { get; set; }

		[ProtoMember(7, IsRequired = false)]
		public DeploymentPlan DeploymentPlan { get; set; }

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

		public ClientToServerMessage(DeploymentPlan plan)
		{
			Type = ClientToServerMessageType.SubmitDeploymentPlan;
			DeploymentPlan = plan;
		}

		ClientToServerMessage(ClientToServerMessageType type, PickSubmission picks)
		{
			Type = type;
			Picks = picks;
		}

		public static ClientToServerMessage SubmitOpenPicks(PickSubmission picks)
		{
			return new ClientToServerMessage(ClientToServerMessageType.SubmitOpenPicks, picks);
		}

		public static ClientToServerMessage SubmitHiddenPicks(PickSubmission picks)
		{
			return new ClientToServerMessage(ClientToServerMessageType.SubmitHiddenPicks, picks);
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

		[ProtoMember(7, IsRequired = false)]
		public PickUpdate PickUpdate { get; set; }

		[ProtoMember(8, IsRequired = false)]
		public ArmyListing ArmyListing { get; set; }

		[ProtoMember(9, IsRequired = false)]
		public DeploymentPlan EnemeyDeploymentPlan { get; set; }

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

		public ServerToClientMessage(ArmyListing listing)
		{
			Type = ServerToClientMessageType.DeploymentStart;
			ArmyListing = listing;
		}

		public ServerToClientMessage(DeploymentPlan plan)
		{
			Type = ServerToClientMessageType.EnemyDeployment;
			EnemeyDeploymentPlan = plan;
		}

		ServerToClientMessage(ServerToClientMessageType type, PickUpdate update)
		{
			Type = type;
			PickUpdate = update;
		}

		public static ServerToClientMessage HiddenPicks(PickUpdate update)
		{
			return new ServerToClientMessage(ServerToClientMessageType.HiddenPicks, update);
		}

		public static ServerToClientMessage Deployment(PickUpdate update)
		{
			return new ServerToClientMessage(ServerToClientMessageType.DeploymentStart, update);
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
	public class TimeConfiguration
	{
		// In seconds
		[ProtoMember(1)]
		public int OpenPicks { get; set; }

		[ProtoMember(2)]
		public int HiddenPicks { get; set; }

		[ProtoMember(3)]
		public int Deployment { get; set; }

		public TimeConfiguration()
		{
			OpenPicks = 60;
			HiddenPicks = 60;
			Deployment = 60;
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
		public TimeConfiguration TimeConfiguration { get; set; }

		[ProtoMember(3)]
		public string Opponent { get; set; }

		// Points available for the open picking phase
		[ProtoMember(4)]
		public int PointsAvailable { get; set; }

		public GameStart(MapConfiguration mapConfiguration, TimeConfiguration timeConfiguration, string opponent, int pointsAvailable)
		{
			MapConfiguration = mapConfiguration;
			TimeConfiguration = timeConfiguration;
			Opponent = opponent;
			PointsAvailable = pointsAvailable;
		}
	}

	[ProtoContract]
	public class UnitConfiguration
	{
		// The ID isn't specified until after the hidden picking phase
		[ProtoMember(1, IsRequired = false)]
		public int? UnitId { get; set; }

		// The numeric ID of the faction the unit is from
		[ProtoMember(2)]
		public int FactionId { get; set; }

		// This is the numeric identifier of the type of the unit as generated from the faction configuration file
		[ProtoMember(3)]
		public int UnitTypeId { get; set; }

		[ProtoMember(4)]
		public List<int> Upgrades { get; set; }

		public UnitConfiguration(int factionId, int unitTypeId)
		{
			UnitId = null;
			FactionId = factionId;
			UnitTypeId = unitTypeId;
			Upgrades = new List<int>();
		}

		public UnitConfiguration(int factionId, int unitId, int unitTypeId)
		{
			UnitId = unitId;
			UnitTypeId = unitTypeId;
			Upgrades = new List<int>();
		}
	}

	// This data type is used for the submission of picks in both the open and the hidden picking phase
	[ProtoContract]
	public class PickSubmission
	{
		[ProtoMember(1)]
		public List<UnitConfiguration> Units { get; set; }

		public PickSubmission()
		{
			Units = new List<UnitConfiguration>();
		}
	}

	// This is transmitted once the hidden picking phase commences
	// It contains both the points left to be spent in the hidden picking phase and also units purchased by the opponent.
	[ProtoContract]
	public class PickUpdate
	{
		[ProtoMember(1)]
		public int PointsAvailable { get; set; }

		[ProtoMember(2)]
		public List<UnitConfiguration> EnemyUnits { get; set; }

		public PickUpdate(int pointsAvailable)
		{
			PointsAvailable = pointsAvailable;
			EnemyUnits = new List<UnitConfiguration>();
		}
	}

	// The army listing contains complete information about the units of both players
	// Unique unit IDs were generated for each unit
	// This is sent after the hidden picks
	[ProtoContract]
	public class ArmyListing
	{
		[ProtoMember(1)]
		public List<UnitConfiguration> OwnUnits;

		[ProtoMember(2)]
		public List<UnitConfiguration> EnemyUnits;

		public ArmyListing()
		{
			OwnUnits = new List<UnitConfiguration>();
			EnemyUnits = new List<UnitConfiguration>();
		}
	}

	[ProtoContract]
	public class Position
	{
		[ProtoMember(1)]
		public int X { get; set; }

		[ProtoMember(2)]
		public int Y { get; set; }

		public Position(int x, int y)
		{
			X = x;
			Y = y;
		}
	}

	[ProtoContract]
	public class UnitPosition
	{
		[ProtoMember(1)]
		public int UnitId { get; set; }

		[ProtoMember(2)]
		public Position Position { get; set; }

		public UnitPosition(int unitId, Position positition)
		{
			UnitId = unitId;
			Position = Position;
		}
	}

	// This data structure is not only used to submit one's own deployment but also to receive the deployment data of the enemy
	[ProtoContract]
	public class DeploymentPlan
	{
		[ProtoMember(1)]
		public List<UnitPosition> Units;

		public DeploymentPlan()
		{
			Units = new List<UnitPosition>();
		}
	}
}
