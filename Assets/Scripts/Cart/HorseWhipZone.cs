using UnityEngine;

[RequireComponent(typeof(SpriteRenderer)), DisallowMultipleComponent]
public sealed class HorseWhipZone : MonoBehaviour
{
    [Tooltip("말 스프라이트 안의 조향 판정 영역. 씬 뷰의 초록 사각형으로 편집합니다.")]
    [SerializeField] private Rect steeringArea = new Rect(.575f, 0f, .425f, 1f);
    [SerializeField] private SpriteRenderer gallopVisual;
    [SerializeField] private SpriteSequence gallop;
    [SerializeField] private SpriteSequence woundedGallop;
    [SerializeField] private float gallopOffset;
    private float phase;
    private float lastFrame;
    private SpriteRenderer horse;
    public Rect SteeringArea => steeringArea;

    public void ShowGallop(bool running, bool wounded, HorseMood mood, float time)
    {
        if (gallopVisual == null) return;
        var sequence = wounded ? woundedGallop : gallop;
        float rate = mood switch
        {
            HorseMood.Tired => .52f,
            HorseMood.Angry => 1.4f,
            HorseMood.Runaway => 2.05f,
            _ => 1f
        };
        if (running) phase += Mathf.Clamp(time - lastFrame, 0f, .1f) * rate;
        lastFrame = time;
        gallopVisual.sprite = sequence.FrameAt(phase + gallopOffset, true);
    }
    public Bounds SpriteBounds
    {
        get
        {
            if (horse == null) horse = GetComponent<SpriteRenderer>();
            return horse.localBounds;
        }
    }

    private void OnValidate()
    {
        steeringArea.width = Mathf.Clamp(steeringArea.width, .01f, 1f);
        steeringArea.height = Mathf.Clamp(steeringArea.height, .01f, 1f);
        steeringArea.x = Mathf.Clamp(steeringArea.x, 0f, 1f - steeringArea.width);
        steeringArea.y = Mathf.Clamp(steeringArea.y, 0f, 1f - steeringArea.height);
    }

    public bool SteersAt(Vector2 uv) => uv.x >= steeringArea.xMin && uv.x <= steeringArea.xMax
        && uv.y >= steeringArea.yMin && uv.y <= steeringArea.yMax;

    // Two adjacent input ranges map directly onto the lower and upper sprites, skipping the gap.
    public static Vector2 CursorUV(Vector2 aim) => new Vector2(
        Mathf.Clamp01((aim.x + 1f) * .5f), Mathf.Clamp01(aim.y < 0f ? aim.y + 1f : aim.y));

    public Vector3 LocalPoint(Vector2 uv)
    {
        var bounds = SpriteBounds;
        return new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, uv.x),
            Mathf.Lerp(bounds.min.y, bounds.max.y, uv.y), bounds.center.z);
    }
}
