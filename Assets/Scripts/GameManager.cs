using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
            }
            return instance;
        }
    }
    static GameManager instance;

    public bool IsGameActive { get; private set; }

    public Text scoreText;

    public Color[] playerColors = new Color[2];
    public Transform[] spawnPositionTransforms = new Transform[2];

    public GameObject ballPrefab;

    public Text gameoverText;
    public GameObject gameoverPanel;

    Dictionary<int, ulong> playerNumberClientIdMap = new Dictionary<int, ulong>();

    int[] playerScores = new int[2];

    const int WinScore = 11;

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            SpawnPlayer();
            SpawnBall();
        }

        IsGameActive = true;

        gameoverPanel.SetActive(false);

        UpdateScoreTextClientRpc(0, 0);

        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (IsGameActive){
            ExitGame();
        }
    }

    void SpawnPlayer()
    {
        if(NetworkManager.ConnectedClientsList.Count != 2)
        {
            Debug.LogError("Pong can only be played by 2 players...");
            return;
        }

        var playerPrefab = NetworkManager.NetworkConfig.PlayerPrefab;

        for(var i = 0; i < 2; i++)
        {
            var client = NetworkManager.ConnectedClientsList[i];

            playerNumberClientIdMap[i] = client.ClientId;

            var spawnPosition = spawnPositionTransforms[i].position;
            var playerColor = playerColors[i];

            var playerGameobject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            var playerPaddle = playerGameobject.GetComponent<PlayerPaddle>();
            playerPaddle.NetworkObject.SpawnAsPlayerObject(client.ClientId);

            playerPaddle.SpawnToPositionClientRpc(spawnPosition);
            playerPaddle.SetRendererColorClientRpc(playerColor);
        }
    }

    void SpawnBall()
    {
        var ballGameObject = Instantiate(ballPrefab, Vector2.zero, Quaternion.identity); 
        var ball = ballGameObject.GetComponent<Ball>();
        ball.NetworkObject.Spawn();
    }

    public void AddScore(int playerNumber, int score)
    {
        playerScores[playerNumber] += score;
        UpdateScoreTextClientRpc(playerScores[0], playerScores[1]);

        if (playerScores[playerNumber] >= WinScore)
        {
            var winnerId = playerNumberClientIdMap[playerNumber];
            EndGame(winnerId);
        }
    }

    [ClientRpc]
    void UpdateScoreTextClientRpc(int player0Score, int player1Score)
    {
        scoreText.text = $"{player0Score} : {player1Score}";
    }

    public void EndGame(ulong winnerId)
    {
        if (!IsServer)
        {
            return;
        }

        var ball = FindFirstObjectByType<Ball>();
        ball.NetworkObject.Despawn();

        EndGameClientRpc(winnerId);
    }

    [ClientRpc]
    public void EndGameClientRpc(ulong winnerId)
    {
        IsGameActive = false;

        if (winnerId == NetworkManager.LocalClientId)
        {
            gameoverText.text = "You Win!";
        }
        else
        {
            gameoverText.text = "You Lose!";
        }

        gameoverPanel.SetActive(true);
    }

    public void ExitGame()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("Menu");
    }
}
