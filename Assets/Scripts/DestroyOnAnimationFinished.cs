using Unity.Netcode;

public class DestroyOnAnimationFinished : NetworkBehaviour
{
    public void DestroyOnAnimationEnd()
	{
		NetworkObject.Despawn(true);
	}
}
