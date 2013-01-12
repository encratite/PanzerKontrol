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
	public class Server
	{
		public const PrefixStyle Prefix = PrefixStyle.Fixed32BigEndian;

		public readonly int Version;

		public readonly OutputManager OutputManager;

		const int PrivateKeyLength = 32;

		ServerConfiguration Configuration;
		bool UseTLS;

		bool ShuttingDown;
 
		TcpListener Listener;
		X509Certificate Certificate;
		List<ServerClient> Clients;

		List<Faction> Factions;
		List<Map> Maps;

		// The keys are the names of players used to join public games
		Dictionary<string, ServerGame> PublicGames;

		// The keys are the randomly generated private strings required to join private games
		Dictionary<string, ServerGame> PrivateGames;

		// Games that are currently being played
		List<Game> ActiveGames;

		#region Construction and startup

		public Server(ServerConfiguration configuration, OutputManager outputManager)
		{
			OutputManager = outputManager;

			Configuration = configuration;
			UseTLS = configuration.CertificatePath != null;

			ShuttingDown = false;

			Version = Assembly.GetEntryAssembly().GetName().Version.Revision;

			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(configuration.Address), configuration.Port);
			Listener = new TcpListener(endpoint);
			if (UseTLS)
				Certificate = new X509Certificate(configuration.CertificatePath);
			else
				Certificate = null;
			Clients = new List<ServerClient>();

			LoadFactions();
			LoadMaps();

			PublicGames = new Dictionary<string, ServerGame>();
			PrivateGames = new Dictionary<string, ServerGame>();
			ActiveGames = new List<Game>();
		}

		public void Run()
		{
			while (!ShuttingDown)
			{
				Socket socket = Listener.AcceptSocket();
				lock (this)
				{
					Stream clientStream;
					NetworkStream networkStream = new NetworkStream(socket);
					if (UseTLS)
					{
						SslStream secureStream = new SslStream(networkStream, false, AcceptAnyCertificate, null);
						secureStream.AuthenticateAsServer(Certificate, false, SslProtocols.Tls12, false);
						clientStream = secureStream;
					}
					else
						clientStream = networkStream;
					ServerClient client = new ServerClient(clientStream, this);
					Clients.Add(client);
				}
			}
		}

		#endregion

		#region Public utility functions

		public Faction GetFaction(int factionId)
		{
			if (factionId < 0 || factionId >= Factions.Count)
				throw new ServerClientException("Invalid faction ID specified");
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

		public CreateGameReply OnCreateGameRequest(ServerClient client, CreateGameRequest request, out ServerGame game)
		{
			Map map = GetMap(request.GameConfiguration.Map);
			if (map == null)
				throw new ServerClientException("No such map");
			ValidateGameConfiguration(request.GameConfiguration);
			if (request.IsPrivate)
			{
				string privateKey = GeneratePrivateKey();
				game = new ServerGame(this, client, true, privateKey, request.GameConfiguration, map);
				PrivateGames[privateKey] = game;
				return new CreateGameReply(privateKey);
			}
			else
			{
				game = new ServerGame(this, client, false, null, request.GameConfiguration, map);
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
				PublicGameInformation information = new PublicGameInformation(ownerName, game.GameConfiguration);
				reply.Games.Add(information);
			}
			return reply;
		}

		public void OnCancelGameRequest(ServerClient client)
		{
			CancelGame(client);
		}

		public bool OnJoinGameRequest(ServerClient client, JoinGameRequest request, out ServerGame game)
		{
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
			
			ActiveGames.Add(game);
			game.StartDeploymentTimer();
			return true;
		}

		public void OnClientTermination(ServerClient client)
		{
			Clients.Remove(client);
			switch (client.State)
			{
				case ClientState.WaitingForOpponent:
					CancelGame(client);
					break;

				case ClientState.InGame:
					OnGameEnd(client.Game, GameOutcomeType.Desertion, client.Opponent);
					break;

				default:
					// Not an issue
					break;
			}
		}

		public void OnGameEnd(ServerGame game, GameOutcomeType outcome, ServerClient winner = null)
		{
			ActiveGames.Remove(game);
			game.EndGame(new GameEnd(outcome, winner));
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
			ServerClient client = Clients.Find((ServerClient x) => x.Name == name);
			return client != null;
		}

		string GetRandomString(int length)
		{
			string output = "";
			while (output.Length < length)
			{
				output += System.IO.Path.GetRandomFileName();
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

		void CancelGame(ServerClient client)
		{
			ServerGame game = client.Game;
			if (game.IsPrivate)
				PrivateGames.Remove(game.Owner.Name);
			else
				PublicGames.Remove(game.PrivateKey);
		}

		void ValidateGameConfiguration(GameConfiguration configuration)
		{
			if (configuration.Points < GameConstants.PointsMinimum)
				throw new ServerClientException("Number of points specified too low");
			if (configuration.Points > GameConstants.PointsMaximum)
				throw new ServerClientException("Number of points specified too high");
			if (configuration.TurnLimit < GameConstants.TurnLimitMinimum)
				throw new ServerClientException("Maximum number of turns specified too low");
			if (configuration.TurnLimit > GameConstants.TurnLimitMaximum)
				throw new ServerClientException("Maximum number of turns specified too high");
			if (configuration.DeploymentTime > GameConstants.DeploymentTimeMinimum)
				throw new ServerClientException("Deployment time limit specified too low");
			if (configuration.DeploymentTime > GameConstants.DeploymentTimeMaximum)
				throw new ServerClientException("Deployment time limit specified too high");
			if (configuration.TurnTime > GameConstants.TurnTimeMinimum)
				throw new ServerClientException("Deployment time limit specified too low");
			if (configuration.TurnTime > GameConstants.TurnTimeMaximum)
				throw new ServerClientException("Deployment time limit specified too high");
		}

		#endregion
	}
}
