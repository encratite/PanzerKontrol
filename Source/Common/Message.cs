using ProtoBuf;

namespace WeWhoDieLikeCattle
{
	// This is used for both requests from the client and replies from the server.
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

	enum LoginReplyType
	{
		Success = 0,
		NoSuchAccount = 1,
		InvalidPassword = 2,
		GuestLoginsNotPermitted = 3,
	}

	[ProtoContract]
	class ClientToServerMessage
	{
		[ProtoMember(1)]
		public MessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public RegistrationRequest Registration { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public LoginRequest Login { get; set; }
	}

	[ProtoContract]
	class ServerToClientMessage
	{
		[ProtoMember(1)]
		public MessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public ServerWelcome ServerWelcome { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public RegistrationReplyType RegistrationReply  { get; set; }

		[ProtoMember(4, IsRequired = false)]
		public LoginReplyType LoginReply { get; set; }
	}

	[ProtoContract]
	class ServerWelcome
	{
		[ProtoMember(1)]
		public int Revision { get; set; }

		[ProtoMember(2)]
		public byte[] Salt { get; set; }
	}

	[ProtoContract]
	class RegistrationRequest
	{
		[ProtoMember(1)]
		public string Account { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public byte[] KeyHash { get; set; }
	}

	[ProtoContract]
	class LoginRequest
	{
		[ProtoMember(1)]
		public string Account { get; set; }

		// This flag is set to true if no password is provided and the user attempts to log in as a guest without having access to the persistent statistics of a registered account.
		[ProtoMember(2)]
		public bool IsGuestLogin { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public byte[] KeyHash { get; set; }
	}
}
