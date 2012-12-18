using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

using ProtoBuf;

namespace PanzerKontrol
{
	enum ClientState
	{
		Connected,
		LoggedIn,
		InGame,
	}

	delegate void MessageHandler(ClientToServerMessage message);

	public class GameServerClient
	{
		GameServer Server;
		SslStream Stream;
		Thread Thread;

		ClientState ClientState;

		ClientToServerMessageType[] ExpectedMessageTypes;
		Dictionary<ClientToServerMessageType, MessageHandler> MessageHandlerMap;

		public GameServerClient(SslStream stream, GameServer server)
		{
			Stream = stream;
			Server = server;
			ClientState = ClientState.Connected;

			ExpectedMessageTypes = null;
			MessageHandlerMap = new Dictionary<ClientToServerMessageType, MessageHandler>();

			InitialiseMessageHandlerMap();
		}

		public void Process()
		{
			Thread = new Thread(Run);
			Thread.Start();
		}

		void InitialiseMessageHandlerMap()
		{
		}

		void SetExpectedMessageTypes(params ClientToServerMessageType[] expectedMessageTypes)
		{
			ExpectedMessageTypes = expectedMessageTypes;
		}

		bool IsExpectedMessageType(ClientToServerMessageType type)
		{
			if (ExpectedMessageTypes == null)
				return true;
			return Array.IndexOf(ExpectedMessageTypes, type) >= 0;
		}

		void Run()
		{
			//SetExpectedMessageTypes(ClientToServerMessageType.WelcomeRequest);

			var enumerator = Serializer.DeserializeItems<ClientToServerMessage>(Stream, GameServer.Prefix, 0);
			foreach (var message in enumerator)
				ProcessMessage(message);
		}

		public void SendMessage(ServerToClientMessage message)
		{
			Serializer.Serialize<ServerToClientMessage>(Stream, message);
		}

		void ProcessMessage(ClientToServerMessage message)
		{
			if (!IsExpectedMessageType(message.Type))
				throw new Exception(string.Format("Received an unexpected message type: {0}", message.Type));
			MessageHandler handler;
			if(!MessageHandlerMap.TryGetValue(message.Type, out handler))
				throw new Exception("Encountered an unknown server to client message type");
		}
	}
}
