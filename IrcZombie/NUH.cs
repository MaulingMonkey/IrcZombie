using System;

namespace IrcZombie {
	struct NUH {
		string Nick, User, Host;

		public static implicit operator NUH( string original ) {
			var ex = original.IndexOf('!');
			var at = original.IndexOf('@');
			if ( ex==-1 || at==-1 || ex>at ) throw new ArgumentException( "Expected string in the format of nick!user@host" );

			return new NUH()
				{ Nick = original.Substring(0,ex)
				, User = original.Substring(ex+1,at-ex-1)
				, Host = original.Substring(ex+1)
				};
		}
	}
}
