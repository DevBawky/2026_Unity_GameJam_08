using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 지하층 바퀴 수리 표시: 빠진 바퀴 위치에 수리 원과 라벨을 띄우고, 다른 사람이 작업 중이면 회색으로 바꾼다.
[DefaultExecutionOrder(55)]
public sealed class WheelRepairMarker : MonoBehaviour
{
    [SerializeField] private RectTransform marker, labelPlate;
    [SerializeField] private Image pad, ring, tool, labelBackground;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Sprite inactiveTool, busyPad, busyLabel;

    [Tooltip("로컬 플레이어 목록 (씬의 MultiplayerGameScene)")]
    [SerializeField] private MultiplayerGameScene crew;
    [Tooltip("라벨 크기를 맞출 카메라들 (1P·2P 화면)")]
    [SerializeField] private CameraRig[] rigs;

    private Sprite availableTool, availablePad, availableLabel;
    private (CameraRig Rig, Camera View)[] views;

    private static Vector3 MarkerPosition(CartStations game) =>
        game.WheelWorkPoint + Vector3.up * NetworkPlayer.CellarOffset;

    internal bool OverlapsPlayer(CartStations game, Bounds body)
    {
        if (!game.WheelDetached || game.DetachedTire == null) return false;
        // Use the current wheel position before the marker's LateUpdate places it for rendering.
        float unit = game.WorldPerWebUnit;
        var markerToWorld = Matrix4x4.TRS(MarkerPosition(game), marker.rotation, new Vector3(unit, unit, 1f));
        var padTransform = pad.rectTransform;
        var worldToPad = padTransform.worldToLocalMatrix * marker.localToWorldMatrix * markerToWorld.inverse;
        return TimingArea.OverlapsPlayer(body, padTransform.rect, worldToPad);
    }

    private void Awake()
    {
        views = new (CameraRig, Camera)[rigs.Length];
        for (int i = 0; i < rigs.Length; i++) views[i] = (rigs[i], rigs[i].GetComponent<Camera>());
        availablePad = pad.sprite;
        availableTool = tool.sprite;
        availableLabel = labelBackground.sprite;
    }

    private void LateUpdate()
    {
        var game = CartStations.Instance;
        bool visible = CartStations.Running && game != null && game.WheelDetached && game.DetachedTire != null;
        if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
        if (!visible) return;

        bool spareInCellar = false;
        bool localOperator = false;
        if (crew != null)
        {
            foreach (var actor in crew.Players)
            {
                if (!actor.Local || !actor.InCellar) continue;
                spareInCellar |= actor.SpareWheel.Value;
                localOperator |= game.HoldsStation(StationAction.WheelRepair, actor);
            }
        }

        int index = (int)StationAction.WheelRepair;
        bool occupied = index < game.StationOwners.Count && game.StationOwners[index] != CartStations.NoStationOwner;
        bool busy = occupied && !localOperator;
        pad.sprite = busy ? busyPad : availablePad;
        tool.sprite = busy ? inactiveTool : availableTool;
        labelBackground.sprite = busy ? busyLabel : availableLabel;
        string message = LocalizationTable.T(game.WheelRepairSteps > 0 ? "핀 고정" : spareInCellar ? "수리 !" : "바퀴 필요");
        if (label.text != message) label.text = message;

        float phase = (float)game.NetworkManager.ServerTime.Time * 5.7f;
        ring.color = busy ? new Color32(222, 223, 223, 255) : Mathf.Sin(phase) > 0f
            ? new Color32(246, 255, 218, 255) : new Color32(210, 234, 211, 255);

        var point = game.WheelWorkPoint;
        marker.position = MarkerPosition(game);
        var parentScale = marker.parent.lossyScale;
        float unit = game.WorldPerWebUnit;
        marker.localScale = new Vector3(unit / Mathf.Abs(parentScale.x), unit / Mathf.Abs(parentScale.y), 1f);
        float labelScale = 1f;
        foreach (var (rig, view) in views)
        {
            var actor = rig.FollowedActor;
            if (!view.enabled || view.pixelHeight == 0 || actor == null || !actor.InCellar) continue;
            float pixelsPerUnit = view.pixelHeight * unit / (2f * view.orthographicSize);
            labelScale = Mathf.Max(labelScale, .9f / pixelsPerUnit);
        }
        labelPlate.localScale = new Vector3(labelScale, labelScale, 1f);
        float side = point.x < game.CartBody.bounds.center.x ? 1f : -1f;
        labelPlate.anchoredPosition = Vector2.right * (side * (31f + labelPlate.rect.width * .5f * labelScale));
    }
}
