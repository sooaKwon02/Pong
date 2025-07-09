using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    public Text lobbyText;
    const int MinimumReadyCountToStartGame = 2;
    Dictionary<ulong, bool> _clientReadyStates = new Dictionary<ulong, bool>();

    public override void OnNetworkSpawn()
    {
        _clientReadyStates.Add(NetworkManager.LocalClientId, false);

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            NetworkManager.SceneManager.OnLoadComplete += OnClientSceneLoadComplete;
        }

        UpdateLobbyText();
    }

    private void OnDisable()
    {
        if (!IsServer)
        {
            return;
        }

        if(NetworkManager.Singleton != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.SceneManager.OnLoadComplete -= OnClientSceneLoadComplete;
        }
    }

    void OnClientConnected(ulong clientId)
    {
        _clientReadyStates.Add(clientId, false);
        UpdateLobbyText();
    }

    void OnClientSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode) 
    {
        if(sceneName != "Lobby")
        {
            return;
        }

        foreach(KeyValuePair<ulong, bool> pair in _clientReadyStates)
        {
            var id = pair.Key;
            var isReady = pair.Value;
            SetClientIsReadyClientRpc(id, isReady);
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (_clientReadyStates.ContainsKey(clientId))
        {
            _clientReadyStates.Remove(clientId);
        }

        RemovePlayerClientRpc(clientId);
        UpdateLobbyText();
    }

    [ClientRpc]
    void SetClientIsReadyClientRpc(ulong clientId, bool isReady)
    {
        if (IsServer)
        {
            return;
        }

        _clientReadyStates[clientId] = isReady;
        UpdateLobbyText();
    }

    [ClientRpc]
    void RemovePlayerClientRpc(ulong clientId)
    {
        if (IsServer)
        {
            return;
        }

        _clientReadyStates.Remove(clientId);
        UpdateLobbyText();
    }

    void UpdateLobbyText()
    {
        var stringBuilder = new StringBuilder();

        foreach (var pair in _clientReadyStates)
        {
            var clientId = pair.Key;
            var isReady = pair.Value;

            if (isReady)
            {
                stringBuilder.AppendLine($"PLAYER_{clientId} : READY");
            }
            else
            {
                stringBuilder.AppendLine($"PLAYER_{clientId} : NOT READY");
            }
        }

        lobbyText.text = stringBuilder.ToString();
    }

    bool CheckIsReadyToStart()
    {
        if(_clientReadyStates.Count < MinimumReadyCountToStartGame)
        {
            return false;
        }

        foreach(var isReady in _clientReadyStates.Values)
        {
            if (!isReady)
            {
                return false;
            }
        }
        return true;
    }

    void StartGame()
    {
        if (!IsServer)
        {
            return;
        }

        NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.SceneManager.OnLoadComplete -= OnClientSceneLoadComplete;

        NetworkManager.SceneManager.LoadScene("InGame", LoadSceneMode.Single);
    }

    public void SetPlayerIsReady()
    {
        var localClientId = NetworkManager.LocalClientId;

        _clientReadyStates[localClientId] = !_clientReadyStates[localClientId];
        var isReady = _clientReadyStates[localClientId];

        UpdateLobbyText();

        if(IsServer)
        {
            SetClientIsReadyClientRpc(localClientId, isReady);

            if(CheckIsReadyToStart())
            {
                StartGame();
            }
        }
        else
        {
            SetClientIsReadyServerRpc(localClientId, isReady);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SetClientIsReadyServerRpc(ulong clientId, bool isReady)
    {
        _clientReadyStates[clientId] = isReady;

        SetClientIsReadyClientRpc(clientId, isReady);
        UpdateLobbyText();

        if(CheckIsReadyToStart())
        {
            StartGame();
        }
    }
}
