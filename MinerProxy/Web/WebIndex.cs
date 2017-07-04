using System.Collections.Generic;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using System.Text;
using WebSocketSharp;
using MinerProxy.Miners;

namespace MinerProxy.Web
{
    public class WebIndex : WebSocketBehavior
    {

        internal static void OnGet(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var resp = e.Response;
            var path = req.RawUrl;
            string body = "<html><body style=\"background-color:black\">" + "\n";
            string footer = "\n" + "</body></html>";
            string output = null;

            if (path == "/")
                path += "index.html";
            
            if (path.ToLower().Contains("/console"))
            {

                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;

                var queue = new List<ConsoleList>(Program._webConsoleQueue);

                foreach (ConsoleList cl in queue)
                {
                    output += string.Format("<font face = \"Lucida Console\" color={0}>" + cl.message + "</font>" + "<br>\n", cl.color.ToString());
                }
                resp.WriteContent(Encoding.UTF8.GetBytes(body + output + footer));
                return;
            }

            if (path.ToLower().Contains("/miners"))
            {
                output += ("<font face = \"Lucida Console\" color=White>");
                for (int i = 0; i < Program._minerStats.Count; i++)
                {
                    output += string.Format("Miner displayName: {0}<br>\n", Program._minerStats[i].displayName);
                    output += string.Format("Miner workerName: {0}<br>\n", Program._minerStats[i].workerName);
                    output += string.Format("Miner rigName: {0}<br>\n", Program._minerStats[i].rigName);
                    output += string.Format("Miner numberOfConnects: {0}<br>\n", Program._minerStats[i].numberOfConnects);
                    output += string.Format("Miner connectionAlive: {0}<br>\n", Program._minerStats[i].connectionAlive);
                    output += string.Format("Miner endPoint: {0}<br>\n", Program._minerStats[i].endPoint);
                    output += string.Format("Miner connectionName: {0}<br>\n", Program._minerStats[i].connectionName);
                    output += string.Format("Miner firstConnectTime: {0}<br>\n", Program._minerStats[i].firstConnectTime.ToString());
                    output += string.Format("Miner connectionStartTime: {0}<br>\n", Program._minerStats[i].connectionStartTime.ToString());
                    output += string.Format("Miner totalTimeConnected: {0}<br>\n", Program._minerStats[i].totalTimeConnected.ToString());
                    output += string.Format("Miner submittedShares: {0}<br>\n", Program._minerStats[i].submittedShares);
                    output += string.Format("Miner acceptedShares: {0}<br>\n", Program._minerStats[i].acceptedShares);
                    output += string.Format("Miner rejectedShares: {0}<br>\n", Program._minerStats[i].rejectedShares);
                    output += string.Format("Miner hashrate: {0}<br>\n", Program._minerStats[i].hashrate);
                    output += string.Format("Miner GetAverageHashrate: {0}<br>\n", Program._minerStats[i].GetAverageHashrate());
                    output += string.Format("Miner Wallets:<br>\n");
                    output += string.Format(string.Join("\n", MinerManager.GetMinerWallets(Program._minerStats[i].displayName).ToArray()) + "<br>\n");
                }
                output += "</font>";
                    resp.WriteContent(Encoding.UTF8.GetBytes(body + output + footer));
                return;
            }

            if (path.ToLower().Contains("/status"))
            {
                
                resp.WriteContent(Encoding.UTF8.GetBytes("True"));
                return;
            }

            if ((path.IndexOfAny(new char[] { '*', '&', '#', '~', '^', '\\', '\0'}) >= 0) | path.Contains("..") == true)
            {  //prevent directory traversal attacks
                if (Program.settings.debug)
                    Logging.Logger.LogToConsole(string.Format("Http InvalidChars 404: {0} by {1}", path, e.Request.RemoteEndPoint));
                resp.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            var content = Program.webSock.GetFile(path);
            if (content == null)
            {

                if (Program.settings.debug)
                    Logging.Logger.LogToConsole(string.Format("Http request 404: {0} by {1}", path, e.Request.RemoteEndPoint));
                resp.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            

            if (path.EndsWith(".html"))
            {
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
            }
            else if (path.EndsWith(".js"))
            {
                resp.ContentType = "application/javascript";
                resp.ContentEncoding = Encoding.UTF8;
            }

            Logging.Logger.LogToConsole(string.Format("Http request: {0} by {1}", path, e.Request.RemoteEndPoint));

            resp.WriteContent(content);
        }
    }
}
