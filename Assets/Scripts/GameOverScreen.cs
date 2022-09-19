using Unity.Netcode;
using UnityEngine;

public class GameOverScreen : NetworkBehaviour
{
	private ShipControl playerToRespawn;

	public void Quit()
	{
		Application.Quit();
	}

	public override void OnNetworkSpawn()
	{
		if (IsClient)
			transform.SetParent(FindObjectOfType<Canvas>().transform, false);
	}

	public void SetPlayer(ShipControl player)
	{
		gameObject.SetActive(true);
		playerToRespawn = player;
	}

	public void RespawnPlayer()
	{
		if (IsServer)
		{
			gameObject.SetActive(false);
			playerToRespawn.Respawn();
		}
	}
}
