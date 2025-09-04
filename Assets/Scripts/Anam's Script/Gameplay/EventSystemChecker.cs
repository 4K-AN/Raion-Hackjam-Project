using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemChecker : MonoBehaviour
{
    void Awake()
    {
        // Cari semua EventSystem
        EventSystem[] systems = FindObjectsOfType<EventSystem>();
        if (systems.Length > 1)
        {
            // Matikan EventSystem tambahan
            foreach (var sys in systems)
            {
                if (sys != EventSystem.current)
                {
                    Debug.Log("[EventSystemChecker] Menonaktifkan EventSystem ekstra di " + sys.gameObject.name);
                    sys.gameObject.SetActive(false);
                }
            }
        }
    }
}
