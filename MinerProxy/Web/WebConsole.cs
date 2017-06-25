using WebSocketSharp;
using WebSocketSharp.Server;

namespace MinerProxy.Web
{
    public class WebConsole : WebSocketBehavior
    {

        protected override void OnClose(CloseEventArgs e)
        {
            Logging.Logger.LogToConsole(string.Format("WebSocket {0} closed: {1}", this.Context.UserEndPoint.ToString(), e.Reason));
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Logging.Logger.LogToConsole(string.Format("WebSocket {0} error: {1}", this.Context.UserEndPoint.ToString(), e.Message));
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Logging.Logger.LogToConsole(string.Format("WebSocket message: {0}", e.ToString()));
        }

        protected override void OnOpen()
        {
            Logging.Logger.LogToConsole(string.Format("WebSocket connected: {0}", this.Context.UserEndPoint.ToString()));
            ;
        }
    }
}
