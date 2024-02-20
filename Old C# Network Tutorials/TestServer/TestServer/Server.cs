using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace TestServer
{
    internal class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public static void Start(int _maxPlayers, int _port)
        {
            // 최대 플레이어 및 포트 지정
            MaxPlayers = _maxPlayers;
            Port = _port;

            // InitializeServerData() 실행
            Console.WriteLine("Starting server...");
            InitializeServerData();

            // 지정된 포트를 통해 들어오는 모든 클라이언트의 요청을 받을 수 있도록 설정
            tcpListener = new TcpListener(IPAddress.Any, Port);

            // 들어오는 연결 받기 시작
            tcpListener.Start();

            // 들어오는 연결을 받아들이는 비동기 작업 수행
            // AsyncCallback : 작업이 완료되었을 때 호출할 메서드를 참조하는 AsyncCallback 대리자
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            // 해당 포트를 사용하는 새로운 UdpClient 생성
            udpListener = new UdpClient(Port);
            // 원격 호스트로 부터 비동기적으로 데이터그램을 수신
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server started on {Port}.");
        }

        private static void TCPConnectCallback(IAsyncResult _result)
        {
            // 들어오는 연결시도를 비동기적으로 수락하고, 원격 호스트 통신을 처리하기 위해 새 TcpClient를 만든다.
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);

            // BeginAcceptTcpClient를 다시 호출함으로써 계속해서 요청을 받아들일 수 있다.
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");

            // TcpClient가 할당되지 않을 곳을 찾아 클라이언트 할당
            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                    return;
                }
            }

            Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
        }

        private static void UDPReceiveCallback(IAsyncResult _result)
        {
            try {
                // 엔드 포인트 생성
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                // 비동기 수신 종료, 받은 데이터 저장
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                // 비동기 읽기 재시작
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (_data.Length < 4)
                {
                    return;
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();

                    if (_clientId == 0)
                    {
                        return;
                    }

                    if (clients[_clientId].udp.endPoint == null)
                    {
                        // 클라이언트의 엔드 포인트를 서버로 지정
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    // 엔드 포인트가 같으면 HandleData 실행
                    if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error receiving UDP data: {_ex}");
            }
        }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    // 데이터를 비동기적으로 전송
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP {_ex}");
            }
        }

        private static void InitializeServerData()
        {
            // 서버에 최대 플레이어 수만큼 클라이언트 인스턴스 생성
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                // 클라이언트에게서 welcomeReceived 패킷이 왔을 때 실행할 함수로 ServerHandle.WelcomeReceived() 지정
                {  (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                // 클라이언트에게서 playerMovement 패킷이 왔을 때 실행할 함수로 ServerHandle.PlayerMovement() 지정
                {  (int)ClientPackets.playerMovement, ServerHandle.PlayerMovement }
            };
            Console.WriteLine("Initialized packets.");
        }
    }
}