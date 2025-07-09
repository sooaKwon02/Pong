using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPaddle : NetworkBehaviour
{
    SpriteRenderer _spriteRenderer;
    public float speed = 10f;

    public InputActionReference moveRef;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    [ClientRpc]
    public void SetRendererColorClientRpc(Color color) 
    {
        color.a = 255f;
        _spriteRenderer.color = color;
    }

    [ClientRpc]
    public void SpawnToPositionClientRpc(Vector3 position)
    {
        transform.position = position;
    }

    private void Update()
    {
        if(GameManager.Instance != null && !GameManager.Instance.IsGameActive)
        {
            return;
        }

        if (!IsOwner)
        {
            return;
        }

        //var input = Input.GetAxis("Vertical");
        float input = moveRef.action.ReadValue<float>();

        var distance = input * speed * Time.deltaTime;
        var position = transform.position;
        position.y += distance;

        position.y = Mathf.Clamp(position.y, -4.5f, 4.5f);
        transform.position = position;
    }

    private void OnEnable()
    {
        moveRef.action.Enable();
    }

    private void OnDisable()
    {
        moveRef.action.Disable();
    }
}
