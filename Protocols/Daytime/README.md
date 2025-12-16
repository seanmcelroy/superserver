# Daytime

This character protocol server implementation listens to tcp/2013 instead of tcp/13 by default, so as not require
elevated privileges to bind to a port number below 1024.

It conforms to IETF RFC 863 "Daytime Protocol", which is provided for reference in the file `rfc867.txt`.