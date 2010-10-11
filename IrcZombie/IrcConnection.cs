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
		string CurrentNickname; // If not null, or 'recovering', no automatic response to 'nick already in use'.  Otherwise, we haven't been welcomed yet -- try a new nick on 'nick already in use'.
		string DesiredNickname;

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

		void Send( string message ) {
			Socket.Send( Encoding.UTF8.GetBytes(message+"\r\n") );
		}

		readonly Dictionary<string,IrcChannel> _channels = new Dictionary<string,IrcChannel>();
		IrcChannel Channel( string channel ) {
			if (!_channels.ContainsKey(channel)) _channels[channel] = new IrcChannel();
			return _channels[channel];
		}
		readonly Queue<Action> Todo = new Queue<Action>();

		public void Begin( Action action ) { Todo.Enqueue(action); }

		Thread Pump;
		volatile bool StopPumping;
		public bool RequestRestart { get; private set; }
		public void BeginPumping() {
			Pump = new Thread(DoPump);
			Pump.Start();
		}
		public void EndPumping() {
			StopPumping = true;
			Pump.Join();
		}
		public void WaitPumping() {
			Pump.Join();
		}


		//NAMES #sparta
		//:Amsterdam.NL.AfterNET.Org 353 TelnetMonkey * #sparta :TelnetMonkey @MaulingMonkey @X3
		//:Amsterdam.NL.AfterNET.Org 366 TelnetMonkey #sparta :End of /NAMES list.

		static readonly RegexDecisionTree<IrcConnection> RCT = new RegexDecisionTree<IrcConnection>(RegexOptions.Compiled)
			{	{ @"PING (\:?.+)"                          , (connection,code) => connection.Send("PONG "+code) }
			,	{ @"^\:?([^ ]+) (\d\d\d) ([^: ]+) \:?(.*)$", (connection,server,code,target,arguments) => {
				if ( connection.CurrentNickname==null && code!="433" && target!="*") {
					connection.CurrentNickname = target;
					connection.Send("WHOIS "+target);
				}

				switch (code) {
				case RPL.Welcome:
					connection.Send("JOIN #sparta");
					connection.Send("JOIN #sparta2");
					break;
				case RPL.WhoIsChannels:
					// WhoIsChannels     = "319", //  "<nick> :{[@|+]<channel><space>}"
					var space1 = arguments.IndexOf(' ');
					var nick = arguments.Substring(0,space1);
					var colon = arguments.IndexOf(':');
					var channels = arguments.Remove(0,colon==-1?(arguments.IndexOf(' ')+1):(colon+1)).Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries).Select(ch=>ch.TrimStart(':','@','+'));

					if ( nick == connection.CurrentNickname ) {
						foreach ( var channel in channels ) {
							var c = connection.Channel(channel);
							c.IsJoined = true;
						}
						connection.Send("NAMES "+String.Join(",",channels));
					}
					break;
				case RPL.NamReply:
					// "<channel> :[[@|+]<nick> [[@|+]<nick> [...]]]"

					var a = arguments.Split(' ');
					a = a.SkipWhile(b=>!b.StartsWith("#")).ToArray(); // skip over random * and =s AfterNET likes to throw into the argument list before channel name
					var c1 = connection.Channel(a[0]);
					if (a[1].StartsWith(":")) a[1] = a[1].Remove(0,1);

					Console.WriteLine("RPL_NAMREPLY:  Channel: {0}  Nicks: [{1}]", a[0], String.Join(",",a.Skip(1)) );
					foreach ( var name in a.Skip(1) ) c1.NicksInChannel.Add(name.TrimStart('@','+'));
					break;
				case ERR.NicknameInUse:
					connection.Send("NICK IrcZombie"+RNG.Next(0,9999).ToString().PadLeft(4,'0'));
					break;
				default: break;
				}
			}},	{ @"^\:?([^ !]+)!([^ @]+)@([^ ]+) ([^ ]+)(?: (.+))?$", (connection,nick,user,host,action,parameters) => {
				var p = parameters.Split(' ');

				IrcChannel channel;

				switch (action) {
				case "NICK":    break;
				case "JOIN":    break;
				case "PART":    break;
				case "QUIT":    break;
				case "KICK":    break;
				case "PRIVMSG":
					switch (p[1].TrimStart(':')) {
					case "!restart":
						connection.StopPumping = connection.RequestRestart = true;
						break;
					case "!listwho":
						channel = connection.Channel(p[0]);
						if (!channel.IsJoined) {
							connection.Send("NOTICE "+p[0]+" :I didn't even realize this was a channel I was in!");
						} else {
							connection.Send("NOTICE "+p[0]+" :I see: "+string.Join(", ",channel.NicksInChannel));
						}
						break;
					case "!topic":
						channel = connection.Channel(p[0]);
						if (!channel.IsJoined) {
							connection.Send("NOTICE " + p[0] + " :I didn't even realize this was a channel I was in!");
						} else {
							connection.Send("NOTICE " + p[0] + " :I think the topic is:"+channel.Topic);
						}
						break;
					case "!join":
						connection.Send("JOIN " + p[2]);
						break;
					case "!quit":
						connection.Send("QUIT");
						connection.StopPumping = true;
						break;
					}
					break;
				case "NOTICE":  break;
				case "MODE":    break;
				case "TOPIC":   break;
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

			while (!StopPumping || long_buffer.Count>0 ) {
				if (StopPumping) imm_buffer = new byte[1]; // read only a byte at a time until the last CRLF such that socket passoff allows shenannigans.

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
