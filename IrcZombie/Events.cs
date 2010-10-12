using System.Collections.Generic;

namespace IrcZombie {
	class Event {
		public IrcConnection Connection;
	}
	class CommandEvent : Event {
		public NUH Who;
		public HashSet<string> AffectedChannels = null;
	}
	class NickEvent    : CommandEvent { public string NewNickname; }
	class JoinEvent    : CommandEvent {}
	class PartEvent    : CommandEvent {}
	class QuitEvent    : CommandEvent { public string Message; }
	class KickEvent    : CommandEvent { public string Kicked, Message; }
	class PrivMsgEvent : CommandEvent { public string Message; }
	class NoticeEvent  : CommandEvent { public string Message; }
	class ModeEvent    : CommandEvent {}
	class TopicEvent   : CommandEvent { public string NewTopic; }
	class ResponseEvent : Event {
		public string Server;
		public string Code;
		public string YourNick;
		public string ArgumentData;
	}
}
