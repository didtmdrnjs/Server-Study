using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Data;

namespace TestServer
{
    internal class Program
    {
        private static bool isRunning = false;
        
        static void Main(string[] args)
        {
            Console.Title = "Test Server";
            isRunning = true;

            // Thread에서 실행 될 메서드를 지정해서 Thread 인스턴스 생성
            Thread mainThread = new Thread(new ThreadStart(MainThread));
            // Thread 시작
            mainThread.Start();

            // Server의 Start() 실행
            Server.Start(50, 26950);
        }

        private static void MainThread()
        {
            Console.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                // 일정 주기로 GameLogic.Update() 실행
                while (_nextLoop < DateTime.Now)
                {
                    // GameLogic.Update() 실행
                    GameLogic.Update();

                    // 다음 루프의 실행시간 계산
                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (_nextLoop > DateTime.Now)
                    {
                        // 현재 시간이 다음 루프의 실행 시간과 같아질 때까지 대기
                        Thread.Sleep(_nextLoop - DateTime.Now);
                    }
                }
            }
        }
    }
}