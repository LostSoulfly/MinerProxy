## Help Wanted
Javascript/HTML developer.

I've started looking into creating a WebSocket console with JavaScript and a jQuery Terminal Emulator.
But JavaScript is very foreign to me, so it's been a big slowdown.

# MinerProxy

![Screenshot](Screens/Screen.PNG)

Original proxy from https://github.com/RajanGrewal -- I've simply modified it to suit my needs.

I wanted a proxy that I could direct my miners to that would allow me to see their status easily, including submitted/accepted shares and reported hashrates.
I also wanted it to replace the wallet that is sent to the pool with my own, while maintaining the miner's original name.

Confirmed working on Alpereum and Ethermine.

# Setup
Create (or modify the example proxy.bat) a batch file to use the proxy, or simply run MinerProxy after editing the default Settings.json

### Proxy
MinerProxy loads a JSON file at startup that contains the necessary settings for it to run. You can have as many settings JSON files as you want. MinerProxy is indifferent to the file name it loads settings from. The default "Settings.json" will be loaded if no other file is supplied as a command line argument, like in a batch file or a CMD prompt:

Example Batch file, to load your custom Ethermine.json file:
```batch
@echo off
REM This is a batch file
MinerProxy.exe Ethermine.json
```
You can even drag and drop a JSON settings file onto MinerProxy.exe and it will load it.

If you want to easily make a new settings file, try this:
```batch
@echo off
REM This is a batch file
MinerProxy.exe MySettingsFileName.json
```
This will cause MinerProxy to create the MySEttingsFileName.json with the default settings, which you can then edit to your liking, and then run MinerProxy again.


### Settings.json
```
allowedAddresses: The addresses your miner will be allowed to connect from. Can specify one or more, note that 0.0.0.0 means ALL.
localPort: the port MinerProxy will listen on. You choose this port, just make sure you point Claymore to it.
remoteHost: your pool's address/hostname
remotePort: your pool's stratum port, check their website
webSocketPort: Port for web server to listen on
log: Log packets and console to file
debug: Show debug messages, can be toggled with D key in console.   
identifyDevFee: Show RigName as DevFee when it is detected  
showEndpointInConsole: Shows IP:Port for connected miners. Can be disabled with E key in console.
showRigStats: shows rig stats every rigStatsIntervalSeconds, like claymore does updates for speeds and temps. S key to disable in console.
useDotWithRigName: If your pool allows a <wallet>.<rig> format, set this to true.
useSlashWithRigName: If your pool allows a <wallet>/<rig> format, set this to true.
useWorkerWithRigName: If your pool uses -eworker, set this to true (and report back, because I don't use a pool that does this!)
colorizeConsole: Adds color to the console. Yellows, greens, reds, etc.
replaceWallet: Replace all wallets (including DevFee wallets)
useWebSockServer: Listen for connectionson webSocketPort. Will you be using the (partially implemented) web UI?
useRigNameAsEndPoint: When showing Endpoint in log output, this will replace the IP address with the Rig's reported name.
donateDevFee: Default is false, but setting to true enables donation of a percentage of DevFee connections.
percentToDonate: The percentage of DevFee connections you'd like to donate, see more below.
rigStatsIntervalSeconds: Console prints stats (hashrate, shares, time connected) for each Rig x seconds.
walletAddress: You can specify a wallet, or a wallet and a RigName (with a . or /)   
devFeeWalletAddress: Replace DevFee wallet with this wallet, otherwise leave blank for default wallet.
minedCoin: Set the coin you want to mine, check below for details on supported coins
```

It's important to note that you don't have to supply a RigName in your walletAddress. If you just put your wallet in there, so long as you have the proper useDot/useSlash/useWorker, your miner will be identified (and reported to the pool) correctly but the wallet will still be replaced with walletAddress.

### Donation
If you choose to Donate a portion of DevFee to MinerProxy maintainers, this is how it works:
Every time Claymore starts mining the DevFee, it starts a new connection to the pool (make sure you don't have "-allpools 1" set) to mine for 36 or 72 seconds, depending on whether you're dual mining. Let's assume that you want to donate and have set "percentToDonate" to "10" in the JSON file, in which case you would be donating only the DevFee 10% of the time, and keeping the DevFee 90% of the time.



### Coins

Coins currently supported
- [x] ETH/ETC: Ethereum + Classic
- [x] UBQ/EXP: Partial, try using ETH as minedCoin
- [x] NICEHASH: Thanks, @Samut3!
- [x] SIA: Partial, early stages
- [x] XMR: Partial, early stages
- [X] ZEC: Partial, only tested with HUSH (EWBF/Claymore)
- [ ] PASC
- [ ] LBRY
- [ ] CRY

### Claymore
```batch
setx GPU_FORCE_64BIT_PTR 0
setx GPU_MAX_HEAP_SIZE 100
setx GPU_USE_SYNC_OBJECTS 1
setx GPU_MAX_ALLOC_PERCENT 100
setx GPU_SINGLE_ALLOC_PERCENT 100
::-epool <MinerProxy's IP>:<Local Port> -ewal <Your wallet>.<RigName>
EthDcrMiner64.exe -epool 127.0.0.1:9000 -ewal 0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332.Rig1 -epsw x
::or
::-epool <MinerProxy's IP>:<Local Port> -ewal <Your wallet>/<RigName>
EthDcrMiner64.exe -epool 127.0.0.1:9000 -ewal 0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332/Rig1 -epsw x
```

# Stale shares
If you run the Proxy on your local network, Claymore will display this error:

    Miner detected that you use local pool or local stratum proxy.
    This mode is not currently supported and will cause more stale shares.

I'm not sure how to resolve this, other than running the proxy from a remote computer.
My mining rigs are off-site, so I'm run the proxy from home.
Whether this will actually cause stale shares, I don't know. There's a lot of suspicion behind Claymore and its handling of Stale shares, but I'm open to speculation.

My personal solution involves using my external, public IP address as my pool address in my mining software, with port forwarding enabled on my router to redirect the connections to MinerProxy. With this, Claymore doesn't seem to realize it's a local proxy and most importantly, stale shares drop significantly.

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
