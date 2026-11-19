using UnityEngine;

// 도로 장애물 예고 "!". 서버가 예정 차선 높이에 소환하고, 각 클라이언트는 화면 왼쪽 끝 안쪽에 붙여 보여준다.
// 일반 주행·마부석에서만 보이고 다른 작업대에 들어가 있으면 숨긴다(웹판 v10 drawRoadWarning).
public sealed class ObstacleWarning : MonoBehaviour
{
    [SerializeField] private SpriteRenderer icon;
    [Tooltip("화면 왼쪽 끝에서 안쪽으로 띄우는 거리(월드)")]
    [SerializeField, Min(0f)] private float edgeInset = 0.9f;

    private void LateUpdate()
    {
        var cam = Camera.main;
        var local = NetworkPlayer.LocalPlayer;
        bool atOtherStation = local != null && local.Timing.HasStationAccess && local.Timing.CurrentAction != StationAction.Driver;
        bool visible = cam != null && !atOtherStation;
        icon.enabled = visible;
        if (!visible) return;

        Vector3 position = transform.position;
        position.x = cam.transform.position.x - cam.orthographicSize * cam.aspect + edgeInset;
        transform.position = position;
    }
}
