using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    internal class Constants
    {
        // 초당 틱 수
        public const int TICKS_PER_SEC = 30;
        // 틱당 시간
        public const int MS_PER_TICK = 1000 / TICKS_PER_SEC;
    }
}
