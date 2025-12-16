# Echo

This echo protocol server implementation listens to tcp/2007 instead of tcp/7 by default, so as not require
elevated privileges to bind to a port number below 1024.

It conforms to IETF RFC 863 "Echo Protocol", which is provided for reference in the file `rfc862.txt`.