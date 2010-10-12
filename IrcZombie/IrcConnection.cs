using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace IrcZombie {
	interface IIrcConnectionListener {
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

	/// <summary>
	/// Immediate state data (what we know NOW)
	/// </summary>
	class IrcConnectionState : IIrcConnectionListener {
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

				Console.WriteLine("RPL_NAMREPLY:  Channel: {0}  Nicks: [{1}]", a[0], String.Join(",",a.Skip(1)) );
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

	/// <summary>
	/// More state data
	/// </summary>
	class IrcConnectionCacheState {
	}

	/// <summary>
	/// Historical state
	/// </summary>
	class IrcConnectionHistory {
	}

	class IrcConnection {
		static readonly Random RNG = new Random();

		public Socket Socket { get; private set; }

		public readonly List<IIrcConnectionListener> Listeners = new List<IIrcConnectionListener>()
			{ new IrcConnectionState()
			};

		public static IrcConnection RecoverConnectionFrom( Socket socket ) {
			return new IrcConnection(socket);
		}

		private IrcConnection( Socket recoverfrom ) {
			Socket = recoverfrom;
			Send("VERSION");
		}

		public IrcConnection( string hostname, int port ) {
			Socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			Socket.Connect( hostname, port );
			Send("USER irczombie * * *");
			Send("NICK IrcZombie");
		}

		public void Send( string message ) {
			Socket.Send( Encoding.UTF8.GetBytes(message+"\r\n") );
		}

		Thread Pump;
		volatile bool StopPumpingFlag;
		public void BeginPumping() {
			Pump = new Thread(DoPump);
			Pump.Start();
		}
		public void StopPumping() {
			StopPumpingFlag = true;
		}
		public void WaitPumping() {
			Pump.Join();
		}

		static readonly RegexDecisionTree<IrcConnection> RCT = new RegexDecisionTree<IrcConnection>(RegexOptions.Compiled)
			{	{ @"PING (\:?.+)"                          , (connection,code) => connection.Send("PONG "+code) }
			,	{ @"^\:?([^ ]+) (\d\d\d) ([^: ]+) \:?(.*)$", (connection,server,code,target,arguments) => {
				switch (code) {
				case RPL.Welcome:
					connection.Send("JOIN #sparta");
					connection.Send("JOIN #sparta2");
					break;
				case ERR.NicknameInUse:
					connection.Send("NICK IrcZombie"+RNG.Next(0,9999).ToString().PadLeft(4,'0'));
					break;
				default: break;
				}

				{
					var e = new ResponseEvent() { Connection=connection, Server=server, Code=code, YourNick=target, ArgumentData=arguments.TrimStart1(':').TrimEnd('\r','\n') };
					foreach ( var l in connection.Listeners ) l.On(e);
				}
			}},	{ @"^\:?([^ !]+![^ @]+@[^ ]+) ([^ ]+)(?: (.+))?$", (connection,nuh_,action,parameters) => {
				var nuh = (NUH)nuh_;
				var p = parameters.Split(' ');

				Func<int,string> skip = off => parameters.Substring(off).TrimStart1(' ').TrimStart1(':');

				switch (action) {
				case "NICK":    { var e=new NickEvent()    { Connection=connection, Who=nuh, NewNickname=parameters.TrimStart1(':')                                                }; foreach ( var l in connection.Listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "JOIN":    { var e=new JoinEvent()    { Connection=connection, Who=nuh, AffectedChannels=new HashSet<string>(parameters.TrimStart1(":").Split(','))           }; foreach ( var l in connection.Listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "PART":    { var e=new PartEvent()    { Connection=connection, Who=nuh, AffectedChannels=new HashSet<string>(parameters.TrimStart1(":").Split(','))           }; foreach ( var l in connection.Listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "QUIT":    { var e=new QuitEvent()    { Connection=connection, Who=nuh, Message=parameters.TrimStart1(':')                                                    }; foreach ( var l in connection.Listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "KICK":    { var e=new KickEvent()    { Connection=connection, Who=nuh, AffectedChannels=new HashSet<string>(){p[0]}, Kicked=p[1], Message=skip(p[0].Length)  }; foreach ( var l in connection.Listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "PRIVMSG": { var e=new PrivMsgEvent() { Connection=connection, Who=nuh, AffectedChannels=new HashSet<string>(){p[0]}, Message =skip(p[0].Length)              }; foreach ( var l in connection.Listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "NOTICE":  { var e=new NoticeEvent()  { Connection=connection, Who=nuh, AffectedChannels=new HashSet<string>(){p[0]}, Message =skip(p[0].Length)              }; foreach ( var l in connection.Listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "MODE":    { var e=new ModeEvent()    { Connection=connection, Who=nuh                                                                                        }; foreach ( var l in connection.Listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "TOPIC":   { var e=new TopicEvent()   { Connection=connection, Who=nuh, AffectedChannels=new HashSet<string>(){p[0]}, NewTopic=skip(p[0].Length)              }; foreach ( var l in connection.Listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				default:        break;
				}
			}}};

		void OnRecv( string line ) {
			RCT.Invoke( this, line );
		}

		void OnRplWelcome() {
		}

		void DoPump() {
			var long_buffer = new List<byte>() { Capacity = 256 };
			var imm_buffer = new byte[256];

			while (!StopPumpingFlag || long_buffer.Count>0 ) {
				if (StopPumpingFlag) imm_buffer = new byte[1]; // read only a byte at a time until the last CRLF such that socket passoff allows shenannigans.

				var read = Socket.Receive( imm_buffer );
				long_buffer.AddRange( imm_buffer.Take(read) );
				if (!imm_buffer.Contains((byte)'\n')) continue;

				for ( int newline ; -1 != (newline=long_buffer.IndexOf((byte)'\n')) ; ) {
					var line = Encoding.UTF8.GetString( long_buffer.Take(newline).ToArray() ).TrimEnd('\r','\n');
					long_buffer.RemoveRange( 0, newline+1 );
					OnRecv(line);
				}
			}
		}
	}
}
