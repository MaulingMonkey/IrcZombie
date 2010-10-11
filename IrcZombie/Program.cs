using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;

namespace IrcZombie {
	class Program {
		static          string OriginalPath;

		static IrcConnection Connection;

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
				//Console.WriteLine( "Pipe Name: {0}", pipename );
				//Console.ReadKey();
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
			Console.WriteLine("PID {0}", Process.GetCurrentProcess().Id );
#if !DEBUG
			if (!args.Any(arg=>arg.StartsWith("--original="))) { // Assume we're the original process
				OriginalPath = Assembly.GetExecutingAssembly().GetName().CodeBase;
				// Relaunch and quit so that we don't lock the original executable:
				Relaunch();
				return;
			}

			OriginalPath = args.First(arg=>arg.StartsWith("--original=")).Remove(0,"--original=".Length);
			if (args.Any(arg=>arg.StartsWith("--pipe="))) {
				var pipename = args.First(arg=>arg.StartsWith("--pipe=")).Remove(0,"--pipe=".Length);
				//Console.WriteLine( "Pipe Name: {0}", pipename );
				//Console.ReadKey();
				var pipe   = new AnonymousPipeClientStream(pipename);
				var buffer = new byte[9001];
				var length = pipe.Read(buffer,0,buffer.Length);
				Debug.Assert(pipe.IsMessageComplete);
				var si = new SocketInformation()
					{ Options = SocketInformationOptions.Connected | SocketInformationOptions.UseOnlyOverlappedIO
					, ProtocolInformation = buffer.Take(length).ToArray()
					};
				Connection = IrcConnection.RecoverConnectionFrom( new Socket(si) );
			} else {
				Connection = new IrcConnection( "irc.afternet.org", 6667 );
			}
#else
			Connection = new IrcConnection( "irc.afternet.org", 6667 );
#endif

			Connection.BeginPumping();
			Connection.WaitPumping();
			if ( Connection.RequestRestart ) Relaunch();
		}
	}
}
