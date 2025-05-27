using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class Tester1 : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.isPressed)
        {
            if (NetworkAudio.Instance != null)
            {
                Debug.Log("[TESTER1]" + gameObject.name + " activated, triggering audio play.");
                NetworkAudio.Instance.TriggerPlay3D("wpn_762_single", gameObject);
                Debug.Log("Pressed P key to trigger audio.");
            }
        }
    }
}
