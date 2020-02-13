using UnityEngine;

namespace GOTest
{
    public class GOComponent : MonoBehaviour
    {
        float duration = 0f;
        void FixedUpdate()
        {
            duration += Time.fixedDeltaTime;
            if(duration > 1f)
            {
                duration = 0f;
                KLog.Log(KLogLevel.Debug, $"test success!");
            }
            
        }
    }
}