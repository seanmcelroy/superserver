# Discard

This discard protocol server implementation listens to tcp/2009 instead of tcp/9 by default, so as not require
elevated privileges to bind to a port number below 1024.

It conforms to IETF RFC 863 "Discard Protocol", which is provided for reference in the file `rfc863.txt`.