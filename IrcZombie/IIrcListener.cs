
namespace IrcZombie {
	interface IIrcListener {
		void Before( CommandEvent e );
		void After(  CommandEvent e );
		void On( NickEvent     e );
		void On( JoinEvent     e );
		void On( PartEvent     e );
		void On( QuitEvent     e );
		void On( KickEvent     e );
		void On( PrivMsgEvent  e );
		void On( NoticeEvent   e );
		void On( ModeEvent     e );
		void On( TopicEvent    e );
		void On( ResponseEvent e );
	}
}
