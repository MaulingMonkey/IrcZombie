using System.Collections.Generic;

namespace IrcZombie {
	class IrcChannel {
		public string Topic;
		public bool   IsJoined;
		public readonly HashSet<string> NicksInChannel = new HashSet<string>();
	}
}
