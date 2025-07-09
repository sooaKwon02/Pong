using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public Text infoText;
    public InputField hostAddressInputField;
    const ushort DefaultPort = 7777;

    private void Awake()
    {
        infoText.text = string.Empty;
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    }

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    private void OnDisable()
    {
        if(NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }

    void OnClientDisconnectCallback(ulong obj)
    {
        var disconnectReason = NetworkManager.Singleton.DisconnectReason;
        infoText.text = disconnectReason;
        Debug.Log(disconnectReason);
    }

    void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if(NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
        }
        else
        {
            response.Approved = false;
            response.Reason = "Max player in session is 2";
        }
    }

    public void CreateGameAsHost()
    {
        var networkManager = NetworkManager.Singleton;
        var transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        transport.ConnectionData.Port = DefaultPort;

        if (networkManager.StartHost())
        {
            networkManager.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            infoText.text = "Host failed to start";
            Debug.LogError("Host failed to start");
        }
    }

    public void JoinGameAsClient()
    {
        var networkManager = NetworkManager.Singleton;
        var transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;

        transport.SetConnectionData(hostAddressInputField.text, DefaultPort);

        if (!NetworkManager.Singleton.StartClient())
        {
            infoText.text = "Client failed to start";
            Debug.LogError("Client failed to start");
        }
    }
}
