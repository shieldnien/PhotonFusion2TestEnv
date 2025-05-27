using Fusion;
using UnityEngine;

public class NetworkAudio : NetworkBehaviour

// Se triggerea que se debe emitir un sonido 
{
    public static NetworkAudio Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        //DontDestroyOnLoad(gameObject);
    }

    public void TriggerPlay(string clip)
    {
        Debug.Log("Triggering play for clip: " + clip);
        if (HasInputAuthority)
        {
            Debug.Log("TriggerPlay: " + clip + " on object: " + gameObject.name);
            RPC_AskServerPlay(clip);
        }
    }

    public void TriggerPlay3D(string clip, GameObject go) {
        Debug.Log(go.name + " has input authority, triggering play 3D for clip: " + clip);
        if (!HasInputAuthority) {
            Debug.Log("NO TIENEE!!");
            return;
        } 
            if (go.TryGetComponent<NetworkObject>(out var netObject))
            {
                Debug.Log("TriggerPlay3D: " + clip + " on object: " + netObject.gameObject.name);
                RPC_AskServerPlay3D(clip, netObject.Id);
            }
    }

    public void TriggerPlayAtPoint(string clip, GameObject go) {
        Debug.Log(go.name + " has input authority, triggering play at point for clip: " + clip);
        if (HasInputAuthority)
        {
            if (go.TryGetComponent<NetworkObject>(out var netObject))
            {
                Debug.Log("TriggerPlayAtPoint: " + clip + " on object: " + netObject.gameObject.name);
                RPC_AskServerPlayAtPoint(clip, netObject.Id);
            }
        }
    }

    // Peticiones: Cliente -> Server

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_AskServerPlay(string clip, RpcInfo info = default)
    {
        Debug.Log("RPC_AskServerPlay called for clip: " + clip);
        RpcPlay(clip);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_AskServerPlay3D(string clip, NetworkId netId, RpcInfo info = default)
    {
        Debug.Log("RPC_AskServerPlay3D called for clip: " + clip + " with netId: " + netId);
        RpcPlay3D(clip, netId);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_AskServerPlayAtPoint(string clip, NetworkId netId, RpcInfo info = default)
    {
        Debug.Log("RPC_AskServerPlayAtPoint called for clip: " + clip + " with netId: " + netId);
        RpcPlayAtPoint(clip, netId);
    }

    // Dispara RPC: Server -> Todos los client
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlay(string clip)
    {
        Debug.Log("RpcPlay called for clip: " + clip);
        AudioManager.Instance.Play(clip);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlay3D(string clip, NetworkId netId)
    {
        NetworkObject netObject = Runner.FindObject(netId);
        Debug.Log("RpcPlay3D called for clip: " + clip + " with netId: " + netId + " on object: " + netObject.gameObject.name);
        AudioManager.Instance.Play3D(clip, netObject.gameObject);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayAtPoint(string clip, NetworkId netId)
    {
        NetworkObject netObject = Runner.FindObject(netId);
        Debug.Log("RpcPlayAtPoint called for clip: " + clip + " with netId: " + netId + " on object: " + netObject.gameObject.name);
        Vector3 position = netObject.gameObject.transform.position;
        AudioManager.Instance.PlayAtPoint(clip, position);
    }
}
