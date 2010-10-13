using System;

namespace IrcZombie {
	struct NUH {
		public string Nick, User, Host;

		public static explicit operator NUH( string original ) {
			var ex = original.IndexOf('!');
			var at = original.IndexOf('@');
			if ( ex==-1 || at==-1 || ex>at ) throw new ArgumentException( "Expected string in the format of nick!user@host" );

			return new NUH()
				{ Nick = original.Substring(0,ex)
				, User = original.Substring(ex+1,at-ex-1)
				, Host = original.Substring(ex+1)
				};
		}

		public override string ToString() { return String.Format("{0}!{1}@{2}",Nick,User,Host); }
	}
}
