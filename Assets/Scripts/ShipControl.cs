using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ShipControl : NetworkBehaviour
{
	private NetworkObjectPool objectPool;

	private ShipType shipType = ShipType.Basic;
	private float rotateSpeed;
	private float acceleration;
	private float shootInterval;
	private float initialHealth;

	private float spin;
	private float oldMoveForce = 0;
	private float oldSpin = 0;
	private Rigidbody2D rb2D;
	private readonly NetworkVariable<float> Thrusting = new NetworkVariable<float>();

	private int bulletDamage;
	private float bulletSize;
	private bool canShoot;

	[SerializeField] private GameObject gameOverScreenPrefab;
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
	private bool destroyed;
	private bool gameOver;

	//camera
	private const float MAX_X = 26.4f;
	private const float MAX_Y = 7f;
	private const float MIN_X = 2.2f;
	private const float MIN_Y = -4f;

	private void Awake()
	{
		objectPool = GameObject.FindWithTag("ObjectPool").GetComponent<NetworkObjectPool>();
		rb2D = GetComponent<Rigidbody2D>();
	}

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			PlayerName.Value = $"Player {OwnerClientId}";
			ShootCooldown.Value = NetworkManager.ServerTime.TimeAsFloat + shootInterval;
		}

		if (IsClient)
		{
			switch (shipType)
			{
				case ShipType.Basic:
				default:
					rotateSpeed = 2f;
					acceleration = 80f;
					bulletDamage = 5;
					bulletSize = 1f;
					shootInterval = 2f;
					initialHealth = 40;
					GetComponentInChildren<SpriteRenderer>().sprite = healthySprite;
					break;
			}
		}
	}

	private void Update()
	{
		if (IsServer)
			UpdateServer();

		if (IsClient)
			UpdateClient();
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
		var targetVel = (Vector2)(transform.localRotation * Vector3.right * (float)(Thrusting.Value * deltaTime * acceleration));
		if (Thrusting.Value != 0)
			rb2D.AddForce(targetVel - curVel, ForceMode2D.Force);


		rb2D.AddTorque(spin * (float)(deltaTime * rotateSpeed));
	}

	void LateUpdate()
	{
		if (IsLocalPlayer)
		{
			Camera.main.transform.position = new Vector3(
			   Mathf.Clamp(transform.position.x, MIN_X, MAX_X),
			   Mathf.Clamp(transform.position.y, MIN_Y, MAX_Y),
			   -50
			);
		}
	}

	private void UpdateServer()
	{
		if (ShootCooldown.Value < NetworkManager.ServerTime.TimeAsFloat)
		{
			canShoot = true;
		}
	}

	private void UpdateClient()
	{
		if (!IsLocalPlayer)
			return;

		if (Input.GetKeyDown(KeyCode.G))
			destroyed = true;

		if (destroyed && !gameOver)
		{
			ShowGameOverScreen();
			return;
		}

		// movement
		int spin = -(int)Mathf.Round(Input.GetAxisRaw("Horizontal"));
		int moveForce = (int)Mathf.Round(Input.GetAxisRaw("Vertical"));

		if (oldMoveForce != moveForce || oldSpin != spin)
		{
			ThrustServerRpc(moveForce, spin);
			oldMoveForce = moveForce;
			oldSpin = spin;
		}

		// fire
		if (Input.GetKeyDown(KeyCode.Space))
		{
			FireServerRpc();
		}
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

		if (Health.Value <= 0 && IsServer && !destroyed)
		{
			Health.Value = 0;

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

	private void ShowGameOverScreen()
	{
		var gameOverScreen = objectPool.GetNetworkObject(gameOverScreenPrefab).gameObject;
		gameOverScreen.GetComponent<GameOverScreen>().SetPlayer(this);
		gameOverScreen.GetComponent<NetworkObject>().Spawn(true);
		gameOver = true;
	}

	public void Respawn()
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
		gameOver = false;
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
		if (NetworkManager.Singleton.IsServer == false)
		{
			return;
		}

		if (other.gameObject.CompareTag("Wall") || other.gameObject.CompareTag("Obstacle"))
		{
			TakeDamage(5);
		}
	}

	public void ChangeShipType(ShipType type) => shipType = type;

	#region Server Rpc

	[ServerRpc]
	public void ThrustServerRpc(float thrust, int spin)
	{
		Thrusting.Value = Mathf.Clamp(thrust, -.3f, 1);
		this.spin = spin;
	}

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

public enum ShipType
{
	Basic = 1,
}
