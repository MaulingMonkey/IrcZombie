using System;

namespace IrcZombie {
	/// <summary>
	/// Historical state
	/// </summary>
	class IrcConnectionHistory : IIrcListener {
		public void Before( CommandEvent e ) {}
		public void After(  CommandEvent e ) {}
		public void On( NickEvent     e ) {}
		public void On( JoinEvent     e ) {}
		public void On( PartEvent     e ) {}
		public void On( QuitEvent     e ) {}
		public void On( KickEvent     e ) {}
		public void On( PrivMsgEvent  e ) {}
		public void On( NoticeEvent   e ) {}
		public void On( ModeEvent     e ) {}
		public void On( TopicEvent    e ) {}
		public void On( ResponseEvent e ) {}
	}
}
