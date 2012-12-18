using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Db4objects.Db4o;
using ProtoBuf;

namespace PanzerKontrol
{
	public class Lobby
	{
		public readonly long GameId;
		public readonly GameServerClient Owner;
		public readonly string Description;
		public readonly bool IsPrivate;
		public readonly List<GameServerClient> Players;
		public readonly List<Team> Teams;

		// Map data missing
		// Points aren't set until the owner has chosen a number
		public readonly int? Points;

		public Lobby(long gameId, GameServerClient owner, string description, bool isPrivate)
		{
			GameId = gameId;
			Owner = owner;
			Description = description;
			IsPrivate = isPrivate;
			Players = new List<GameServerClient>();
			Players.Add(owner);
			Teams = new List<Team>();
			for (int i = 0; i < GameServer.TeamLimit; i++)
				Teams.Add(new Team());
		}

		public bool IsOnATeam(GameServerClient client)
		{
			foreach (Team team in Teams)
			{
				if(team.Includes(client.Player))
					return true;
			}
			return false;
		}

		public void RemovePlayer(GameServerClient client)
		{
			foreach (Team team in Teams)
				team.Remove(client.Player);
		}

		public TeamPlayer GetPlayer(GameServerClient client)
		{
			foreach (Team team in Teams)
			{
				TeamPlayer player = team.Get(client.Player);
				if (player != null)
					return player;
			}
			return null;
		}

		public void AddPlayer(TeamPlayer player, int teamId)
		{
			Teams[teamId].Players.Add(player);
		}

		public void JoinTeam(GameServerClient client, int teamId, Faction defaultFaction)
		{
			TeamPlayer player = GetPlayer(client);
			if (player == null)
				player = new TeamPlayer(client, defaultFaction);
			else
				RemovePlayer(client);
			Teams[teamId].Players.Add(player);
		}
	}

	public class Team
	{
		public readonly List<TeamPlayer> Players;

		public Team()
		{
			Players = new List<TeamPlayer>();
		}

		public TeamPlayer Get(Player player)
		{
			TeamPlayer teamPlayer = Players.Find((TeamPlayer x) => object.ReferenceEquals(x.Player, player));
			return teamPlayer;
		}

		public bool Includes(Player player)
		{
			return Get(player) != null;
		}

		public void Remove(Player player)
		{
			Players.RemoveAll((TeamPlayer x) => object.ReferenceEquals(x.Player, player));
		}
	}

	public class TeamPlayer
	{
		public readonly GameServerClient Player;
		public readonly Faction Faction;

		public TeamPlayer(GameServerClient player, Faction faction)
		{
			Player = player;
			Faction = faction;
		}
	}

	public class GameServer
	{
		public const int TeamLimit = 2;

		public const PrefixStyle Prefix = PrefixStyle.Fixed32BigEndian;
		public const int SaltSize = KeyHashSize;
		// SHA-2, 512 bits
		const int KeyHashSize = 512 / 8;

		public readonly int Version;

		public byte[] Salt
		{
			get
			{
				return Configuration.Salt;
			}
		}

		public bool EnableGuestLogin
		{
			get
			{
				return Configuration.EnableGuestLogin;
			}
		}

		GameServerConfiguration Configuration;
		IObjectContainer Database;
 
		TcpListener Listener;
		X509Certificate Certificate;
		bool ShuttingDown;
		List<GameServerClient> Clients;

		GameServerState State;

		List<Faction> Factions;

		List<Lobby> Lobbies;

		public GameServer(GameServerConfiguration configuration, IObjectContainer database)
		{
			Configuration = configuration;
			Database = database;

			Version = Assembly.GetEntryAssembly().GetName().Version.Revision;

			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(configuration.Address), configuration.Port);
			Listener = new TcpListener(endpoint);
			Certificate = new X509Certificate(configuration.CertificatePath);
			ShuttingDown = false;
			Clients = new List<GameServerClient>();

			LoadState();
			LoadFactions();

			Lobbies = new List<Lobby>();
		}

		void LoadState()
		{
			var result = Database.QueryByExample(typeof(GameServerState));
			if (result.Count == 0)
			{
				State = new GameServerState();
				Database.Store(State);
			}
			else
				State = (GameServerState)result.Next();
		}

		void LoadFactions()
		{
			var serialiser = new Nil.Serialiser<UnitConfiguration>(Configuration.FactionsPath);
			var configuration = serialiser.Load();
			Factions = configuration.Factions;
			int id = 0;
			foreach (Faction faction in Factions)
			{
				faction.Id = id;
				id++;
			}
		}

		public void Run()
		{
			while (!ShuttingDown)
			{
				Socket socket = Listener.AcceptSocket();
				NetworkStream stream = new NetworkStream(socket);
				SslStream secureStream = new SslStream(stream, false, AcceptAnyCertificate, null);
				secureStream.AuthenticateAsServer(Certificate, false, SslProtocols.Tls12, false);
				GameServerClient client = new GameServerClient(secureStream, this);
				lock (Clients)
					Clients.Add(client);
			}
		}

		bool AcceptAnyCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		public bool NameHasValidLength(string name)
		{
			return name.Length <= Configuration.MaximumNameLength;
		}

		public bool NameIsInUse(string name)
		{
			var registeredPlayers = Database.Query<RegisteredPlayer>(delegate(RegisteredPlayer player)
			{
				return player.Name == name;
			});
			if (registeredPlayers.Count > 0)
				return false;
			var unregisteredPlayers =
				from x in Clients
				where x.Player != null && x.Player.Name == name
				select x.Player;
			return unregisteredPlayers.Count() > 0;
		}

