# Character Generator

This character protocol server implementation listens to tcp/2019 instead of tcp/19 by default, so as not require
elevated privileges to bind to a port number below 1024.

It conforms to IETF RFC 863 "Character Generator Protocol", which is provided for reference in the file `rfc864.txt`.