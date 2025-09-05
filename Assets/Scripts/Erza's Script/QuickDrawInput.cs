using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class QuickDrawInput : MonoBehaviour
{
    // Variabel untuk menampung kelas input yang di-generate otomatis
    private GameInput gameInput;

    // Definisi event/sinyal yang akan dikirim ke GameManager
    public delegate void PlayerAction(int playerID);
    public static event PlayerAction OnReady;
    public static event PlayerAction OnShoot;

    // Event ini sedikit berbeda karena kita perlu mengirim tombol spesifik yang ditekan
    public static event System.Func<int, Key, bool> OnSequence;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Fallback jika Input System tidak bekerja
    [Header("Fallback Input")]
    public bool useFallbackInput = false;

    private void Awake()
    {
        try
        {
            // Membuat instance baru dari sistem input kita
            gameInput = new GameInput();
            DebugLog("GameInput instance created successfully");
        }
        catch (System.Exception e)
        {
            DebugLog($"Failed to create GameInput: {e.Message}. Using fallback input.");
            useFallbackInput = true;
        }
    }

    private void OnEnable()
    {
        if (!useFallbackInput && gameInput != null)
        {
            try
            {
                // Mengaktifkan Action Map "Player" agar bisa didengarkan
                gameInput.Player.Enable();
                DebugLog("GameInput Player actions enabled");

                // Berlangganan ke setiap aksi dan menghubungkannya ke event yang sesuai
                gameInput.Player.ReadyP1.performed += ctx => {
                    DebugLog("ReadyP1 input detected");
                    OnReady?.Invoke(1);
                };
                
                gameInput.Player.ReadyP2.performed += ctx => {
                    DebugLog("ReadyP2 input detected");
                    OnReady?.Invoke(2);
                };
                
                gameInput.Player.ShootP1.performed += ctx => {
                    DebugLog("ShootP1 input detected");
                    OnShoot?.Invoke(1);
                };
                
                gameInput.Player.ShootP2.performed += ctx => {
                    DebugLog("ShootP2 input detected");
                    OnShoot?.Invoke(2);
                };

                // Untuk sequence, kita perlu mengambil tombol spesifik yang ditekan
                gameInput.Player.SeqP1.performed += ctx => 
                {
                    if (ctx.control is KeyControl keyCtrl)
                    {
                        DebugLog($"SeqP1 input: {keyCtrl.keyCode}");
                        OnSequence?.Invoke(1, keyCtrl.keyCode);
                    }
                };

                gameInput.Player.SeqP2.performed += ctx => 
                {
                    if (ctx.control is KeyControl keyCtrl)
                    {
                        DebugLog($"SeqP2 input: {keyCtrl.keyCode}");
                        OnSequence?.Invoke(2, keyCtrl.keyCode);
                    }
                };
            }
            catch (System.Exception e)
            {
                DebugLog($"Error setting up input actions: {e.Message}. Switching to fallback.");
                useFallbackInput = true;
            }
        }
    }

    private void Update()
    {
        // Fallback input system menggunakan Unity's legacy Input
        if (useFallbackInput)
        {
            HandleFallbackInput();
        }
    }

    private void HandleFallbackInput()
    {
        // Ready inputs
        if (Input.GetKeyDown(KeyCode.Q))
        {
            DebugLog("Q key pressed (Ready P1)");
            OnReady?.Invoke(1);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            DebugLog("P key pressed (Ready P2)");
            OnReady?.Invoke(2);
        }

        // Shoot inputs (same keys for now, but could be different)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            DebugLog("Q key pressed (Shoot P1)");
            OnShoot?.Invoke(1);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            DebugLog("P key pressed (Shoot P2)");
            OnShoot?.Invoke(2);
        }

        // Sequence inputs - checking common keys
        CheckSequenceInput(KeyCode.W, Key.W, 1);
        CheckSequenceInput(KeyCode.A, Key.A, 1);
        CheckSequenceInput(KeyCode.S, Key.S, 1);
        CheckSequenceInput(KeyCode.D, Key.D, 1);
        CheckSequenceInput(KeyCode.Q, Key.Q, 1);
        CheckSequenceInput(KeyCode.E, Key.E, 1);

        CheckSequenceInput(KeyCode.I, Key.I, 2);
        CheckSequenceInput(KeyCode.J, Key.J, 2);
        CheckSequenceInput(KeyCode.K, Key.K, 2);
        CheckSequenceInput(KeyCode.L, Key.L, 2);
        CheckSequenceInput(KeyCode.U, Key.U, 2);
        CheckSequenceInput(KeyCode.O, Key.O, 2);
        CheckSequenceInput(KeyCode.P, Key.P, 2);
    }

    private void CheckSequenceInput(KeyCode keyCode, Key key, int playerID)
    {
        if (Input.GetKeyDown(keyCode))
        {
            DebugLog($"{keyCode} pressed for Player {playerID} sequence");
            OnSequence?.Invoke(playerID, key);
        }
    }

    private void OnDisable()
    {
        if (!useFallbackInput && gameInput != null)
        {
            try
            {
                // Menonaktifkan Action Map saat objek ini tidak aktif
                gameInput.Player.Disable();
                DebugLog("GameInput Player actions disabled");
            }
            catch (System.Exception e)
            {
                DebugLog($"Error disabling input actions: {e.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        if (gameInput != null)
        {
            gameInput.Dispose();
            DebugLog("GameInput disposed");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[QuickDrawInput] {message}");
        }
    }

    // Method untuk testing - bisa dipanggil dari Inspector atau script lain
    [ContextMenu("Test Q Input")]
    public void TestQInput()
    {
        DebugLog("Testing Q input manually");
        OnReady?.Invoke(1);
    }

    [ContextMenu("Test P Input")]
    public void TestPInput()
    {
        DebugLog("Testing P input manually");
        OnReady?.Invoke(2);
    }
}