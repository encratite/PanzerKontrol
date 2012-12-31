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
		// Submit the deployment plan
		SubmitDeploymentPlan,
		// Move a unit
		MoveUnit,
		// Attack a unit
		AttackUnit,
		// End the current turn
		// Zero data message
		EndTurn,
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
		// The deployment phase is over, the enemy deployment plan is revealed
		EnemyDeployment,
		// A new turn starts
		NewTurn,
		// A unit moved
		UnitMove,
		// An attack occurred
		UnitAttack,
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
		public ClientToServerMessageType Type;

		[ProtoMember(2, IsRequired = false)]
		public ErrorMessage ErrorMessage;

		[ProtoMember(3, IsRequired = false)]
		public LoginRequest LoginRequest;

		[ProtoMember(4, IsRequired = false)]
		public CreateGameRequest CreateGameRequest;

		[ProtoMember(5, IsRequired = false)]
		public JoinGameRequest JoinGameRequest;

		[ProtoMember(6, IsRequired = false)]
		public DeploymentPlan DeploymentPlan;

		[ProtoMember(7, IsRequired = false)]
		public MoveUnitRequest MoveUnitRequest;

		[ProtoMember(8, IsRequired = false)]
		public AttackUnitRequest AttackUnitRequest;

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

		public ClientToServerMessage(MoveUnitRequest request)
		{
			Type = ClientToServerMessageType.MoveUnit;
			MoveUnitRequest = request;
		}

		public ClientToServerMessage(AttackUnitRequest request)
		{
			Type = ClientToServerMessageType.AttackUnit;
			AttackUnitRequest = request;
		}
	}

	[ProtoContract]
	public class ServerToClientMessage
	{
		[ProtoMember(1)]
		public ServerToClientMessageType Type;

		[ProtoMember(2, IsRequired = false)]
		public ErrorMessage ErrorMessage;

		[ProtoMember(3, IsRequired = false)]
		public LoginReply LoginReply;

		[ProtoMember(4, IsRequired = false)]
		public CreateGameReply CreateGameReply;

		[ProtoMember(5, IsRequired = false)]
		public ViewPublicGamesReply ViewPublicGamesReply;

		[ProtoMember(6, IsRequired = false)]
		public GameStart GameStart;

		[ProtoMember(7, IsRequired = false)]
		public DeploymentPlan EnemeyDeploymentPlan;

		[ProtoMember(8, IsRequired = false)]
		public NewTurn NewTurn;

		[ProtoMember(9, IsRequired = false)]
		public UnitMove UnitMove;

		[ProtoMember(10, IsRequired = false)]
		public UnitAttack AttackUnitReply;

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

		public ServerToClientMessage(GameStart start)
		{
			Type = ServerToClientMessageType.GameStart;
			GameStart = start;
		}

		public ServerToClientMessage(DeploymentPlan plan)
		{
			Type = ServerToClientMessageType.EnemyDeployment;
			EnemeyDeploymentPlan = plan;
		}

		public ServerToClientMessage(NewTurn newTurn)
		{
			Type = ServerToClientMessageType.NewTurn;
			NewTurn = newTurn;
		}

		public ServerToClientMessage(UnitMove move)
		{
			Type = ServerToClientMessageType.UnitMove;
			UnitMove = move;
		}

		public ServerToClientMessage(UnitAttack attack)
		{
			Type = ServerToClientMessageType.UnitAttack;
			AttackUnitReply = attack;
		}
	}

	[ProtoContract]
	public class ErrorMessage
	{
		[ProtoMember(1)]
		public string Message;

		public ErrorMessage(string message)
		{
			Message = message;
		}
	}

	[ProtoContract]
	public class LoginRequest
	{
		[ProtoMember(1)]
		public string Name;

		[ProtoMember(2)]
		public int ClientVersion;

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
		public LoginReplyType Type;

		[ProtoMember(2)]
		public int ServerVersion;

		public LoginReply(LoginReplyType type, int version)
		{
			Type = type;
			ServerVersion = version;
		}
	}

	[ProtoContract]
	public class CreateGameRequest
	{
		[ProtoMember(1)]
		public BaseArmy Army;

		[ProtoMember(2)]
		public bool IsPrivate;

		[ProtoMember(3)]
		public GameConfiguration GameConfiguration;

		public CreateGameRequest(BaseArmy army, bool isPrivate, GameConfiguration gameConfiguration)
		{
			Army = army;
			IsPrivate = isPrivate;
			GameConfiguration = gameConfiguration;
		}
	}

	[ProtoContract]
	public class GameConfiguration
	{
		// This is a part of the filename of the map
		[ProtoMember(1)]
		public string Map;

		// The number of points that may be spent during the picking phase
		[ProtoMember(2)]
		public int Points;

		// The number of seconds players are given to submit a deployment plan
		[ProtoMember(3)]
		public int DeploymentTime;

		// The number of seconds players are given to finish a turn
		[ProtoMember(4)]
		public int TurnTime;

		public GameConfiguration(string map, int points, int deploymentTime, int turnTime)
		{
			Map = map;
			Points = points;
			DeploymentTime = deploymentTime;
			TurnTime = turnTime;
		}
	}

	[ProtoContract]
	public class CreateGameReply
	{
		[ProtoMember(1, IsRequired = false)]
		public string PrivateKey;

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
		public string Owner;

		[ProtoMember(2)]
		public GameConfiguration GameConfiguration;

		public PublicGameInformation(string owner, GameConfiguration gameConfiguration)
		{
			Owner = owner;
			GameConfiguration = gameConfiguration;
		}
	}

	[ProtoContract]
	public class ViewPublicGamesReply
	{
		[ProtoMember(1)]
		public List<PublicGameInformation> Games;

		public ViewPublicGamesReply()
		{
			Games = new List<PublicGameInformation>();
		}
	}

	[ProtoContract]
	public class JoinGameRequest
	{
		[ProtoMember(1)]
		public BaseArmy Army;

		[ProtoMember(2)]
		public bool IsPrivate;

		// Public games are joined based on the name of the owner
		[ProtoMember(3, IsRequired = false)]
		public string Owner;

		// Private games joined using the private key that was shared
		[ProtoMember(4, IsRequired = false)]
		public string PrivateKey;

		private JoinGameRequest(BaseArmy army, bool isPrivate, string owner, string privateKey)
		{
			Army = army;
			IsPrivate = isPrivate;
			Owner = owner;
			PrivateKey = privateKey;
		}

		public static JoinGameRequest JoinPublicGame(BaseArmy army, string owner)
		{
			return new JoinGameRequest(army, false, owner, null);
		}

		public static JoinGameRequest JoinPrivateGame(BaseArmy army, string privateKey)
		{
			return new JoinGameRequest(army, false, null, privateKey);
		}
	}

	[ProtoContract]
	public class GameStart
	{
		[ProtoMember(1)]
		public GameConfiguration GameConfiguration;

		[ProtoMember(2)]
		public BaseArmy MyArmy;

		[ProtoMember(3)]
		public BaseArmy EnemyArmy;

		[ProtoMember(4)]
		public string Opponent;

		[ProtoMember(5)]
		public int ReinforcementPoints;

		public GameStart(GameConfiguration gameConfiguration, BaseArmy myArmy, BaseArmy enemyArmy, string opponent, int reinforcementPoints)
		{
			GameConfiguration = gameConfiguration;

			MyArmy = myArmy;
			EnemyArmy = enemyArmy;

			Opponent = opponent;

			ReinforcementPoints = reinforcementPoints;
		}
	}

	[ProtoContract]
	public class UnitConfiguration
	{
		// The ID isn't specified when the client sends the base army to the server
		[ProtoMember(1, IsRequired = false)]
		public int? UnitId;

		// The numeric ID of the faction the unit is from
		[ProtoMember(2)]
		public int FactionId;

		// This is the numeric identifier of the type of the unit as generated from the faction configuration file
		[ProtoMember(3)]
		public int UnitTypeId;

		[ProtoMember(4)]
		public List<int> Upgrades;

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

	[ProtoContract]
	public class BaseArmy
	{
		[ProtoMember(1)]
		public int FactionId;

		[ProtoMember(2)]
		public List<UnitConfiguration> Units;

		public BaseArmy(Faction faction, List<Unit> units)
		{
			FactionId = faction.Id.Value;
			Units = new List<UnitConfiguration>();
			foreach (var unit in units)
			{
				UnitConfiguration unitConfiguration = new UnitConfiguration(FactionId, unit.Type.Id.Value);
				foreach (var upgrade in unit.Upgrades)
					unitConfiguration.Upgrades.Add(upgrade.Id.Value);
				Units.Add(unitConfiguration);
			}
		}
	}

	[ProtoContract]
	public class Position
	{
		[ProtoMember(1)]
		public int X;

		[ProtoMember(2)]
		public int Y;

		public int Z
		{
			get
			{
				return -X - Y;
			}
		}

		public Position(int x, int y)
		{
			X = x;
			Y = y;
		}

		public static Position operator +(Position a, Position b)
		{
			return new Position(a.X + b.X, a.Y + b.Y);
		}
	}

	[ProtoContract]
	public class UnitPosition
	{
		[ProtoMember(1)]
		public int UnitId;

		[ProtoMember(2)]
		public Position Position;

		public UnitPosition(int unitId, Position positition)
		{
			UnitId = unitId;
			Position = positition;
		}
	}

	// This data structure is not only used to submit one's own deployment but also to receive the deployment data of the enemy
	[ProtoContract]
	public class DeploymentPlan
	{
		[ProtoMember(1)]
		public bool RequestedFirstTurn;

		[ProtoMember(2)]
		public List<UnitPosition> Units;

		public DeploymentPlan(bool requestedFirstTurn)
		{
			RequestedFirstTurn = requestedFirstTurn;
			Units = new List<UnitPosition>();
		}
	}

	[ProtoContract]
	public class NewTurn
	{
		[ProtoMember(1)]
		public PlayerIdentifier ActivePlayer;

		public NewTurn(PlayerIdentifier activePlayer)
		{
			ActivePlayer = activePlayer;
		}
	}

	[ProtoContract]
	public class MoveUnitRequest
	{
		[ProtoMember(1)]
		public int UnitId;

		[ProtoMember(2)]
		public Position NewPosition;

		public MoveUnitRequest(int unitId, Position newPosition)
		{
			UnitId = unitId;
			NewPosition = newPosition;
		}
	}

	[ProtoContract]
	public class UnitMove
	{
		// This is redundant, but whatever
		[ProtoMember(1)]
		public int UnitId;

		[ProtoMember(2)]
		public int RemainingMovementPoints;

		public UnitMove(int unitId, int remainingMovementPoints)
		{
			UnitId = unitId;
			RemainingMovementPoints = remainingMovementPoints;
		}
	}

	[ProtoContract]
	public class AttackUnitRequest
	{
		[ProtoMember(1)]
		public int AttackerUnitId;

		[ProtoMember(2)]
		public int DefenderUnitId;

		public AttackUnitRequest(int attackerUnitId, int defenderUnitId)
		{
			AttackerUnitId = attackerUnitId;
			DefenderUnitId = defenderUnitId;
		}
	}

	[ProtoContract]
	public class UnitCasualties
	{
		[ProtoMember(1)]
		public int UnitId;

		[ProtoMember(2)]
		public double NewStrength;

		public UnitCasualties(int unitId, double newStrength)
		{
			UnitId = unitId;
			NewStrength = newStrength;
		}
	}

	[ProtoContract]
	public class UnitAttack
	{
		[ProtoMember(1)]
		public UnitCasualties AttackerCasualties;

		[ProtoMember(2)]
		public UnitCasualties DefenderCasualties;

		public UnitAttack(UnitCasualties attackerCasualties, UnitCasualties defenderCasualties)
		{
			AttackerCasualties = attackerCasualties;
			DefenderCasualties = defenderCasualties;
		}
	}
}
