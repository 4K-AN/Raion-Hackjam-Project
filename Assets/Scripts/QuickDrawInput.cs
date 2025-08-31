using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class QuickDrawInput : MonoBehaviour
{
    private GameInput gameInput;

    public delegate void PlayerAction(int playerID);
    public static event PlayerAction OnReady;
    public static event PlayerAction OnShoot;
    public static event System.Func<int, Key, bool> OnSequence;

    private void Awake()
    {
        gameInput = new GameInput();
    }

    private void OnEnable()
    {
        gameInput.Player.Enable();

        gameInput.Player.ReadyP1.performed += ctx => OnReady?.Invoke(1);
        gameInput.Player.ReadyP2.performed += ctx => OnReady?.Invoke(2);

        gameInput.Player.ShootP1.performed += ctx => OnShoot?.Invoke(1);
        gameInput.Player.ShootP2.performed += ctx => OnShoot?.Invoke(2);

        gameInput.Player.SeqP1.performed += ctx =>
        {
            if (ctx.control is KeyControl keyCtrl)
                OnSequence?.Invoke(1, keyCtrl.keyCode);
        };

        gameInput.Player.SeqP2.performed += ctx =>
        {
            if (ctx.control is KeyControl keyCtrl)
                OnSequence?.Invoke(2, keyCtrl.keyCode);
        };
    }

    private void OnDisable()
    {
        gameInput.Player.Disable();
    }
}
