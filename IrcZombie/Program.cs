using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Collections.Generic;

namespace IrcZombie {
	class CommandResponder : IIrcListener {
		public IrcConnectionState State;

		public void Before( CommandEvent e ) {}
		public void After(  CommandEvent e ) {}
		public void On( NickEvent     e ) {}
		public void On( JoinEvent     e ) {}
		public void On( PartEvent     e ) {}
		public void On( QuitEvent     e ) {}
		public void On( KickEvent     e ) {}
		public void On( PrivMsgEvent  e ) {
			var a = e.Message.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);

			foreach ( var chan in e.AffectedChannels ) {
				switch (a[0]) {
				case "!restart":
					Program.RequestRelaunch();
					e.Connection.StopPumping();
					break;
				case "!quit":
					e.Connection.Send("QUIT");
					e.Connection.StopPumping();
					break;
				case "!info":
					if (State==null) {
						e.Connection.Send("NOTICE "+chan+" :No state, I'm senile!");
					} else {
						var info = State.GetChannelInfo(chan);
						if (!info.Joined) e.Connection.Send("NOTICE "+chan+" :What?  I'm in this channel?!?");
						else              e.Connection.Send("NOTICE "+chan+" :Topic: "+(info.Topic??"N/A")+"   People: "+string.Join(", ",info.People.Select(p=>p.NUH.Nick)));
					}
					break;
				case "!join":
					e.Connection.Send("JOIN "+a[1]);
					break;
				}
			}
		}
		public void On( NoticeEvent   e ) {}
		public void On( ModeEvent     e ) {}
		public void On( TopicEvent    e ) {}
		public void On( InviteEvent   e ) {
			foreach ( var chan in e.AffectedChannels ) {
				e.Connection.Send("JOIN "+chan);
				e.Connection.Send("NOTICE "+chan+" :Invited by "+e.Who.ToString());
			}
		}
		public void On( ResponseEvent e ) {}
	}

	class Program {
		static        string OriginalPath;
		static IrcConnection Connection;

		static bool RelaunchFlag = false;
		public static void RequestRelaunch() {
			RelaunchFlag = true;
		}

		static void Relaunch() {
			var tempdir = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() );
			Directory.CreateDirectory( tempdir );
			var xcopy_args = String.Format( "/E \"{0}\" \"{1}\\\"", Path.GetDirectoryName(OriginalPath).Replace("file:\\",""), tempdir );
			var pcopy = Process.Start( "xcopy", xcopy_args );
			if (!pcopy.WaitForExit(10000)) throw new InvalidOperationException("Relaunch copy hanged!");
			Win32.MoveFileEx( tempdir, null, Win32.MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT) ;
			var newexe = Path.Combine( tempdir, Path.GetFileName(OriginalPath) );

			if ( Connection != null && Connection.Socket.Connected ) {
				var pipe = new AnonymousPipeServerStream( PipeDirection.Out, HandleInheritability.Inheritable );
				var pipename = pipe.GetClientHandleAsString();
				var pcloneinfo = new ProcessStartInfo( newexe, string.Format( "--original={0} --pipe={1}", OriginalPath, pipename ) );
				pcloneinfo.UseShellExecute = false;
				var pclone = Process.Start(pcloneinfo);
				var sockinfo = Connection.Socket.DuplicateAndClose(pclone.Id).ProtocolInformation;
				pipe.Write( sockinfo, 0, sockinfo.Length );
				pipe.WaitForPipeDrain();
				pipe.Close();
			} else {
				var pclone = Process.Start( newexe, string.Format( "--original={0}", OriginalPath ) );
			}
		}

		static void Main( string[] args ) {
#if DEBUG
			Console.WriteLine("PID {0} Debug", Process.GetCurrentProcess().Id );
#else
			Console.WriteLine("PID {0} Release", Process.GetCurrentProcess().Id );
#endif
			if (!args.Any(arg=>arg.StartsWith("--original="))) { // Assume we're the original process
				OriginalPath = Assembly.GetExecutingAssembly().GetName().CodeBase;
				// Relaunch and quit so that we don't lock the original executable:
				Relaunch();
				return;
			}

			OriginalPath = args.First(arg=>arg.StartsWith("--original=")).Remove(0,"--original=".Length);
			if (args.Any(arg=>arg.StartsWith("--pipe="))) {
				var pipename = args.First(arg=>arg.StartsWith("--pipe=")).Remove(0,"--pipe=".Length);
				var pipe   = new AnonymousPipeClientStream(pipename);
				var buffer = new byte[9001];

				int read=0, length=0;
				do {
					read = pipe.Read(buffer,length,buffer.Length-length);
					length += read;
				} while ( read!=0 );

				var si = new SocketInformation()
					{ Options = SocketInformationOptions.Connected | SocketInformationOptions.UseOnlyOverlappedIO
					, ProtocolInformation = buffer.Take(length).ToArray()
					};
				Connection = IrcConnection.RecoverConnectionFrom( new Socket(si) );
			} else {
				Connection = new IrcConnection( "irc.afternet.org", 6667 );
			}

			Connection.Listeners.Add(new CommandResponder(){State=Connection.Listeners[0] as IrcConnectionState});
			Connection.BeginPumping();
			Connection.WaitPumping();
			if ( RelaunchFlag ) Relaunch();
		}
	}
}
