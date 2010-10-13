using System;
using System.Collections.Generic;
using System.Linq;

namespace IrcZombie {
	/// <summary>
	/// Immediate state data (what we know NOW)
	/// </summary>
	class IrcConnectionState : IIrcListener {
		public string CurrentNickname { get; private set; }
		public readonly Dictionary<string,ChannelInfo> Channels = new Dictionary<string,ChannelInfo>();
		public ChannelInfo GetChannelInfo( string channel ) {
			if (!Channels.ContainsKey(channel)) Channels.Add( channel, new ChannelInfo() );
			return Channels[channel];
		}

		public class ChannelInfo {
			public bool   Joined;
			public string Topic;
			public readonly List<Person> People = new List<Person>();

			public class Person { public string Sigil; public NUH NUH; }
		}

		public void Before( CommandEvent e ) {
			foreach ( var channel in Channels.Values ) foreach ( var person in channel.People ) if ( person.NUH.Nick == e.Who.Nick ) person.NUH = e.Who;
		}

		public void After( CommandEvent e ) {
		}

		public void On( NickEvent     e ) {
			if ( e.Who.Nick == CurrentNickname ) {
				CurrentNickname = e.NewNickname; // We changed our name (successfully)
			} else foreach ( var channel in Channels ) foreach ( var person in channel.Value.People ) if ( person.NUH.Nick==e.Who.Nick ) {
				person.NUH = e.Who;
				person.NUH.Nick = e.NewNickname;
			}
		}
		public void On( JoinEvent     e ) {
			if ( e.Who.Nick == CurrentNickname ) foreach ( var channel in e.AffectedChannels ) {
				GetChannelInfo(channel).Joined = true;
			} else foreach ( var channel in e.AffectedChannels ) {
				GetChannelInfo(channel).People.Add( new ChannelInfo.Person() { Sigil="", NUH=e.Who } );
			}
		}
		public void On( PartEvent     e ) {
			if ( e.Who.Nick == CurrentNickname ) foreach ( var channel in e.AffectedChannels ) {
				var info = GetChannelInfo(channel);
				info.Joined = false;
				info.People.Clear();
			} else foreach ( var channel in e.AffectedChannels ) {
				GetChannelInfo(channel).People.RemoveAll(p=>p.NUH.Nick == e.Who.Nick);
			}
		}
		public void On( QuitEvent     e ) {
			if ( e.Who.Nick == CurrentNickname ) {
				CurrentNickname = null;
				Channels.Clear();
			} else foreach ( var channel in Channels ) {
				channel.Value.People.RemoveAll(p=>p.NUH.Nick == e.Who.Nick);
			}
		}
		public void On( KickEvent     e ) {
			if ( e.Kicked == CurrentNickname ) foreach ( var channel in e.AffectedChannels ) {
				GetChannelInfo(channel).Joined = false;
			} else foreach ( var channel in e.AffectedChannels ) {
				GetChannelInfo(channel).People.RemoveAll(p=>p.NUH.Nick == e.Kicked);
			}
		}
		public void On( PrivMsgEvent  e ) {}
		public void On( NoticeEvent   e ) {}
		public void On( ModeEvent     e ) {}
		public void On( TopicEvent    e ) { foreach ( var channel in e.AffectedChannels ) GetChannelInfo(channel).Topic = e.NewTopic; }
		public void On( InviteEvent   e ) {}

		public void On( ResponseEvent e ) {
			if ( e.Code != ERR.NicknameInUse && e.YourNick != "*" ) { // The check against "*" is just paranoia -- it's what AfterNET uses on 433/NicknameInUse replies if our first NICK is in use, since we don't have a nick at that point, herp derp.
				if ( e.Code != "001" && CurrentNickname == null ) e.Connection.Send("WHOIS "+e.YourNick); // So that's who we are!  Bloody amnesia.  Find out more about ourselves.
				CurrentNickname = e.YourNick;
			}

			var arguments = e.ArgumentData;

			switch ( e.Code ) {
			case RPL.WhoIsChannels:
				// WhoIsChannels     = "319", //  "<nick> :{[@|+]<channel><space>}"
				var space1 = arguments.IndexOf(' ');
				var nick = arguments.Substring(0,space1);
				var colon = arguments.IndexOf(':');
				var channels = arguments.Remove(0,colon==-1?(arguments.IndexOf(' ')+1):(colon+1)).Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries).Select(ch=>ch.TrimStart(':','@','+'));

				if ( nick == CurrentNickname ) {
					foreach ( var channel in channels ) GetChannelInfo(channel).Joined = true;
					e.Connection.Send("NAMES "+String.Join(",",channels));
				}
				break;
			case RPL.NamReply:
				// "<channel> :[[@|+]<nick> [[@|+]<nick> [...]]]"
				// "("="/"*"/"@") <channel> :["@"/"+"] <nick> *(" "["@"/"+"]<nick>)"

				var a = arguments.Split(' ');
				a = a.SkipWhile(b=>!b.StartsWith("#")).ToArray(); // skip over random * and =s AfterNET likes to throw into the argument list before channel name
				var c1 = GetChannelInfo(a[0]);
				if (a[1].StartsWith(":")) a[1] = a[1].Remove(0,1);

				foreach ( var name in a.Skip(1) ) {
					if ( name.Length>0 ) switch ( name[0] ) {
					case '@': c1.People.Add( new ChannelInfo.Person() { Sigil="@", NUH=new NUH(){Nick=name.Substring(1)} } ); break;
					case '+': c1.People.Add( new ChannelInfo.Person() { Sigil="+", NUH=new NUH(){Nick=name.Substring(1)} } ); break;
					default:  c1.People.Add( new ChannelInfo.Person() { Sigil="" , NUH=new NUH(){Nick=name.Substring(0)} } ); break;
					}
				}
				break;
			}
		}
	}
}
