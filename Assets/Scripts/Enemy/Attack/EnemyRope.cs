using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// 추격조 로프. 사거리 안에서 마차에 로프를 걸고, 이 몬스터가 사라지면 풀린다(유지 시간 없음).
// 걸린 로프 수로 마차 감속(CartMotion)과 몬스터 전체 가속(Enemy)을 계산한다.
[RequireComponent(typeof(LineRenderer))]
public class EnemyRope : NetworkBehaviour
{
    private static readonly List<EnemyRope> attached = new();

    // 추후 변경점: 계수는 첫 로프의 SO를 쓴다. 추격조마다 다른 로프 SO를 쓰게 되면 합산 규칙을 정할 것
    private static RopeSkillSO Active => attached.Count > 0 ? attached[0].skill : null;

    public static int Count => attached.Count;
    public static float CartSpeedMultiplier => Active ? Active.CartSpeedMultiplier(attached.Count) : 1f;
    public static float MonsterSpeedMultiplier => Active ? Active.MonsterSpeedMultiplier(attached.Count) : 1f;

    private bool attachedLocally;
    public bool Attached => IsServer ? attachedLocally : Connected.Value;

    private LineRenderer line;
    public NetworkVariable<bool> Connected = new(false);
    public NetworkVariable<Vector3> EndOffset = new();
    private RopeSkillSO skill;
    private Transform cart;
    private Vector3 bodyOffset;
    private Vector3 endOffset;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.enabled = false;
    }

    // 마차가 위아래로 움직이므로 추격조와 줄 끝을 마차 기준 위치로 붙잡아 같이 움직인다.
    public void Attach(RopeSkillSO ropeSkill, Transform cartTransform, Vector3 cartPoint)
    {
        if (!IsServer || Attached) return;
        skill = ropeSkill;
        cart = cartTransform;
        bodyOffset = transform.position - cart.position;
        endOffset = cartPoint - cart.position;
        attachedLocally = true;
        EndOffset.Value=endOffset;
        Connected.Value=true;
        attached.Add(this);
        line.enabled = true;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (!IsSpawned) return;
        if (!IsServer)
        {
            line.enabled=Connected.Value;
            if (!Connected.Value || CartStations.Instance==null) return;
            line.SetPosition(0,transform.position);
            line.SetPosition(1,CartStations.Instance.CartBody.transform.position+EndOffset.Value);
            return;
        }
        if (!Attached || !CartStations.Running) return;
        transform.position = cart.position + bodyOffset;
        line.SetPosition(0, transform.position);
        line.SetPosition(1, cart.position + endOffset);
    }

    public void Detach()
    {
        attached.Remove(this);
        attachedLocally = false;
        Connected.Value = false;
        line.enabled = false;
    }

    private void OnDisable()
    {
        attached.Remove(this);
        attachedLocally = false;
    }
}
