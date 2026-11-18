using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Minimal network avatar for the lobby-to-game handoff; gameplay stations are a separate migration.</summary>
public sealed class LobbyNetworkPlayer : NetworkTransform
{
    public NetworkVariable<int> Slot = new(0);
    [SerializeField] private float moveSpeed = 4f;
    protected override bool OnIsServerAuthoritative() => false;
    private void Update()
    {
        if (!IsSpawned || !IsOwner) return;
        Vector2 input = Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
        }
        if (Gamepad.current != null) input += Gamepad.current.leftStick.ReadValue();
        input = Vector2.ClampMagnitude(input, 1f);
        transform.position += new Vector3(input.x, input.y) * (moveSpeed * Time.deltaTime);
    }
}
