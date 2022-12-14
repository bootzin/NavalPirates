using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
	[SerializeField] private GameObject startMenu;
	[SerializeField] private GameObject multiplayerMenu;
	[SerializeField] private GameObject connectionMenu;
	[SerializeField] private GameObject optionsMenu;
	[SerializeField] private TextMeshProUGUI joinButtonText;
	[SerializeField] private NetworkObjectPool objectPool;
	[SerializeField] private GameObject aiPrefab;
	[SerializeField] private TMP_InputField ipToConnect;
	[SerializeField] private TMP_InputField portToConnect;
	[SerializeField] private GameObject spawnPoints;

	private string activeMenu = "Start";
	private bool hosting;
	private const int NUMBER_ENEMIES = 4;

	#region Start Menu
	public void SinglePlayer()
	{
		DeactivateMenus();
		NetworkManager.Singleton.StartHost();
		AIManager.Instance.Init();
		objectPool.GetNetworkObject(aiPrefab).Spawn(true);

		for (int amount = 0; amount < NUMBER_ENEMIES; amount++)
		{
			var ai = objectPool.GetNetworkObject(aiPrefab);
			ai.transform.position = GetRandomSpawnPoint() + ((Vector3)Random.insideUnitCircle * 2f);
			var rot = ai.transform.rotation;
			var eul = rot.eulerAngles;
			eul.z = Random.Range(0, 360);
			rot.eulerAngles = eul;
			ai.transform.rotation = rot;
			ai.Spawn(true);
		}
	}

	private Vector3 GetRandomSpawnPoint()
	{
		var spawns = spawnPoints.GetComponentsInChildren<Transform>();
		var index = Random.Range(0, spawns.Length);
		return spawns[index].position;
	}

	public void Multiplayer()
	{
		DeactivateMenus();
		multiplayerMenu.SetActive(true);
		activeMenu = "Multiplayer";
	}

	public void Options()
	{
		DeactivateMenus();
		optionsMenu.SetActive(true);
		activeMenu = "Options";
	}

	public void Quit()
	{
		Application.Quit();
	}
	#endregion

	#region Multiplayer Menu
	public void Host()
	{
		DeactivateMenus();
		connectionMenu.SetActive(true);
		hosting = true;
		joinButtonText.text = "Host Game";
		activeMenu = "Connection";
	}

	public void Join()
	{
		DeactivateMenus();
		connectionMenu.SetActive(true);
		hosting = false;
		joinButtonText.text = "Join Game";
		activeMenu = "Connection";
	}
	#endregion

	#region Connection Menu
	public void JoinGame()
	{
		var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

		if (string.IsNullOrWhiteSpace(ipToConnect.text))
			ipToConnect.text = "127.0.0.1";

		if (ushort.TryParse(portToConnect.text, out ushort port))
		{
			transport.SetConnectionData(ipToConnect.text, port);
		}
		else
		{
			transport.SetConnectionData(ipToConnect.text, 7777);
		}

		if (!hosting)
			NetworkManager.Singleton.StartClient();
		else
			NetworkManager.Singleton.StartHost();

		DeactivateMenus();
	}
	#endregion

	public void Back()
	{
		DeactivateMenus();

		switch (activeMenu)
		{
			case "Connection":
				multiplayerMenu.SetActive(true);
				activeMenu = "Multiplayer";
				break;
			case "Options":
			case "Multiplayer":
			default:
				startMenu.SetActive(true);
				break;
		}
	}

	public void MuteUnmuteGame()
	{
		if (AudioListener.volume == 0)
			AudioListener.volume = 1;
		else
			AudioListener.volume = 0;
	}

	private void DeactivateMenus()
	{
		startMenu.SetActive(false);
		multiplayerMenu.SetActive(false);
		connectionMenu.SetActive(false);
		optionsMenu.SetActive(false);
	}
}
