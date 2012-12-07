using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

using Db4objects.Db4o;
using ProtoBuf;

namespace PanzerKontrol
{
	enum PlayerLoginResult
	{
		Success,
		NotFound,
		InvalidPassword,
		AlreadyLoggedIn,
	}

	class GameServer
	{
		public const PrefixStyle Prefix = PrefixStyle.Fixed32BigEndian;

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

		IObjectContainer Database;
		GameServerConfiguration Configuration;
 
		TcpListener Listener;
		bool ShuttingDown;
		List<ClientHandler> Clients;

		GameServerState State;

		public GameServer(GameServerConfiguration configuration, IObjectContainer database)
		{
			Configuration = configuration;
			Database = database;

			Version = Assembly.GetEntryAssembly().GetName().Version.Revision;

			Listener = new TcpListener(Configuration.ServerEndpoint);
			ShuttingDown = false;
			Clients = new List<ClientHandler>();

			LoadState();
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

		public void Run()
		{
			while (!ShuttingDown)
			{
				Socket socket = Listener.AcceptSocket();
				ClientHandler client = new ClientHandler(socket, this);
				lock (Clients)
					Clients.Add(client);
			}
		}

		public bool NameHasValidLength(string name)
		{
			return name.Length <= Configuration.MaximumNameLength;
		}

		public bool NameIsInUse(string name)
		{
			lock (Clients)
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
		}

		public PlayerLoginResult PlayerLogin(string name, byte[] keyHash, out RegisteredPlayer playerOutput)
		{
			lock (Clients)
			{
				playerOutput = null;
				var registeredPlayers = Database.Query<RegisteredPlayer>(delegate(RegisteredPlayer player)
				{
					return player.Name == name;
				});
				if (registeredPlayers.Count == 0)
					return PlayerLoginResult.NotFound;
				var loggedInPlayers =
					from x in Clients
					where x.Player != null && x.Player.Name == name
					select x.Player;
				if (loggedInPlayers.Count() != 0)
					return PlayerLoginResult.AlreadyLoggedIn;
				RegisteredPlayer registeredPlayer = registeredPlayers[0];
				if (keyHash == registeredPlayer.PasswordHash)
				{
					playerOutput = registeredPlayer;
					return PlayerLoginResult.Success;
				}
				else
					return PlayerLoginResult.InvalidPassword;
			}
		}
	}
}
