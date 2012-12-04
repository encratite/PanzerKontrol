using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

using ProtoBuf;

namespace WeWhoDieLikeCattle
{
	class GameServer
	{
		public const PrefixStyle Prefix = PrefixStyle.Fixed32BigEndian;

		public readonly int Version;
		public readonly byte[] Salt;
 
		TcpListener Listener;
		bool ShuttingDown;
		List<ClientHandler> Clients;

		public GameServer(IPEndPoint endpoint, byte[] salt)
		{
			Version = Assembly.GetEntryAssembly().GetName().Version.Revision;
			Salt = salt;

			Listener = new TcpListener(endpoint);
			ShuttingDown = false;
			Clients = new List<ClientHandler>();
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
	}
}
