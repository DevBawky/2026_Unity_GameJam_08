using System.Collections;
using UnityEngine;

public sealed class CartStations : MonoBehaviour
{
    [SerializeField] private CartMotion cart;
    [SerializeField] private GameObject carriedShell;
    [SerializeField] private GameObject storedShell;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform cannonMuzzle;
    [SerializeField] private UnityEngine.UI.Image princeAnxietyBar;
    [SerializeField] private UnityEngine.UI.Image princessAnxietyBar;
    [SerializeField, Range(0f, 1f)] private float princeAnxiety = 0.5f;
    [SerializeField, Range(0f, 1f)] private float princessAnxiety = 0.5f;
    [SerializeField, Min(0f)] private float anxietyPerSecond = 0.015f;
    [SerializeField, Range(0f, 1f)] private float calmAmount = 0.25f;
    [SerializeField, Min(0f)] private float fuseDelay = 2.5f;
    [SerializeField, Min(0f)] private float projectileSpeed = 10f;
    private bool carrying;
    private bool deposited;
    private bool loaded;
    private bool fuseLit;
    private CannonAim cannonAim;
    public bool IsFiring => fuseLit;

    private void Awake() { cannonAim = GetComponent<CannonAim>(); Refresh(); }
    private void Update()
    {
        princeAnxiety = Mathf.Clamp01(princeAnxiety + anxietyPerSecond * Time.deltaTime);
        princessAnxiety = Mathf.Clamp01(princessAnxiety + anxietyPerSecond * Time.deltaTime);
        Refresh();
    }
    public void Enter(StationAction action)
    {
        if (action != StationAction.ShellDeposit || !carrying || deposited) return;
        carrying = false;
        deposited = true;
        Refresh();
    }
    public void Succeed(StationAction action)
    {
        switch (action)
        {
            case StationAction.RightHorse: if (cart != null) cart.BoostHorse(true); break;
            case StationAction.LeftHorse: if (cart != null) cart.BoostHorse(false); break;
            case StationAction.RoyalFamily:
                princeAnxiety = Mathf.Clamp01(princeAnxiety - calmAmount);
                princessAnxiety = Mathf.Clamp01(princessAnxiety - calmAmount);
                break;
            case StationAction.ShellPile: if (!carrying && !deposited) carrying = true; break;
            case StationAction.Reload:
                if (deposited && !loaded && !fuseLit) { deposited = false; loaded = true; }
                break;
        }
        Refresh();
    }
    public void Ignite()
    {
        if (loaded && !fuseLit && shellPrefab != null && cannonMuzzle != null) StartCoroutine(Fire());
    }
    private IEnumerator Fire()
    {
        fuseLit = true;
        yield return new WaitForSeconds(fuseDelay);
        loaded = false;
        fuseLit = false;
        GameObject shot = Instantiate(shellPrefab, cannonMuzzle.position, cannonMuzzle.rotation);
        Projectile projectile = shot.GetComponent<Projectile>();
        if (projectile != null) projectile.Launch(cannonAim != null ? (Vector3)cannonAim.Direction : cannonMuzzle.right, projectileSpeed);
        Refresh();
    }
    private void Refresh()
    {
        if (carriedShell != null) carriedShell.SetActive(carrying);
        if (storedShell != null) storedShell.SetActive(deposited);
        if (princeAnxietyBar != null) princeAnxietyBar.fillAmount = princeAnxiety;
        if (princessAnxietyBar != null) princessAnxietyBar.fillAmount = princessAnxiety;
    }
}
