# superserver

Because sometimes terrible ideas deserve to be reimplemented.

This is a simple inetd-style project that implements several RFCs from the early Internet, namely:

* RFC 862 - Echo Protocol                - tcp/2007 (instead of tcp/7) and udp/2007 (instead of udp/7)
* RFC 863 - Discard Protocol             - tcp/2009 (instead of tcp/9) and udp/2009 (instead of udp/9)
* RFC 864 - Character Generator Protocol - tcp/2019 (instead of tcp/19) and udp/2019 (instead of udp/19)
* RFC 867 - Daytime Protocol             - tcp/2013 (instead of tcp/13) and udp/2013 (instead of udp/13)

## Security notice

Many of these early protocols have little to no utility on the modern Internet.  Worse, some can reveal
information about a host's state which can be security sensitive, or they can allow for amplification attacks
when packets are spoofed.  This server should be considered a hobbyist/experimental project and not deployed,
especially at scale.