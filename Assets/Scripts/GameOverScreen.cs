using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI score;
	private ShipControl playerToRespawn;
	private ulong playerClientId;

	public void Quit()
	{
		Application.Quit();
	}

	private void Start()
	{
		transform.SetParent(GameObject.Find("Canvas").transform, false);
		score.text = "Score: " + playerToRespawn.Score;
	}

	//public override void OnNetworkSpawn()
	//{
	//	if (IsClient)
	//	{
	//		transform.SetParent(GameObject.Find("Canvas").transform, false);
	//	}
	//}

	//private void Update()
	//{
		//if (IsSpawned && playerToRespawn != null)
		//	score.text = "Score: " + playerToRespawn.Score;
		//else if (IsSpawned && playerToRespawn == null && IsServer)
		//{
		//	playerToRespawn = NetworkManager.ConnectedClients[playerClientId].PlayerObject.gameObject.GetComponent<ShipControl>();
		//}
	//}

	public void SetPlayer(ShipControl player)
	{
		gameObject.SetActive(true);
		playerToRespawn = player;
	}

	public void RespawnPlayer()
	{
		gameObject.SetActive(false);
		playerToRespawn.Respawn();
	}
}
