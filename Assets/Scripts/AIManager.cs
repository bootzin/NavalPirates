using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AIManager : MonoBehaviour
{
	public static AIManager Instance;

	private readonly Dictionary<ulong, ShipControl> playerShips = new Dictionary<ulong, ShipControl>();
	private readonly List<AIShipControl> aiShips = new List<AIShipControl>();

	private void Awake()
	{
		Instance = this;
	}

	public void Init()
	{
		NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
		{
			playerShips.Add(id, NetworkManager.Singleton.ConnectedClients[id].PlayerObject.GetComponent<ShipControl>());
		};
		NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
		{
			playerShips.Remove(id);
		};
	}

	public int AICount() => aiShips.Count;

	public void RegisterAI(AIShipControl ai)
	{
		aiShips.Add(ai);
	}

	public void RemoveAI(AIShipControl ai)
	{
		aiShips.Remove(ai);
	}

	public Vector3? FirstInRange(Vector3 position, float range)
	{
		if (Random.value > .5f)
		{
			foreach (var ai in aiShips)
			{
				if (Vector3.Distance(ai.transform.position, position) < range)
					return ai.transform.position;
			}

			foreach (var player in playerShips.Values)
			{
				if (Vector3.Distance(player.transform.position, position) < range)
					return player.transform.position;
			}
		}
		else
		{
			foreach (var player in playerShips.Values)
			{
				if (Vector3.Distance(player.transform.position, position) < range)
					return player.transform.position;
			}

			foreach (var ai in aiShips)
			{
				if (Vector3.Distance(ai.transform.position, position) < range)
					return ai.transform.position;
			}
		}

		return null;
	}
}
