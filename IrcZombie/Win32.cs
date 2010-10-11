using System.Runtime.InteropServices;

namespace IrcZombie {
	static class Win32 {
		internal enum MoveFileFlags {
			MOVEFILE_REPLACE_EXISTING = 1,
			MOVEFILE_COPY_ALLOWED = 2,
			MOVEFILE_DELAY_UNTIL_REBOOT = 4,
			MOVEFILE_WRITE_THROUGH = 8
		}
		[DllImport("kernel32.dll",EntryPoint="MoveFileEx")]
		internal static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, MoveFileFlags dwFlags);
	}
}
