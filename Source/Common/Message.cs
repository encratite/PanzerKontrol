using ProtoBuf;

namespace WeWhoDieLikeCattle
{
	enum MessageType
	{
		Welcome = 0,
		Register = 1,
		Login = 2,
	}

	enum RegistrationReplyType
	{
		Success = 0,
		NameTaken = 1,
		InvalidName = 2,
		RegistrationDisabled = 3,
	}

	[ProtoContract]
	class ClientToServerMessage
	{
		[ProtoMember(1)]
		public MessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public Login Login { get; set; }
	}

	[ProtoContract]
	class ServerToClientMessage
	{
		[ProtoMember(1)]
		public MessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public ServerWelcome ServerWelcome { get; set; }
	}

	[ProtoContract]
	class ServerWelcome
	{
		[ProtoMember(1)]
		public int Revision { get; set; }
		[ProtoMember(2)]
		public string Salt { get; set; }
	}

	[ProtoContract]
	class RegistrationReply
	{
		[ProtoMember(1)]
		public RegistrationReplyType Type { get; set; }
	}

	// Used for both registration and login.
	[ProtoContract]
	class Login
	{
		[ProtoMember(1)]
		public string Account { get; set; }
		[ProtoMember(2)]
		public byte[] PasswordHash { get; set; }
	}
}
