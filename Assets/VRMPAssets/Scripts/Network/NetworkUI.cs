using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    string ipAddress = "127.0.0.1";
    ushort port = 7777;

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 30), "IP Address:");
        ipAddress = GUI.TextField(new Rect(120, 10, 150, 30), ipAddress);

        GUI.Label(new Rect(10, 50, 100, 30), "Port:");
        ushort.TryParse(GUI.TextField(new Rect(120, 50, 80, 30), port.ToString()), out port);

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUI.Button(new Rect(10, 100, 200, 40), "Start Host"))
            {
                NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>()
                    .SetConnectionData("0.0.0.0", port); // Host 本机监听
                NetworkManager.Singleton.StartHost();
            }

            if (GUI.Button(new Rect(10, 150, 200, 40), "Start Client"))
            {
                NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>()
                    .SetConnectionData(ipAddress, port); // 连接到 Host
                NetworkManager.Singleton.StartClient();
            }
        }
    }
}
