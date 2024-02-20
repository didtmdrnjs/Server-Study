using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    internal class GameLogic
    {
        public static void Update()
        {
            // 모든 플레이어의 위치, 방향 갱신
            foreach (Client _client in Server.clients.Values)
            {
                if (_client.player != null)
                {
                    _client.player.Update();
                }
            }

            // ThreadManager.UpdateMain() 실행
            ThreadManager.UpdateMain();
        }
    }
}
