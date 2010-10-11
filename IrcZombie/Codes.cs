namespace IrcZombie {
	static class RPL {
		public const string
			Welcome           = "001", //  "Welcome to the Internet Relay Network <nick>!<user>@<host>"
			YourHost          = "002", //  "Your host is <servername>, running version <ver>"
			Created           = "003", //  "This server was created <date>"
			MyInfo            = "004", //  "<servername> <version> <available user modes> <available channel modes>"  The server sends Replies 001 to 004 to a user upon successful registration.
			//Bounce            = "005", //  "Try server <server name>, port <port number>"                             Sent by the server to a user to suggest an alternative server. This is often used when the connection is refused because the server is already full.
			// Some real 005 responses that are clearly not bounce:
			//:Saffron.US.AfterNET.Org 005 TelnetMonkey NAMESX UHNAMES WHOX WALLCHOPS WALLVOICES USERIP CPRIVMSG CNOTICE MODES=6 MAXNICKLEN=30 TOPICLEN=250 AWAYLEN=250 KICKLEN=250 :are supported by this server
			//:Saffron.US.AfterNET.Org 005 TelnetMonkey MAXCHANNELLEN=200 CASEMAPPING=rfc1459 ELIST=MNUCT MAXCHANNELS=50 NICKLEN=30 CHANNELLEN=200 MAXBANS=45 SILENCE=25 WATCH=128 NETWORK=AfterNET CHANTYPES=# PREFIX=(ohv)@%+ STATUSMSG=@%+ :are supported by this server
			//:Saffron.US.AfterNET.Org 005 TelnetMonkey CHANMODES=be,k,lL,acimnprstzCMNOQSTZ MAXLIST=b:45,e:45 EXCEPTS=e MAXEXCEPTS=45 EXTBANS=~,acjnqrRt :are supported by this server

			None              = "300", //  Dummy reply number. Not used.
			UserHost          = "302", //  ":[<reply>{<space><reply>}]"                Reply format used by USERHOST to list replies to the query list. The reply string is composed as follows: <reply> ::= <nick>['*'] '=' <'+'|'-'><hostname> The '*' indicates whether the client has registered as an Operator. The '-' or '+' characters represent whether the client has set an AWAY message or not respectively.
			IsOn              = "303", //  ":[<nick> {<space><nick>}]"                 Reply format used by ISON to list replies to the query list.
			Away              = "301", //  "<nick> :<away message>"
			UnAway            = "305", //  ":You are no longer marked as being away"
			NoWAway           = "306", //  ":You have been marked as being away"       These replies are used with the AWAY command (if allowed). RPL_AWAY is sent to any client sending a PRIVMSG to a client which is away. RPL_AWAY is only sent by the server to which the client is connected. Replies RPL_UNAWAY and RPL_NOWAWAY are sent when the client removes and sets an AWAY message.
			WhoIsUser         = "311", //  "<nick> <user> <host> * :<real name>"
			WhoIsServer       = "312", //  "<nick> <server> :<server info>"
			WhoIsOperator     = "313", //  "<nick> :is an IRC operator"
			WhoIsIdle         = "317", //  "<nick> <integer> :seconds idle"
			EndOfWhoIs        = "318", //  "<nick> :End of /WHOIS list"
			WhoIsChannels     = "319", //  "<nick> :{[@|+]<channel><space>}"
			// Replies 311 - 313, 317 - 319 are all replies generated in response to a WHOIS message. Given that there are enough parameters present, the answering server must either formulate a reply out of the above numerics (if the query nick is found) or return an
			// error reply. The '*' in RPL_WHOISUSER is there as the literal character and not as a wild card. For each reply set, only RPL_WHOISCHANNELS may appear more than once (for long lists of channel names). The '@' and '+' characters next to the channel
			// name indicate whether a client is a channel operator or has been granted permission to speak on a moderated channel. The RPL_ENDOFWHOIS reply is used to mark the end of processing a WHOIS message.
			WhoWasUser        = "314", //  "<nick> <user> <host> * :<real name>"
			EndOfWhoWas       = "369", //  "<nick> :End of WHOWAS"
			// When replying to a WHOWAS message, a server must use the replies RPL_WHOWASUSER, RPL_WHOISSERVER or ERR_WASNOSUCHNICK for each nickname in the presented list. At the end of all reply batches, there must be
			// RPL_ENDOFWHOWAS (even if there was only one reply and it was an error).



			NoTopic           = "331",
			Topic             = "332",
			Inviting          = "341",
			NamReply          = "353";
	}
	static class ERR {
		public const string // http://www.irchelp.org/irchelp/rfc/chapter6.html#c6_1
			NoSuchNick        = "401", //  "<nickname> :No such nick/channel"                      Used to indicate the nickname parameter supplied to a command is currently unused.
			NoSuchServer      = "402", //  "<server name> :No such server"                         Used to indicate the server name given currently doesn't exist.
			NoSuchChannel     = "403", //  "<channel name> :No such channel"                       Used to indicate the given channel name is invalid.
			CannotSendToChan  = "404", //  "<channel name> :Cannot send to channel"                Sent to a user who is either (a) not on a channel which is mode +n or (b) not a chanop (or mode +v) on a channel which has mode +m set and is trying to send a PRIVMSG message to that channel.
			TooManyChannels   = "405", //  "<channel name> :You have joined too many channels"     Sent to a user when they have joined the maximum number of allowed channels and they try to join another channel.
			WasNoSuchNick     = "406", //  "<nickname> :There was no such nickname"                Returned by WHOWAS to indicate there is no history information for that nickname.
			TooManyTargets    = "407", //  "<target> :Duplicate recipients. No message delivered"  Returned to a client which is attempting to send PRIVMSG/NOTICE using the user@host destination format and for a user@host which has several occurrences.
			NoOrigin          = "409", //  ":No origin specified"                                  PING or PONG message missing the originator parameter which is required since these commands must work without valid prefixes.
			NoRecipient       = "411", //  ":No recipient given (<command>)"
			NoTextToSend      = "412", //  ":No text to send"
			NoTopLevel        = "413", //  "<mask> :No toplevel domain specified"
			WildTopLevel      = "414", //  "<mask> :Wildcard in toplevel domain"                   412 - 414 are returned by PRIVMSG to indicate that the message wasn't delivered for some reason. ERR_NOTOPLEVEL and ERR_WILDTOPLEVEL are errors that are returned when an invalid use of "PRIVMSG $<server>" or "PRIVMSG #<host>" is attempted.
			UnknownCommand    = "421", //  "<command> :Unknown command"                            Returned to a registered client to indicate that the command sent is unknown by the server.
			NoMotd            = "422", //  ":MOTD File is missing"                                 Server's MOTD file could not be opened by the server.
			NoAdminInfo       = "423", //  "<server> :No administrative info available"            Returned by a server in response to an ADMIN message when there is an error in finding the appropriate information.
			FileError         = "424", //  ":File error doing <file op> on <file>"                 Generic error message used to report a failed file operation during the processing of a message.
			NoNicknameGiven   = "431", //  ":No nickname given"                                    Returned when a nickname parameter expected for a command and isn't found.
			ErroNeusNickname  = "432", //  "<nick> :Erroneus nickname"                             Returned after receiving a NICK message which contains characters which do not fall in the defined set. See section x.x.x for details on valid nicknames.
			NicknameInUse     = "433", //  "<nick> :Nickname is already in use"                    Returned when a NICK message is processed that results in an attempt to change to a currently existing nickname.
			NickCollision     = "436", //  "<nick> :Nickname collision KILL"                       Returned by a server to a client when it detects a nickname collision (registered of a NICK that already exists by another server).
			UserNotInChannel  = "441", //  "<nick> <channel> :They aren't on that channel"         Returned by the server to indicate that the target user of the command is not on the given channel.
			NotOnChannel      = "442", //  "<channel> :You're not on that channel"                 Returned by the server whenever a client tries to perform a channel effecting command for which the client isn't a member.
			UserOnChannel     = "443", //  "<user> <channel> :is already on channel"               Returned when a client tries to invite a user to a channel they are already on.
			NoLogin           = "444", //  "<user> :User not logged in"                            Returned by the summon after a SUMMON command for a user was unable to be performed since they were not logged in.
			SummonDisabled    = "445", //  ":SUMMON has been disabled"                             Returned as a response to the SUMMON command. Must be returned by any server which does not implement it.
			UsersDisabled     = "446", //  ":USERS has been disabled"                              Returned as a response to the USERS command. Must be returned by any server which does not implement it.
			NotRegistered     = "451", //  ":You have not registered"                              Returned by the server to indicate that the client must be registered before the server will allow it to be parsed in detail.
			NeedMoreParams    = "461", //  "<command> :Not enough parameters"                      Returned by the server by numerous commands to indicate to the client that it didn't supply enough parameters.
			AlreadyRegistered = "462", //  ":You may not reregister"                               Returned by the server to any link which tries to change part of the registered details (such as password or user details from second USER message).
			NoPermForHost     = "463", //  ":Your host isn't among the privileged"                 Returned to a client which attempts to register with a server which does not been setup to allow connections from the host the attempted connection is tried.
			PasswdMismatch    = "464", //  ":Password incorrect"                                   Returned to indicate a failed attempt at registering a connection for which a password was required and was either not given or incorrect.
			YoureBannedCreep  = "465", //  ":You are banned from this server"                      Returned after an attempt to connect and register yourself with a server which has been setup to explicitly deny connections to you.
			KeySet            = "467", //  "<channel> :Channel key already set"
			ChannelIsFull     = "471", //  "<channel> :Cannot join channel (+l)"
			UnknownMode       = "472", //  "<char> :is unknown mode char to me"
			InviteOnlyChan    = "473", //  "<channel> :Cannot join channel (+i)"
			BannedFromChan    = "474", //  "<channel> :Cannot join channel (+b)"
			BadChannelKey     = "475", //  "<channel> :Cannot join channel (+k)"
			NoPrivileges      = "481", //  ":Permission Denied- You're not an IRC operator"        Any command requiring operator privileges to operate must return this error to indicate the attempt was unsuccessful.
			ChanOPrivsNeeded  = "482", //  "<channel> :You're not channel operator"                Any command requiring 'chanop' privileges (such as MODE messages) must return this error if the client making the attempt is not a chanop on the specified channel.
			CantKillServer    = "483", //  ":You cant kill a server!"                              Any attempts to use the KILL command on a server are to be refused and this error returned directly to the client.
			NoOperHost        = "491", //  ":No O-lines for your host"                             If a client sends an OPER message and the server has not been configured to allow connections from the client's host as an operator, this error must be returned.
			UModeUnknownFlag  = "501", //  ":Unknown MODE flag"                                    Returned by the server to indicate that a MODE message was sent with a nickname parameter and that the a mode flag sent was not recognized.
			UsersDontMatch    = "502"; //  ":Cant change mode for other users"                     Error sent to any user trying to view or change the user mode for a user other than themselves.
	}
}
