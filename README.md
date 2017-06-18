# MinerProxy

![Screenshot](Screens/Screen.PNG)

Original proxy from https://github.com/RajanGrewal -- I've simply modified it to suit my needs.

I wanted a proxy that I could direct my miners to that would allow me to see their status easily, including submitted/accepted shares and reported hashrates.
I also wanted it to replace the wallet that is sent to the pool with my own, while maintaining the miner's original name.

Confirmed working on Alpereum and Ethermine.

# Setup
Create (or modify the example proxy.bat) a batch file to use the proxy, or simply run MinerProxy after editing the default Settings.json

### Proxy
Now only accepts a JSON file as the passed argument. If it doesn't exist, a generic one will be created for you.
Can be run without an argument passed, this causes MinerProxy to load Settings.json.

Example Batch file:

    MinerProxy.exe <JSON File>
    MinerProxy.exe Ethermine.json

### Claymore
-epool <Your Server's Address>:<Local Port> -ewal <Your wallet>
    
Example Claymore batch file:

    setx GPU_FORCE_64BIT_PTR 0
    setx GPU_MAX_HEAP_SIZE 100
    setx GPU_USE_SYNC_OBJECTS 1
    setx GPU_MAX_ALLOC_PERCENT 100
    setx GPU_SINGLE_ALLOC_PERCENT 100
    EthDcrMiner64.exe -epool 127.0.0.1:9000 -ewal <0x Wallet Address>.RigName -epsw x



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
- [X] Save/load settings from JSON file
- [X] Allow connect from list of IPs or IP subnet
- [X] Display stats for each rig on a configurable timer
- [X] Colorized output for easy readability
- [X] Support '/' and '.' RigNames for pools
- [ ] Verify support for other pools (nanopool, mininghub, etc)
- [ ] Verify support for other Ethash coins (Expanse, Ubiq, etc)
- [ ] Support other non-Ethash coins (ZEC, CryptoNote, etc)
- [ ] Multiple proxies in same MinerProxy instance
- [ ] Stats via built in server/Possibly REST API
- [ ] Auto failover if server stops accepting shares/no getWork replies
- [ ] Verbose mode/Multiple debug levels
- [ ] Understand full Stratum protocol

## Help
I realize this code probably doesn't look pretty. I'm still quite new to C#, so if you have any recommendations or improvements, please submit a PR and hopefully I can learn from it! At the end of the day, regardless of whether this causes stale shares or other issues, I've had fun writing it.
