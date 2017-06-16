# MinerProxy

![Screenshot](Screens/Screen.PNG)

Original proxy from https://github.com/RajanGrewal -- I've simply modified it to suit my needs.

I wanted a proxy that I could direct my miners to that would allow me to see their status easily, including submitted/accepted shares and reported hashrates.
I also wanted it to replace the wallet that is sent to the pool with my own, while maintaining the miner's original name.

Confirmed working on Alpereum and Ethermine.

# Setup
Create (or modify the example proxy.bat) a batch file to use the proxy.

### Proxy
* \<Local Port\> is the port the proxy listens on for requests from Claymore
* \<remote host\> is the address for the Pool you want to connect to
* \<remote port\> is the port of the Pool you want to connect to
* \<allowed IP\> is the IP address you want to accept connections from. User 0.0.0.0 to allow all connections.
* \<Your Wallet Address\> is the address you want to send to the server/replace the DevFee with
* \<Identify DevFee\> True/False - Show the DevFee shares as 'DevFee' when submitting shares to pool
* \<Log to file\> True/False - Output all JSON between server and client to a file in the same directory
* \<debug\> show debug messages in the console

Example Batch file:

    MinerProxy.exe <local port> <remote host> <remote port> <Allowed IP> <Your Wallet Address> <Identify DevFee> <Log to file> <debug>
    MinerProxy.exe 9000 us1.ethermine.org 4444 127.0.0.1 0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332 True False False

### Claymore
-epool <Your Server's Address> <Local Port> -ewal
    
Example Claymore batch file:

    setx GPU_FORCE_64BIT_PTR 0
    setx GPU_MAX_HEAP_SIZE 100
    setx GPU_USE_SYNC_OBJECTS 1
    setx GPU_MAX_ALLOC_PERCENT 100
    setx GPU_SINGLE_ALLOC_PERCENT 100
    EthDcrMiner64.exe -epool 127.0.0.1:9000 -ewal <0x Wallet Address>.RigName -asm 2 -epsw x


Most debug output is disabled currently, even when enabled.

# Stale shares
If you run the Proxy on your local network, Claymore will display this error:

    Miner detected that you use local pool or local stratum proxy.
    This mode is not currently supported and will cause more stale shares.

I'm not sure how to resolve this, other than running the proxy from a remote computer.
My mining rigs are off-site, so I'm run the proxy from home.
Whether this will actually cause stale shares, I don't know. There's a lot of suspicion behind Claymore and its handling of Stale shares, but I'm open to speculation.

# Todo
- [x] Log traffic to file
- [x] Deserialize/Serialize JSON objects
- [x] Replace all wallets with your own
- [x] Only accept from specific IP (or all)
- [x] Calculate and display hashrate (debug must be on)
- [ ] Verify support for other pools (nanopool, mininghub, etc)
- [ ] Verify support for other Ethash coins (Expanse, Ubiq, etc)
- [ ] Support other non-Ethash coins (ZEC, CryptoNote, etc)
- [ ] Allow connect from list of IPs or IP subnet
- [ ] Stats via built in server/Possibly REST API
- [ ] Auto failover if server stops accepting shares/no getWork replies
- [ ] Save/load settings from JSON file
- [ ] Verbose mode/Multiple debug levels
- [ ] Understand full Stratum protocol

## Help
I realize this code probably looks pretty horrible. I'm still quite new to C#, so if you have any recommendations or improvements, please submit a PR and hopefully I can learn from it!
