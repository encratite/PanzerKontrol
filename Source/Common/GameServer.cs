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

		const int PrivateKeyLength = 32;

		GameServerConfiguration Configuration;
 
		TcpListener Listener;
		X509Certificate Certificate;
		bool ShuttingDown;
		List<GameServerClient> Clients;

		List<Faction> Factions;

		// The keys are the names of players used to join public games.
		Dictionary<string, Game> PublicGames;

		// The keys are the randomly generated private strings required to join private games.
		Dictionary<string, Game> PrivateGames;

		public GameServer(GameServerConfiguration configuration)
		{
			Configuration = configuration;

			Version = Assembly.GetEntryAssembly().GetName().Version.Revision;

			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(configuration.Address), configuration.Port);
			Listener = new TcpListener(endpoint);
			Certificate = new X509Certificate(configuration.CertificatePath);
			ShuttingDown = false;
			Clients = new List<GameServerClient>();

			LoadFactions();

			PublicGames = new Dictionary<string, Game>();
			PrivateGames = new Dictionary<string, Game>();
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

		public LoginReply Login(LoginRequest request)
		{
			lock (Clients)
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
		}

		public void OnClientTermination(GameServerClient client)
		{
			Clients.Remove(client);
			throw new MissingFeatureException("If a game is going on, it needs to be shut down gracefully");
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

		public CreateGameReply CreateGame(GameServerClient client, CreateGameRequest request, out Faction faction, out Game game)
		{
			lock (Clients)
			{
				faction = GetFaction(request.FactionId);
				if (request.IsPrivate)
				{
					string privateKey = GeneratePrivateKey();
					game = new Game(client, true, privateKey, request.MapConfiguration);
					PrivateGames[privateKey] = game;
					return new CreateGameReply(privateKey);
				}
				else
				{
					game = new Game(client, false, null, request.MapConfiguration);
					PublicGames[client.Name] = game;
					return new CreateGameReply();
				}
			}
		}

		public ViewPublicGamesReply ViewPublicGames()
		{
			lock (Clients)
			{
				ViewPublicGamesReply reply = new ViewPublicGamesReply();
				foreach (var pair in PublicGames)
				{
					string ownerName = pair.Key;
					Game game = pair.Value;
					MapConfiguration configuration = new MapConfiguration(game.Map, game.Points);
					PublicGameInformation information = new PublicGameInformation(ownerName, configuration);
					reply.Games.Add(information);
				}
				return reply;
			}
		}

		public void CancelGame(GameServerClient client)
		{
			lock (Clients)
			{
				Game game = client.Game;
				if (game.IsPrivate)
					PrivateGames.Remove(game.Owner.Name);
				else
					PublicGames.Remove(game.PrivateKey);
			}
		}

		public Faction GetFaction(int factionId)
		{
			if (factionId >= Factions.Count)
				throw new ClientException("Invalid faction ID specified");
			return Factions[factionId];
		}
	}
}
