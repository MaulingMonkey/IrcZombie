using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IrcZombie {
	static partial class ExtensionMethods {
		public static string TrimStart1( this string original, string prefix ) { return original.StartsWith(prefix) ? original.Substring(prefix.Length) : original; }
		public static string TrimStart1( this string original, char   prefix ) { return original.TrimStart1(prefix.ToString()); }
	}
}
