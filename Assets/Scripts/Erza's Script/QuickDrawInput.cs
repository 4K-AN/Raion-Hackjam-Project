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

    private void Awake()
    {
        // Membuat instance baru dari sistem input kita
        gameInput = new GameInput();
    }

    private void OnEnable()
    {
        // Mengaktifkan Action Map "Player" agar bisa didengarkan
        gameInput.Player.Enable();

        // Berlangganan ke setiap aksi dan menghubungkannya ke event yang sesuai
        gameInput.Player.ReadyP1.performed += ctx => OnReady?.Invoke(1);
        gameInput.Player.ReadyP2.performed += ctx => OnReady?.Invoke(2);
        
        gameInput.Player.ShootP1.performed += ctx => OnShoot?.Invoke(1);
        gameInput.Player.ShootP2.performed += ctx => OnShoot?.Invoke(2);

        // Untuk sequence, kita perlu mengambil tombol spesifik yang ditekan
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
        // Menonaktifkan Action Map saat objek ini tidak aktif
        gameInput.Player.Disable();
    }
}