using UnityEngine;

namespace MLTest
{
    public class TestComponent : MonoBehaviour
    {
        private float duration;
        void FixedUpdate()
        {
            duration += Time.deltaTime;
            if (duration > 1f)
            {
                duration = 0f;
                KLog.Log(KLogLevel.Debug, "[MLTest] component running!");
            }
        }
    }
}