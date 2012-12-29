using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using ProtoBuf;

namespace PanzerKontrol
{
	public class GameServer
	{
		public const PrefixStyle Prefix = PrefixStyle.Fixed32BigEndian;

		public readonly int Version;

		public readonly OutputManager OutputManager;

		const int PrivateKeyLength = 32;

		GameServerConfiguration Configuration;

		bool ShuttingDown;
 
		TcpListener Listener;
		X509Certificate Certificate;
		List<GameServerClient> Clients;

		List<Faction> Factions;
		List<Map> Maps;

		// The keys are the names of players used to join public games
		Dictionary<string, Game> PublicGames;

		// The keys are the randomly generated private strings required to join private games
		Dictionary<string, Game> PrivateGames;

		// Games that are currently being played
		List<Game> ActiveGames;

		#region Construction and startup

		public GameServer(GameServerConfiguration configuration, OutputManager outputManager)
		{
			OutputManager = outputManager;

			Configuration = configuration;

			ShuttingDown = false;

			Version = Assembly.GetEntryAssembly().GetName().Version.Revision;

			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(configuration.Address), configuration.Port);
			Listener = new TcpListener(endpoint);
			Certificate = new X509Certificate(configuration.CertificatePath);
			Clients = new List<GameServerClient>();

			LoadFactions();
			LoadMaps();

			PublicGames = new Dictionary<string, Game>();
			PrivateGames = new Dictionary<string, Game>();
			ActiveGames = new List<Game>();
		}

		public void Run()
		{
			while (!ShuttingDown)
			{
				Socket socket = Listener.AcceptSocket();
				lock (this)
				{
					NetworkStream stream = new NetworkStream(socket);
					SslStream secureStream = new SslStream(stream, false, AcceptAnyCertificate, null);
					secureStream.AuthenticateAsServer(Certificate, false, SslProtocols.Tls12, false);
					GameServerClient client = new GameServerClient(secureStream, this);
					Clients.Add(client);
				}
			}
		}

		#endregion

		#region Public utility functions

		public Faction GetFaction(int factionId)
		{
			if (factionId < 0 || factionId >= Factions.Count)
				throw new ClientException("Invalid faction ID specified");
			return Factions[factionId];
		}

		#endregion

		#region Client request/event handlers

		public LoginReply OnLoginRequest(LoginRequest request)
		{
			LoginReplyType replyType;
			if (!IsCompatibleClientVersion(request.ClientVersion))
				replyType = LoginReplyType.IncompatibleVersion;
			else if (NameIsTooLong(request.Name))
				replyType = LoginReplyType.NameTooLong;
			else if (NameIsInUse(request.Name))
				replyType = LoginReplyType.NameInUse;
			else
				replyType = LoginReplyType.Success;
			LoginReply reply = new LoginReply(replyType, Version);
			return reply;
		}

		public CreateGameReply OnCreateGameRequest(GameServerClient client, CreateGameRequest request, out Faction faction, out Game game)
		{
			Map map = GetMap(request.MapConfiguration.Map);
			if (map == null)
				throw new ClientException("No such map");
			faction = GetFaction(request.Army.FactionId);
			if (request.IsPrivate)
			{
				string privateKey = GeneratePrivateKey();
				game = new Game(this, client, true, privateKey, request.MapConfiguration, Configuration.TimeConfiguration, map);
				PrivateGames[privateKey] = game;
				return new CreateGameReply(privateKey);
			}
			else
			{
				game = new Game(this, client, false, null, request.MapConfiguration, Configuration.TimeConfiguration, map);
				PublicGames[client.Name] = game;
				return new CreateGameReply();
			}
		}

		public ViewPublicGamesReply OnViewPublicGamesRequest()
		{
			ViewPublicGamesReply reply = new ViewPublicGamesReply();
			foreach (var pair in PublicGames)
			{
				string ownerName = pair.Key;
				Game game = pair.Value;
				PublicGameInformation information = new PublicGameInformation(ownerName, game.MapConfiguration);
				reply.Games.Add(information);
			}
			return reply;
		}

		public void OnCancelGameRequest(GameServerClient client)
		{
			CancelGame(client);
		}

		public bool OnJoinGameRequest(GameServerClient client, JoinGameRequest request)
		{
			Game game;
			if (request.IsPrivate)
			{
				string key = request.PrivateKey;
				if (!PrivateGames.TryGetValue(key, out game))
					return false;
				PrivateGames.Remove(key);
			}
			else
			{
				string key = request.Owner;
				if (!PublicGames.TryGetValue(key, out game))
					return false;
				PublicGames.Remove(key);
			}
			game.Opponent = client;
			game.SetFirstTurn();
			game.Owner.OnGameStart(game);
			client.OnGameStart(game);
			ActiveGames.Add(game);
			game.StartTimer(game.TimeConfiguration.DeploymentTime, OnDeploymentTimerExpiration);
			return true;
		}

		public void OnLeaveGameRequest(GameServerClient client)
		{
			LeaveGame(client);	
		}

		public void OnClientTermination(GameServerClient client)
		{
			Clients.Remove(client);
			switch (client.State)
			{
				case ClientStateType.WaitingForOpponent:
					CancelGame(client);
					break;

				case ClientStateType.InGame:
					LeaveGame(client);
					break;
			}
		}

		#endregion

		#region Timer event handlers

		public void OnDeploymentTimerExpiration(Game game)
		{
			lock (this)
			{
				game.Owner.OnDeploymentTimerExpiration();
				game.Opponent.OnDeploymentTimerExpiration();
			}
		}

		#endregion

		#region Internal utility functions

		void WriteLine(string line, params object[] arguments)
		{
			OutputManager.Message(string.Format(line, arguments));
		}

		void LoadFactions()
		{
			var serialiser = new Nil.Serialiser<FactionConfiguration>(Configuration.FactionsPath);
			var configuration = serialiser.Load();
			Factions = configuration.Factions;
			int id = 0;
			foreach (Faction faction in Factions)
			{
				faction.Id = id;
				faction.SetIds();
				id++;
			}
		}

		void LoadMaps()
		{
			Maps = new List<Map>();
			throw new NotImplementedException("LoadMaps");
		}

		Map GetMap(string name)
		{
			return Maps.Find((Map x) => x.Name == name);
		}

		bool AcceptAnyCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		bool IsCompatibleClientVersion(int clientVersion)
		{
			return clientVersion == Version;
		}

		bool NameIsTooLong(string name)
		{
			return name.Length > Configuration.MaximumNameLength;
		}

		bool NameIsInUse(string name)
		{
			GameServerClient client = Clients.Find((GameServerClient x) => x.Name == name);
			return client != null;
		}

		string GetRandomString(int length)
		{
			string output = "";
			while (output.Length < length)
			{
				output += Path.GetRandomFileName();
				output = output.Replace(".", "");
			}
			output = output.Substring(0, length);
			return output;
		}

		string GeneratePrivateKey()
		{
			while (true)
			{
				string privateKey = GetRandomString(PrivateKeyLength);
				if (!PrivateGames.ContainsKey(privateKey))
					return privateKey;
			}
		}

		void CancelGame(GameServerClient client)
		{
			Game game = client.Game;
			if (game.IsPrivate)
				PrivateGames.Remove(game.Owner.Name);
			else
				PublicGames.Remove(game.PrivateKey);
		}

		void LeaveGame(GameServerClient client)
		{
			Game game = client.Game;
			game.GameOver();
			ActiveGames.Remove(game);
			client.Opponent.OnOpponentLeftGame();
		}

		#endregion
	}
}
