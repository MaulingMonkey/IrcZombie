So what exactly does this prototype do?

1)  It copies the entire application to a temporary directory so you can update the executable in-place.
2)  If you tell it to "!restart", it will finish up the line it's currently reading (if any), and pass off the Socket to a new process refreshed (and copied again) from the original directory -- all without needing to reconnect!
3)  It will recover state information via IRC commands (It's nick, the channels it's in, who's in those channels and so forth)

Notes:

Currently, it knows more when it 'recovers' than when it connects normally.  Lol.