using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShellSupplyView : MonoBehaviour
{
    [SerializeField] private Image pile;
    [SerializeField] private TMP_Text stockLabel;
    private int displayed = -1;

    private void LateUpdate()
    {
        var game = CartStations.Instance;
        bool running = CartStations.Running;
        bool test = running && game.Endless.Value;
        int stock = running ? game.ShellStock : 0;
        pile.enabled = running && (test || stock > 0);
        stockLabel.transform.parent.gameObject.SetActive(running && !test);
        if (!running || test || displayed == stock) return;
        displayed = stock;
        stockLabel.SetText("{0}", stock);
    }
}
