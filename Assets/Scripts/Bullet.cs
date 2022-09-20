using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
	private int damage;
	private ShipControl owner;
	private AIShipControl AIOwner;
	private NetworkObjectPool objectPool;
	private readonly float lifetime = 2;

	[SerializeField] private GameObject explosionParticle;

	private void Awake()
	{
		objectPool = GameObject.FindWithTag("ObjectPool").GetComponent<NetworkObjectPool>();
	}

	public void Setup(ShipControl owner, int damage = 5, float size = 1)
	{
		this.owner = owner;
		SetupInternal(damage, size);
	}

	private void SetupInternal(int damage, float size)
	{
		this.damage = damage;

		GetComponent<CircleCollider2D>().radius *= size;
		transform.localScale *= size;

		StartCoroutine(WaitAndDestroy(lifetime));
	}

	public void Setup(AIShipControl owner, int damage = 5, float size = 1)
	{
		AIOwner = owner;
		SetupInternal(damage, size);
	}

	public override void OnNetworkDespawn()
	{
		if (IsOwner)
			SpawnExplosionServerRpc();
	}

	[ServerRpc]
	private void SpawnExplosionServerRpc()
	{
		GameObject explosion = objectPool.GetNetworkObject(explosionParticle).gameObject;
		explosion.transform.position = transform.position;
		explosion.GetComponent<NetworkObject>().Spawn(true);
		explosion.GetComponent<Animator>().Play("Explosion");
	}

	private void DestroyBullet()
	{
		if (IsSpawned)
			NetworkObject.Despawn(true);
	}

	private IEnumerator WaitAndDestroy(float waitTime)
	{
		yield return new WaitForSeconds(waitTime);
		DestroyBullet();
	}

	void OnCollisionEnter2D(Collision2D other)
	{
		var otherObject = other.gameObject;

		if (otherObject.CompareTag("Wall") || otherObject.CompareTag("Obstacle"))
		{
			DestroyBullet();
		}

		if (otherObject.TryGetComponent<AIShipControl>(out var aiShipControl))
		{
			if (aiShipControl != AIOwner)
			{
				aiShipControl.TakeDamage(damage, true);
				DestroyBullet();
				if (owner != null && aiShipControl.Health.Value <= 0)
					owner.Score += 1;
			}
		}

		if (otherObject.TryGetComponent<ShipControl>(out var shipControl))
		{
			if (shipControl != owner)
			{
				shipControl.TakeDamage(damage, true);
				DestroyBullet();
				if (owner != null && shipControl.Health.Value <= 0)
					owner.Score += 1;
			}
		}
	}
}
