
namespace MinerProxy.Logging
{
    class LogMessage
    {
        public string Filepath;
        public string Text;

        public LogMessage(string v1, string v2)
        {
            this.Filepath = v1;
            this.Text = v2;
        }

    }


}
