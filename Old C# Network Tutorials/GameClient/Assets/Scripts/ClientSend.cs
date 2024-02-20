using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
    private static void SendTCPData(Packet _packet)
    {
        // 패킷에 보내는 데이터의 길이 추가
        _packet.WriteLength();
        // 데이터 전송
        Client.instance.tcp.SendData(_packet);
    }

    private static void SendUDPData(Packet _packet)
    {
        // 패킷에 데이터 길이 삽입
        _packet.WriteLength();
        // 데이터 전송
        Client.instance.udp.SendData(_packet);
    }

    #region Packets
    public static void WelcomeReceived()
    {
        // welcomeReceived 패킷 생성 후 전송
        using (Packet _packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            _packet.Write(Client.instance.myId);
            _packet.Write(UIManager.instance.usernameField.text);

            SendTCPData(_packet);
        }
    }

    public static void PlayerMovement(bool[] _inputs)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            // 입력 길이 삽입
            _packet.Write(_inputs.Length);
            // 입력 삽임
            foreach (bool _input in _inputs)
            {
                _packet.Write(_input);
            }
            // 플레이어 방향 삽입
            _packet.Write(GameManager.players[Client.instance.myId].transform.rotation);

            // UDP 전송
            SendUDPData(_packet);
        }
    }

    public static void PlayerShoot(Vector3 facing)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerShoot))
        {
            // 방향 삽입
            _packet.Write(facing);

            // TCP 전송
            SendTCPData(_packet);
        }
    }

    public static void PlayerThrowItem(Vector3 facing)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerThrowItem))
        {
            _packet.Write(facing);

            SendTCPData(_packet);
        }
    }
    #endregion
}
