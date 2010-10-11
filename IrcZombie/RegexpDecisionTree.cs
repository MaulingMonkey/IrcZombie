using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace IrcZombie {
	class RegexDecisionTree<O> : List<RegexDecisionTree<O>.Entry> {
		readonly RegexOptions RegexOptions;

		public RegexDecisionTree( RegexOptions options ) {
			RegexOptions = options;
		}

		public struct Entry {
			public Regex           Regex;
			public Action<O,Match> OnMatch;
		}

		public delegate void ExtAction<A,B,C,D,E>(A a,B b,C c,D d,E e);
		public delegate void ExtAction<A,B,C,D,E,F>(A a,B b,C c,D d,E e,F f);

		public void Add( string regexp, Action<O>                                       action ) {
			var r=new Regex(regexp,RegexOptions);
			Debug.Assert(r.GetGroupNumbers().Length==0);
			Add(new Entry(){Regex=r,OnMatch=(o,m)=>action(o)});
		}
		public void Add( string regexp, Action<O,string>                                action ) {
			var r=new Regex(regexp,RegexOptions);
			Debug.Assert(r.GetGroupNumbers().Length==1);
			Add(new Entry(){Regex=r,OnMatch=(o,m)=>action(o,m.Groups[1].Value)});
		}
		public void Add( string regexp, Action<O,string,string>                         action ) {
			var r=new Regex(regexp,RegexOptions);
			Debug.Assert(r.GetGroupNumbers().Length==2);
			Add(new Entry(){Regex=r,OnMatch=(o,m)=>action(o,m.Groups[1].Value,m.Groups[2].Value)});
		}
		public void Add( string regexp, Action<O,string,string,string>                  action ) {
			var r=new Regex(regexp,RegexOptions);
			Debug.Assert(r.GetGroupNumbers().Length==3);
			Add(new Entry(){Regex=r,OnMatch=(o,m)=>action(o,m.Groups[1].Value,m.Groups[2].Value,m.Groups[3].Value)});
		}
		public void Add( string regexp, ExtAction<O,string,string,string,string>        action ) {
			var r=new Regex(regexp,RegexOptions);
			Debug.Assert(r.GetGroupNumbers().Length==4);
			Add(new Entry(){Regex=r,OnMatch=(o,m)=>action(o,m.Groups[1].Value,m.Groups[2].Value,m.Groups[3].Value,m.Groups[4].Value)});
		}
		public void Add( string regexp, ExtAction<O,string,string,string,string,string> action ) {
			var r=new Regex(regexp,RegexOptions);
			Debug.Assert(r.GetGroupNumbers().Length==5);
			Add(new Entry(){Regex=r,OnMatch=(o,m)=>action(o,m.Groups[1].Value,m.Groups[2].Value,m.Groups[3].Value,m.Groups[4].Value,m.Groups[5].Value)});
		}

		public void Invoke( O state, string text ) {
			foreach ( var entry in this ) {
				var m = entry.Regex.Match(text);
				if ( !m.Success ) continue;

				entry.OnMatch( state, m );
				return;
			}
		}
	}
}
