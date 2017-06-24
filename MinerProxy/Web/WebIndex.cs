using System;
using System.Collections.Generic;
using MinerProxy.Logging;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using System.Text;
using WebSocketSharp;

namespace MinerProxy.Web
{
    public class WebIndex : WebSocketBehavior
    {

        internal static void OnGet(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var resp = e.Response;

            var path = req.RawUrl;


            if (path == "/")
                path += "index.html";

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
