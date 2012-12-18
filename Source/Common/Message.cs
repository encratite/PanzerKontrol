using System.Collections.Generic;

using ProtoBuf;

namespace PanzerKontrol
{
	public enum ClientToServerMessageType
	{
		LoginRequest,
		CreateGameRequest,
	}

	public enum ServerToClientMessageType
	{
		LoginReply,
		CreateGameReply,
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
		public LoginRequest LoginRequest { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public CreateGameRequest CreateGameRequest { get; set; }

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
	}

	[ProtoContract]
	public class ServerToClientMessage
	{
		[ProtoMember(1)]
		public ServerToClientMessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public LoginReply LoginReply { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public CreateGameReply CreateGameReply { get; set; }

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
		[ProtoMember(1)]
		public bool IsPrivate;

		public CreateGameRequest(bool isPrivate)
		{
			IsPrivate = isPrivate;
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
}
