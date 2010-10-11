using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace IrcZombie {
	class IrcConnection {
		public Socket Socket { get; private set; }
		string CurrentNickname; // If not null, or 'recovering', no automatic response to 'nick already in use'.  Otherwise, we haven't been welcomed yet -- try a new nick on 'nick already in use'.
		string DesiredNickname;

		public static IrcConnection RecoverConnectionFrom( Socket socket ) {
			return new IrcConnection(socket);
		}

		private IrcConnection( Socket recoverfrom ) {
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

		readonly Dictionary<string,IrcChannel> Channels = new Dictionary<string,IrcChannel>();
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

		static readonly RegexDecisionTree<IrcConnection> RCT = new RegexDecisionTree<IrcConnection>(RegexOptions.Compiled)
			{	{ @"PING (\:?.+)"                          , (connection,code) => connection.Send("PONG "+code) }
			,	{ @"^\:?([^ ]+) (\d\d\d) ([^: ]+) \:?(.*)$", (connection,server,code,target,arguments) => {
				if ( connection.CurrentNickname==null && code!="433" && target!="*") {
					connection.CurrentNickname = target;
					connection.Send("WHOIS "+target);
				}

				switch (code) {
				case RPL.Welcome: connection.Send("JOIN #sparta"); break;
				default: break;
				}
			}},	{ @"^\:?([^ !]+)!([^ @]+)@([^ ]+) ([^ ]+)(?: (.+))?$", (connection,nick,user,host,action,parameters) => {
				var p = parameters.Split(' ');

				switch (action) {
				case "NICK":    break;
				case "JOIN":    break;
				case "PART":    break;
				case "QUIT":    break;
				case "KICK":    break;
				case "PRIVMSG":
					if (p[1]=="!restart") connection.StopPumping = connection.RequestRestart = true;
					if (p[1]=="!listwho") {
						//connection.Send("PRIVMSG "+p[0]+" 
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
					bool cr = long_buffer[newline-1] == (byte)'\r';
					var line = Encoding.UTF8.GetString( long_buffer.Take(newline-(cr?1:0)).ToArray() );
					long_buffer.RemoveRange( 0, newline );
					OnRecv(line);
				}
			}
		}
	}
}
