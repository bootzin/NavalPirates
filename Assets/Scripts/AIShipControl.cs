using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class AIShipControl : NetworkBehaviour
{
	private NetworkObjectPool objectPool;

	private ShipType shipType = ShipType.Basic;
	private float rotateSpeed;
	private float acceleration;
	private float shootInterval;
	private int initialHealth;
	private float range;
	private Rigidbody2D rb2D;

	private int bulletDamage;
	private float bulletSize;
	private bool canShoot;

	[SerializeField] private GameObject bulletPrefab;
	[SerializeField] private GameObject deadShipPrefab;
	[SerializeField] private AudioSource fireSound;
	[SerializeField] private AudioSource hitSound;
	[SerializeField] private Sprite healthySprite;
	[SerializeField] private Sprite damageSprite;
	[SerializeField] private Sprite heavyDamageSprite;

	public readonly NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
	public NetworkVariable<float> Health = new NetworkVariable<float>();
	public readonly NetworkVariable<float> ShootCooldown = new NetworkVariable<float>();
	public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes(""));

	// gui
	[SerializeField] Texture m_Box;
	[SerializeField] Vector2 m_NameLabelOffset;
	[SerializeField] Vector2 m_ResourceBarsOffset;
	private Vector3? targetPosition = null;
	private bool reverse;
	private bool destroyed;

	private void Awake()
	{
		objectPool = GameObject.FindWithTag("ObjectPool").GetComponent<NetworkObjectPool>();
		rb2D = GetComponent<Rigidbody2D>();
	}

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			PlayerName.Value = $"AI {AIManager.Instance.AICount()}";
			ShootCooldown.Value = NetworkManager.ServerTime.TimeAsFloat + shootInterval;

			switch (shipType)
			{
				case ShipType.Basic:
				default:
					rotateSpeed = 2.2f;
					acceleration = 80f;
					bulletDamage = 5;
					bulletSize = 1f;
					shootInterval = 2f;
					initialHealth = 40;
					range = 50f;
					break;
			}

			AIManager.Instance.RegisterAI(this);
		}
	}

	private void Update()
	{
		if (IsServer)
			UpdateServer();
	}

	private void FixedUpdate()
	{
		if (IsServer)
			UpdateServerFixed();
	}

	private void UpdateServerFixed()
	{
		var deltaTime = NetworkManager.ServerTime.FixedDeltaTime;
		var curVel = rb2D.velocity;

		if (targetPosition == null)
		{
			var target = AIManager.Instance.FirstInRange(transform.position, range);
			if (target != null)
				targetPosition = target;
			else
				targetPosition = transform.position + (Vector3)Random.insideUnitCircle * Random.Range(1, 5f);
		}

		var targetVel = (Vector2)(transform.localRotation * Vector3.right * (float)((reverse ? -.3f : 1) * deltaTime * acceleration));
		if (Vector3.Distance(transform.position, targetPosition.Value) > 2)
		{
			rb2D.AddForce(targetVel - curVel);
			var s = Vector3.SignedAngle(transform.position, targetPosition.Value, new Vector3(0, 0, 1));
			if (Mathf.Abs(s) > 10)
				rb2D.AddTorque(Mathf.Sign(s) * (float)(deltaTime * rotateSpeed));
		}
		else
		{
			targetPosition = null;
			rb2D.angularVelocity = Mathf.Lerp(rb2D.angularDrag, 0, 1);
			reverse = false;
		}
	}

	private void UpdateServer()
	{
		if (ShootCooldown.Value < NetworkManager.ServerTime.TimeAsFloat)
		{
			canShoot = true;
		}

		if (canShoot)
		{
			var hits = Physics2D.RaycastAll(transform.position, transform.right, range);
			foreach (var hit in hits)
			{
				if (hit.collider.TryGetComponent<ShipControl>(out _))
				{
					Fire(transform.right);

					canShoot = false;
					ShootCooldown.Value = NetworkManager.ServerTime.TimeAsFloat + shootInterval;
				}
			}
		}

		if (destroyed)
			Respawn();
	}

	private void Fire(Vector3 direction)
	{
		fireSound.Play();

		GameObject bullet = objectPool.GetNetworkObject(bulletPrefab).gameObject;
		bullet.transform.position = transform.position + direction;

		var bulletRb = bullet.GetComponent<Rigidbody2D>();
		var velocity = rb2D.velocity;
		velocity += (Vector2)(direction) * 10;
		bulletRb.velocity = velocity;
		bullet.GetComponent<Bullet>().Setup(this, bulletDamage, bulletSize);
		bullet.GetComponent<NetworkObject>().Spawn(true);
	}

	public void TakeDamage(int amount, bool playSound = false)
	{
		Health.Value -= amount;

		if (playSound)
			hitSound.Play();

		if (Health.Value < initialHealth * 2 / 3)
			GetComponentInChildren<SpriteRenderer>().sprite = damageSprite;

		if (Health.Value < initialHealth / 3)
			GetComponentInChildren<SpriteRenderer>().sprite = heavyDamageSprite;

		if (Health.Value <= 0 && IsServer && NetworkObject.IsSpawned)
		{
			var deadShip = objectPool.GetNetworkObject(deadShipPrefab);
			deadShip.transform.position = transform.position;
			deadShip.transform.rotation = transform.rotation;

			var deadShipRb = deadShip.GetComponent<Rigidbody2D>();
			deadShipRb.velocity = rb2D.velocity;
			deadShipRb.angularVelocity = rb2D.angularVelocity;

			deadShip.Spawn(true);

			destroyed = true;
		}
	}

	private void Respawn()
	{
		Health.Value = initialHealth;
		transform.position = SelectRandomSpawn() + ((Vector3)Random.insideUnitCircle * 2f);
		var rot = transform.rotation;
		var eul = rot.eulerAngles;
		eul.z = Random.Range(0, 360);
		rot.eulerAngles = eul;
		transform.rotation = rot;
		GetComponent<Rigidbody2D>().velocity = Vector3.zero;
		GetComponent<Rigidbody2D>().angularVelocity = 0;
		GetComponentInChildren<SpriteRenderer>().sprite = healthySprite;
		destroyed = false;
	}

	private Vector3 SelectRandomSpawn()
	{
		var spawnPoints = GameObject.Find("SpawnPoints");
		var spawns = spawnPoints.GetComponentsInChildren<Transform>();
		var index = Random.Range(0, spawns.Length);
		return spawns[index].position;
	}

	void OnCollisionEnter2D(Collision2D other)
	{
		if (other.gameObject.CompareTag("Wall") || other.gameObject.CompareTag("Obstacle"))
		{
			targetPosition = transform.position - 3 * transform.right;
			reverse = true;
		}
	}

	public void ChangeShipType(ShipType type) => shipType = type;

	public override void OnNetworkDespawn()
	{
		AIManager.Instance.RemoveAI(this);
	}

	#region Server Rpc

	[ServerRpc]
	public void FireServerRpc()
	{
		if (canShoot)
		{
			Fire(transform.right);

			canShoot = false;
			ShootCooldown.Value = NetworkManager.ServerTime.TimeAsFloat + shootInterval;
		}
	}

	[ServerRpc]
	public void SetNameServerRpc(string name)
	{
		PlayerName.Value = name;
	}

	#endregion // Server Rpc

	void OnGUI()
	{

		Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);

		// draw the name with a shadow (colored for buf)	
		GUI.color = Color.black;
		GUI.Label(new Rect((pos.x + m_NameLabelOffset.x) - 20, Screen.height - (pos.y + m_NameLabelOffset.y) - 30, 400, 30), PlayerName.Value.Value);

		GUI.color = Color.white;

		GUI.Label(new Rect((pos.x + m_NameLabelOffset.x) - 21, Screen.height - (pos.y + m_NameLabelOffset.y) - 31, 400, 30), PlayerName.Value.Value);

		// draw health bar background
		GUI.color = Color.grey;
		GUI.DrawTexture(new Rect((pos.x + m_ResourceBarsOffset.x) - 26, Screen.height - (pos.y + m_ResourceBarsOffset.y) + 20, 52, 7), m_Box);

		// draw health bar amount
		GUI.color = Color.green;
		GUI.DrawTexture(new Rect((pos.x + m_ResourceBarsOffset.x) - 25, Screen.height - (pos.y + m_ResourceBarsOffset.y) + 21, Health.Value / 4 * 5, 5), m_Box);

		// draw shoot cooldown bar background
		GUI.color = Color.grey;
		GUI.DrawTexture(new Rect((pos.x + m_ResourceBarsOffset.x) - 26, Screen.height - (pos.y + m_ResourceBarsOffset.y) + 27, 52, 7), m_Box);

		// draw shoot cooldown bar amount
		GUI.color = Color.magenta;
		GUI.DrawTexture(new Rect((pos.x + m_ResourceBarsOffset.x) - 25, Screen.height - (pos.y + m_ResourceBarsOffset.y) + 28, Mathf.Min((1 - ((ShootCooldown.Value - NetworkManager.ServerTime.TimeAsFloat) / shootInterval)) * 50, 50), 5), m_Box);
	}
}