		public LoginReplyType ProcessGuestPlayerLoginRequest(LoginRequest login, out GuestPlayer player)
		{
			player = null;

			if (EnableGuestLogin)
			{
				if (NameHasValidLength(login.Name))
				{
					if (NameIsInUse(login.Name))
						return LoginReplyType.GuestNameTaken;
					else
					{
						player = new GuestPlayer(GeneratePlayerId(), login.Name);
						return LoginReplyType.Success;
					}
				}
				else
					return LoginReplyType.GuestNameTooLong;
			}
			else
				return LoginReplyType.GuestLoginNotPermitted;
		}

		public LoginReplyType ProcessRegisteredPlayerLoginRequest(LoginRequest login, out RegisteredPlayer playerOutput)
		{
			lock (Clients)
			{
				playerOutput = null;
				var registeredPlayers = Database.Query<RegisteredPlayer>(delegate(RegisteredPlayer registeredPlayer)
				{
					return registeredPlayer.Name == login.Name;
				});
				if (registeredPlayers.Count == 0)
					return LoginReplyType.NotFound;
				var loggedInPlayers =
					from x in Clients
					where x.Player != null && x.Player.Name == login.Name
					select x.Player;
				if (loggedInPlayers.Count() != 0)
					return LoginReplyType.AlreadyLoggedIn;
				RegisteredPlayer player = registeredPlayers[0];
				if (login.KeyHash == player.KeyHash)
				{
					playerOutput = player;
					return LoginReplyType.Success;
				}
				else
					return LoginReplyType.InvalidPassword;
			}
		}

		bool PlayerIdIsInUse(long id)
		{
			lock (Clients)
			{
				var activePlayers =
					from x in Clients
					where x.Player != null && x.Player.Id == id
					select x.Player;
				if (activePlayers.Count() > 0)
					return true;
				var registeredPlayers = Database.Query<RegisteredPlayer>(delegate(RegisteredPlayer player)
				{
					return player.Id == id;
				});
				return registeredPlayers.Count > 0;
			}
		}

		long GeneratePlayerId()
		{
			while (true)
			{
				long id = State.GetPlayerId();
				if (!PlayerIdIsInUse(id))
				{
					Database.Store(State);
					return id;
				}
			}
		}

		public RegistrationReplyType RegisterPlayer(RegistrationRequest request)
		{
			lock (Clients)
			{
				if (!Configuration.EnableUserRegistration)
					return RegistrationReplyType.RegistrationDisabled;
				if (NameIsInUse(request.Name))
					return RegistrationReplyType.NameTaken;
				if (request.KeyHash.Length != KeyHashSize)
					return RegistrationReplyType.WrongKeyHashSize;
				RegisteredPlayer player = new RegisteredPlayer(GeneratePlayerId(), request.Name, request.KeyHash);
				Database.Store(player);
				return RegistrationReplyType.Success;
			}
		}

		public CreateLobbyReply CreateLobby(GameServerClient client, CreateLobbyRequest request)
		{
			lock (Lobbies)
			{
				long gameId = State.GetGameId();
				Lobby lobby = new Lobby(gameId, client, request.Description, request.IsPrivate);
				Lobbies.Add(lobby);
				CreateLobbyReply reply = new CreateLobbyReply(gameId);
				return reply;
			}
		}

		public ViewLobbiesReply ViewLobbies()
		{
			lock (Lobbies)
			{
				ViewLobbiesReply reply = new ViewLobbiesReply();
				foreach (Lobby lobby in Lobbies)
				{
					// Conceal private lobbies
					if (lobby.IsPrivate)
						continue;
					GameInformation information = new GameInformation(lobby);
					reply.Lobbies.Add(information);
				}
				return reply;
			}
		}

		void UpdateGameInformation(Lobby lobby, GameServerClient excludedClient = null)
		{
			foreach (GameServerClient client in lobby.Players)
			{
				if (excludedClient != null && object.ReferenceEquals(excludedClient, client))
					continue;
				DetailedGameInformation gameInformation = new DetailedGameInformation(lobby);
				ServerToClientMessage message = new ServerToClientMessage(gameInformation);
				client.SendMessage(message);
			}
		}

		public JoinLobbyReply JoinLobby(GameServerClient client, JoinLobbyRequest request, out Lobby lobby)
		{
			lock (Lobbies)
			{
				lobby = Lobbies.Find((Lobby x) => x.GameId == request.GameId);
				if (lobby == null)
					return new JoinLobbyReply(JoinLobbyReplyType.LobbyDoesNotExist);
				if(lobby.IsPrivate)
					return new JoinLobbyReply(JoinLobbyReplyType.NeedInvitation);
				lobby.Players.Add(client);
				UpdateGameInformation(lobby, client);
				return new JoinLobbyReply(lobby);
			}
		}

		public void JoinTeam(GameServerClient client, Lobby lobby, JoinTeamRequest request)
		{
			Faction defaultFaction = Factions.First();
			if (request.PlayerId == null)
			{
				// The player himself is requesting to join a team
				lobby.JoinTeam(client, request.NewTeamId, defaultFaction);
			}
			else
			{
				// It's a request to move another player to a certain team
				if (!object.ReferenceEquals(client, lobby.Owner))
					throw new ClientException("You are not the owner");
				throw new Exception("Not fully implemented");
			}
		}
	}
}
