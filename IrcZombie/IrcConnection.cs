using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace IrcZombie {
	class IrcConnection {
		static readonly Random RNG = new Random();

		public Socket Socket { get; private set; }

		public readonly IrcConnectionState   State = new IrcConnectionState();
		public readonly IrcConnectionHistory History = new IrcConnectionHistory();
		public readonly List<IIrcListener> Listeners;

		public static IrcConnection RecoverConnectionFrom( Socket socket ) {
			return new IrcConnection(socket);
		}

		private IrcConnection() {
			Listeners = new List<IIrcListener>() { State, History };
		}

		private IrcConnection( Socket recoverfrom ):this() {
			Socket = recoverfrom;
			Send("VERSION");
		}

		public IrcConnection( string hostname, int port ):this() {
			Socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			Socket.Connect( hostname, port );
			Send("USER irczombie * * *");
			Send("NICK IrcZombie");
		}

		public void Send( string message ) {
			Socket.Send( Encoding.UTF8.GetBytes(message+"\r\n") );
			Thread.Sleep(1000);
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
					connection.Send("JOIN #sparta,#sparta2");
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

				Func<int,string>     skip = off => parameters.Substring(off).TrimStart1(' ').TrimStart1(':');
				Action<CommandEvent> inject = ce => { ce.Connection = connection; ce.Who = nuh; };
				var listeners = connection.Listeners;

				switch (action) {
				case "NICK":    { var e=new NickEvent()    { NewNickname=parameters.TrimStart1(':')                                                              }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "JOIN":    { var e=new JoinEvent()    { AffectedChannels=new HashSet<string>(parameters.TrimStart1(":").Split(','))                         }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "PART":    { var e=new PartEvent()    { AffectedChannels=new HashSet<string>(parameters.TrimStart1(":").Split(','))                         }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "QUIT":    { var e=new QuitEvent()    { Message=parameters.TrimStart1(':')                                                                  }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "KICK":    { var e=new KickEvent()    { AffectedChannels=new HashSet<string>(){p[0]}, Kicked=p[1], Message=skip(p[0].Length+p[1].Length+2)  }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "PRIVMSG": { var e=new PrivMsgEvent() { AffectedChannels=new HashSet<string>(){p[0]}, Message =skip(p[0].Length)                            }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "NOTICE":  { var e=new NoticeEvent()  { AffectedChannels=new HashSet<string>(){p[0]}, Message =skip(p[0].Length)                            }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "MODE":    { var e=new ModeEvent()    {                                                                                                     }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "TOPIC":   { var e=new TopicEvent()   { AffectedChannels=new HashSet<string>(){p[0]}, NewTopic=skip(p[0].Length)                            }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				case "INVITE":  { var e=new InviteEvent()  { AffectedChannels=new HashSet<string>(){p[1].TrimStart1(":")}, Invited=p[0]                          }; inject(e); foreach ( var l in listeners ) { l.Before(e); l.On(e); l.After(e); } } break;
				default:        break;
				}
			}}};

		void OnRecv( string line ) {
			RCT.Invoke( this, line );
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
