# MinerProxy
Multithreaded Ethereum Stratum Proxy - Replace all Wallets with your own.
![Screenshot](Screens/Screen.PNG)

Original proxy from https://github.com/RajanGrewal -- I've simply modified it to suit my needs.

I wanted a proxy that I could direct my miners to that would allow me to see their status easily, including submitted/accepted shares and reported hashrates.
I also wanted it to replace the wallet that is sent to the pool with my own, while maintaining the miner's original name.

Confirmed working on Alpereum and Ethermine.

    Setting \<Identify DevFee\> will modify the DevFee's rigName to 'DevFee' when reported to pool. Even if you don't set it to 'True', the DevFee wallet will be replaced.
    Usage : MinerProxy.exe <local port> <remote host> <remote port> <Allowed IP> <Your Wallet Address> <Identify DevFee> <Log to file> <debug>
    MinerProxy.exe 9000 us1.ethermine.org 4444 127.0.0.1 0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332 True False False

Most debug output is disabled currently, even when enabled.


# Todo
- [x] Log traffic to file
- [x] Deserialize/Serialize JSON objects
- [x] Replace all wallets with your own
- [x] Only accept from specific IP (or all)
- [x] Calculate and display hashrate (debug must be on)
- [ ] Verify support for other pools (nanopool, mininghub, etc)
- [ ] Verify support for other Ethash coins (Expanse, Ubiq, etc)
- [ ] Support other non-Ethash coins (ZEC, CryptoNote, etc)
- [ ] Stats via built in server/Possibly REST API
- [ ] Auto failover if server stops accepting shares/no getWork replies
- [ ] Save/load settings from JSON file
- [ ] Verbose mode/Multiple debug levels
- [ ] Understand full Stratum protocol
