using System;
using System.Collections.Generic;
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
 
		TcpListener Listener;
		X509Certificate Certificate;
		bool ShuttingDown;
		List<GameServerClient> Clients;

		List<Faction> Factions;

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
	}
}
