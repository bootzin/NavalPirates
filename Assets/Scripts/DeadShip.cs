using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DeadShip : NetworkBehaviour
{
	private readonly float lifetime = 20f;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
			StartCoroutine(WaitAndDestroy());
	}

	public IEnumerator WaitAndDestroy()
	{
		yield return new WaitForSeconds(lifetime);
		NetworkObject.Despawn(true);
	}
}
