using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI score;
	private ShipControl playerToRespawn;

	public void Quit()
	{
		Application.Quit();
	}

	private void Start()
	{
		transform.SetParent(GameObject.Find("Canvas").transform, false);
		score.text = "Score: " + playerToRespawn.Score.Value;
	}

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
