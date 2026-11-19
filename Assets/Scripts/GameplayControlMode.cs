using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public static class GameplayControlMode
{
#if UNITY_EDITOR
    public static bool IsMobile { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => IsMobile = false;

    internal static void UpdateEditorMode()
    {
        if (Keyboard.current?.f6Key.wasPressedThisFrame == true)
            IsMobile = !IsMobile;
    }
#elif UNITY_ANDROID || UNITY_IOS
    public static bool IsMobile => true;
#else
    public static bool IsMobile => false;
#endif
}
