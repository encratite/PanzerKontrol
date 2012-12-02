using System.Collections.Generic;

using ProtoBuf;

namespace WeWhoDieLikeCattle
{
	// This is used for both requests from the client and replies from the server.
	enum MessageType
	{
		Welcome = 0,
		Register = 1,
		Login = 2,
		CustomGames = 3,
		CreateGame = 4,
		JoinGame = 5,
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

	enum CreateGameReplyType
	{
		Success = 0,
		NameTaken = 1,
		InvalidName = 2,
	}

	enum JoinGameReplyType
	{
		Success = 0,
		GameIsFull = 1,
		GameDoesNotExist = 2,
		PasswordRequired = 3,
		WrongPassword = 4,
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

		[ProtoMember(4, IsRequired = false)]
		public CreateGameRequest CreateGame { get; set; }
	}

	[ProtoContract]
	class ServerToClientMessage
	{
		[ProtoMember(1)]
		public MessageType Type { get; set; }

		[ProtoMember(2, IsRequired = false)]
		public ServerWelcome ServerWelcome { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public RegistrationReplyType? RegistrationReply  { get; set; }

		[ProtoMember(4, IsRequired = false)]
		public LoginReplyType? LoginReply { get; set; }

		[ProtoMember(5, IsRequired = false)]
		public CustomGamesReply CustomGames { get; set; }

		[ProtoMember(6, IsRequired = false)]
		public CreateGameReplyType? CreateGameReply { get; set; }

		[ProtoMember(7, IsRequired = false)]
		public JoinGameReply JoinGameReply { get; set; }
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

	[ProtoContract]
	class CustomGamesReply
	{
		[ProtoMember(1)]
		public List<CustomGameEntry> Games { get; set; }
	}

	[ProtoContract]
	class CustomGameEntry
	{
		[ProtoMember(1)]
		public int GameId { get; set; }

		[ProtoMember(2)]
		public string Description { get; set; }

		[ProtoMember(3)]
		public bool HasPassword { get; set; }

		[ProtoMember(4)]
		public string CreatorName { get; set; }
	}

	[ProtoContract]
	class CreateGameRequest
	{
		[ProtoMember(1)]
		public string Description { get; set; }

		[ProtoMember(2)]
		public bool HasPassword { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public byte[] KeyHash { get; set; }
	}

	[ProtoContract]
	class JoinGameRequest
	{
		[ProtoMember(1)]
		public int GameId { get; set; }

		[ProtoMember(2)]
		public bool HasPassword { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public byte[] KeyHash { get; set; }
	}

	[ProtoContract]
	class JoinGameReply
	{
		[ProtoMember(1)]
		public JoinGameReplyType Type { get; set; }

		[ProtoMember(2)]
		public List<PlayerInformation> Team1 { get; set; }

		[ProtoMember(3)]
		public List<PlayerInformation> Team2 { get; set; }
	}

	[ProtoContract]
	class PlayerInformation
	{
		[ProtoMember(1)]
		public int PlayerId { get; set; }

		[ProtoMember(2)]
		public string Name { get; set; }

		[ProtoMember(3, IsRequired = false)]
		public int? FactionId { get; set; }
	}
}
