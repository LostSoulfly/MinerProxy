using System;
using System.Net.Sockets;

namespace TcpProxy
{
    internal sealed class Redirector : IDisposable
    {
        private readonly string m_name;
        private Session m_client, m_server;
        private bool m_alive;

        public Redirector(Socket client,string ip, int port)
        {
            m_name = client.RemoteEndPoint.ToString();

            Console.WriteLine("Proxy session created ({0})", m_name);

            m_alive = false;

            m_client = new Session(client);
            m_client.OnDataReceived = OnClientPacket;
            m_client.OnDisconnected = Dispose;
            
            var outSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            outSocket.BeginConnect(ip, port, EndConnect, outSocket);
        }

        private void EndConnect(IAsyncResult iar)
        {
            try
            {
                var outSocket = iar.AsyncState as Socket;

                outSocket.EndConnect(iar);

                m_server = new Session(outSocket);
                m_server.OnDataReceived = OnServerPacket;
                m_server.OnDisconnected = Dispose;

                m_alive = true;

                m_server.Receive();
                m_client.Receive();
            }
            catch (SocketException se)
            {
                m_client.Dispose();
                Console.WriteLine("Connection bridge failed with {0} ({1})",se.ErrorCode,m_name);
            }
        }

        private void OnServerPacket(byte[] buffer,int length)
        {
            if (m_alive && m_client.Disposed == false)
                m_client.Send(buffer, length);
        }
        private void OnClientPacket(byte[] buffer,int length)
        {
            if (m_alive && m_server.Disposed == false)
                m_server.Send(buffer, length);
        }

        public void Dispose()
        {
            if (m_alive)
            {
                m_alive = false;

                if (m_client != null)
                    m_client.Dispose();

                if (m_server != null)
                    m_server.Dispose();

                Console.WriteLine("Proxy session ended ({0})", m_name);
            }
        }
    }
}
