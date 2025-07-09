using Unity.Netcode;
using UnityEngine;

public class PositionSync : NetworkBehaviour
{
    Vector2 _lastPosition;

    public NetworkVariable<Vector2> networkPosition = new NetworkVariable<Vector2>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner);

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            if(Vector2.Distance(_lastPosition, transform.position) > 0.001f)
            {
                _lastPosition = (Vector2)transform.position;
                networkPosition.Value = _lastPosition; 
            }
        }
        else
        {
            transform.position = (Vector3)networkPosition.Value;
        }
    }

}
