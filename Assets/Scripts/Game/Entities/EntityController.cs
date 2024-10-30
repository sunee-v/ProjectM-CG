using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
/// <summary>
/// @alex-memo 2024
/// </summary>
public class EntityController : NetworkBehaviour, IDamageable
{
	public NetworkVariable<UserData> UserData = new();
	public NetworkVariable<float> CurrentHealth { get; protected set; } = new(2000);
	public NetworkVariable<float> MaxHealth { get; protected set; } = new(2000);
	public NetworkVariable<float> Shield { get; protected set; } = new(0);
	protected DamageGraph damageGraph = new();
	protected List<DamageGraph> damageGraphs = new();
	public NetworkVariable<bool> IsDead { get; protected set; } = new(false);
	public WorldHpBarScript HealthBar { get; private set; }
	public override async void OnNetworkSpawn()
	{
		if (!IsLocalPlayer)
		{
			instantiateHealthBar();
		}
		//StartCoroutine(setInitialStats());
		UserData.OnValueChanged += OnUserDataChanged;
		if (IsServer)
		{
			await UniTask.WaitUntil(() => MultiplayerGameManager.Instance != null);
			MultiplayerGameManager.Instance.AddEntity(this);
			//SetMaxHealthServerRpc(EntityStats.GetMaxHealth);
			//SetCurrentHealthServerRpc(EntityStats.GetMaxHealth);
		}
	}
	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		MaxHealth.OnValueChanged -= onMaxHealthChanged;
		UserData.OnValueChanged -= OnUserDataChanged;
		if (IsServer)
		{
			MultiplayerGameManager.Instance.RemoveEntity(this);
		}
	}
	protected virtual IEnumerator setInitialStats()
	{
		yield return new WaitForSecondsRealtime(1);//wait for ticks to sync
		MaxHealth.OnValueChanged += onMaxHealthChanged;
	}
	protected virtual void instantiateHealthBar()
	{
		//if(!IsLocalPlayer){return;}
		HealthBar = Instantiate(Resources.Load<GameObject>("HealthBar"), transform).GetComponent<WorldHpBarScript>();

		MaxHealth.OnValueChanged += HealthBar.SetMaxHealth;
		CurrentHealth.OnValueChanged += HealthBar.SetHealth;
		Shield.OnValueChanged += HealthBar.SetShield;
		UserData.OnValueChanged += HealthBar.SetHealthBarColour;

		HealthBar.SetMaxHealth(0, MaxHealth.Value);
		HealthBar.SetHealth(0, CurrentHealth.Value);
		//print($"HealthBar instantiated for {name}");
	}
	/// <summary>
	/// should be triggered in server and just replicate in clients, so abilities and autos that
	/// hit player in server just replicate in clients
	/// melee users just trigger this method so those are not in server require ownership false
	/// </summary>
	/// <param name="_damage"></param>
	/// <param name="_element"></param>
	[ServerRpc(RequireOwnership = false)]
	public void TakeDamageServerRpc(float _damage, Element _element, UserData _dmgDealerData, DamageSource _damageSource)
	{
		takeDamage(_damage, _element, _dmgDealerData, _damageSource);
	}
	public void ServerTakeDamage(float _damage, Element _element, UserData _dmgDealerData, DamageSource _damageSource)
	{
		takeDamage(_damage, _element, _dmgDealerData, _damageSource);
	}
	protected void takeDamage(float _damage, Element _element, UserData _dmgDealerData, DamageSource _damageSource)
	{
		if (IsDead.Value) { return; }
		/**damage formula states as follows
			post mitigation dmg = raw dmg * (100/(100+armour))
			if less than 0 = raw dmg * 2-(100/(100-armour))**/

		//EntityStats.CustomTakeDamageModifiers(ref _damage, ref _damageType, ref _damageDealerStats);
		if (_damage < 0) { _damage = 0; }
		if (Shield.Value > 0)
		{
			Shield.Value -= (int)(_damage + .5f);
			if (Shield.Value < 0)
			{
				_damage = Shield.Value * -1;
				Shield.Value = 0;
			}
		}

		if (_damage <= 0) { return; }
		if (CurrentHealth.Value - _damage <= 0) { _damage = CurrentHealth.Value; }
		if (_damage <= 0) { return; }

		CurrentHealth.Value -= _damage;
		//print(_damage + " " + _damageType + " " + _damageDealerId + " " + CurrentHealth.Value);
		damageGraph.AddDamage(_damage, _element, _dmgDealerData);

		if (CurrentHealth.Value <= 0)
		{
			CurrentHealth.Value = 0;
			secureKill(ref _dmgDealerData);
			Die(_dmgDealerData);
			//we retrieve dmg graph and reset it
		}
		if (_dmgDealerData.ChosenToolID >= -500)
		{
			addIonization(_dmgDealerData.ClientID, _damage);
		}
		InstantiateDamageCanvasClientRpc(_damage, _element);
	}
	protected virtual void addIonization(ulong _clientId, float _damage) { }
	/// <summary>
	/// if the entity dies from external source from player,
	/// if they took damage in the last 15 seconds
	/// then we replace the damage dealer for the last damage taken
	/// </summary>
	private void secureKill(ref UserData _dmgDealerData)
	{
		if (_dmgDealerData.ChosenToolID > -500) { return; }
		if (Time.time - damageGraph.lastDamageTakenTime > 15) { return; }
		if (damageGraph.lastDamageDealer == null) { return; }
		_dmgDealerData = damageGraph.lastDamageDealer;
	}
	/// <summary>
	/// Die Called from server
	/// </summary>
	/// <param name="_assasinData"></param>
	protected virtual void Die(UserData _assasinData)
	{
		if (!IsServer) { return; }//here we should still be in server all the time but still just in case
		print(damageGraph.ToString());
		damageGraphs.Add(damageGraph);
		damageGraph = new();
		IsDead.Value = true;

		DieClientRpc();
		//if (IsOwnedByServer && !IsLocalPlayer) { NetworkObject.Despawn(); }
		//enable the line above if we want to despawn the entity when killed
	}
	/// <summary>
	/// Runs on the killed client
	/// </summary>
	/// <param name="_params"></param>

	[ClientRpc]
	protected void DieClientRpc()
	{
		ClientDie();
	}
	protected virtual void ClientDie()
	{
		//runs on every client so visuals are synched
		//we should play death animation and disable the entity
		Debug.Log("Die ClientRpc");
		if (IsLocalPlayer)
		{//things to do when player dies if its the local player
		 //if anim is client based then here death anim
		}
		if (TryGetComponent<Collider>(out var _collider))
		{
			_collider.enabled = false;
		}
		gameObject.layer = LayerMask.NameToLayer("Dead");
		foreach (Transform _child in transform)
		{
			if (_child.name.Contains("DamageCanvas")) { continue; }
			_child.gameObject.SetActive(false);
		}
		//play death sound here
	}
	public List<DamageGraph> ExtractDamageGraphs()
	{
		if (!IsServer) { return null; }
		var _tempGraphs = new List<DamageGraph>(damageGraphs)
		{
			damageGraph
		};
		return _tempGraphs;
	}
	public virtual void ServerRespawn()
	{
		IsDead.Value = false;
		HealToMaxHP();
		RespawnVisualsClientRpc();
	}
	[ServerRpc]
	public void RespawnServerRpc()
	{
		IsDead.Value = false;
		HealToMaxHP();
		RespawnVisualsClientRpc();
	}
	[ClientRpc]
	protected virtual void RespawnVisualsClientRpc()
	{
		//runs on every client so visuals are synched
		if (TryGetComponent<Collider>(out var _collider))
		{
			_collider.enabled = true;
		}
		//GetComponent<Collider>().enabled = true;
		gameObject.layer = LayerMask.NameToLayer("Entity");
		foreach (Transform _child in transform)
		{
			_child.gameObject.SetActive(true);
		}
	}

	[ClientRpc]
	private void InstantiateDamageCanvasClientRpc(float _damage, Element _damageType)
	{
		if (IsLocalPlayer) { return; }
		DamageCanvas _damageCanvas = GetComponentInChildren<DamageCanvas>();
		if (_damageCanvas != null)
		{
			_damageCanvas.AddDamage(_damage, _damageType);
			return;
		}
		GameObject _canvas = Instantiate(Resources.Load<GameObject>("DamageCanvas"), transform);

		_canvas.GetComponent<DamageCanvas>().SetDamage(_damage, _damageType);
	}

	#region Health and Shield Utilities Region
	private void onMaxHealthChanged(float _oldValue, float _newValue)
	{
		if (!IsOwner) { return; }
		var _addedHealth = _newValue - _oldValue;
		Heal(_addedHealth);
	}
	public void HealToMaxHP()
	{
		SetCurrentHealthServerRpc(MaxHealth.Value);
	}
	public void Heal(float _hp)
	{
		float _newHealth = CurrentHealth.Value + _hp;
		SetCurrentHealthServerRpc(_newHealth);
	}
	public void HealOrShield(float _hp)
	{
		if (CurrentHealth.Value < MaxHealth.Value)
		{
			var _healthOverflow = CurrentHealth.Value + _hp - MaxHealth.Value;
			if (_healthOverflow > 0)
			{
				Heal(MaxHealth.Value - CurrentHealth.Value);
				addShieldServer(_healthOverflow).Forget();
				return;
			}
			Heal(_hp);
			return;
		}
		addShieldServer(_hp).Forget();
	}

	[ServerRpc(RequireOwnership = false)]
	protected void SetCurrentHealthServerRpc(float _health)
	{
		var _newHealth = Mathf.Clamp(_health, 0, MaxHealth.Value);
		//print(_newHealth);
		CurrentHealth.Value = _newHealth;
	}
	[ServerRpc]
	protected void SetMaxHealthServerRpc(float _maxHealth)
	{
		MaxHealth.Value = _maxHealth;
	}
	public void AddShield(float _shield)
	{
		if (!IsLocalPlayer) { return; }
		AddShieldServerRpc(_shield);
	}
	[ServerRpc]
	private void AddShieldServerRpc(float _shield)
	{
		addShieldServer(_shield).Forget();
	}
	private async UniTaskVoid addShieldServer(float _shield)
	{
		if (!IsServer) { return; }
		Shield.Value += _shield;
		//all shields decay over 3 seconds
		const float _decayTime = 3;
		const float _tickInterval = .15f;
		float _tickDecayValue = _shield * _tickInterval / _decayTime;
		float _timer = 0;
		while (_timer < _decayTime)
		{
			await UniTask.WaitForSeconds(_tickInterval);
			if (Shield.Value <= 0) { break; }
			Shield.Value -= _tickDecayValue;

			_timer += _tickInterval;
		}
		if (Shield.Value < 0)
		{
			Shield.Value = 0;
		}
	}

	#endregion
	public string GetEnemyTeam()
	{
		if (UserData.Value == null) { return "Enemy"; }
		var _team = UserData.Value.Team.ToString();//  TeamNumber.Value.ToString();

		return _team switch
		{
			"TeamA" => "TeamB",
			"TeamB" => "TeamA",
			_ => "Enemy",
		};
	}
	private void OnUserDataChanged(UserData _previousValue, UserData _newValue)
	{
		//print(name + "team number changed from " + previousValue + " to " + newValue);
		tag = _newValue.Team.ToString();
	}
	[Rpc(SendTo.Server)]
	public void SpawnProjectileServerRpc(bool _isAbility, int _id, Vector3 _spawnPos, Quaternion _rotation, RpcParams _params = default)
	{
		SpawnProjectileLocal(_isAbility, _id, _spawnPos, _rotation);

		spawnProjectileClientRpc(_isAbility, _id, _spawnPos, _rotation, RpcTarget.Not(
			new ulong[]
			{
				_params.Receive.SenderClientId, //we dont want to send to the client that sent the rpc
				NetworkManager.Singleton.LocalClientId //we dont want to send to the server cuz alr happened here(server)
			},
			RpcTargetUse.Temp));
	}
	[Rpc(SendTo.SpecifiedInParams)]
	private void spawnProjectileClientRpc(bool _isAbility, int _id, Vector3 _spawnPos, Quaternion _rotation, RpcParams _params = default)
	{
		SpawnProjectileLocal(_isAbility, _id, _spawnPos, _rotation);
	}
	public void SpawnProjectileLocal(bool _isAbility, int _id, Vector3 _spawnPos, Quaternion _rotation)
	{
		GameObject _obj = null;
		switch (_isAbility)
		{
			case true:
				ProjectileAbility _ability = StaticListManager.Instance.GetAbility(_id) as ProjectileAbility;
				_obj = _ability.ProjectilePrefab;
				if (_obj == null) { return; }
				var _abilityInstance = Instantiate(_obj, _spawnPos, _rotation, null);
				//play cast sfx
				_abilityInstance.GetComponent<NetworkAbility>().SetAbility(NetworkObjectId, _ability, _id);

				break;
			case false:
				//_obj = StaticListManager.Instance.GetToolScript(_id).GetPrefab();
				break;
		}

		//if (_obj == null) { return; }
		//var _abilityInstance = Instantiate(_obj, _spawnPos, _rotation, null);
		//play cast sfx
		//_abilityInstance.GetComponent<NetworkAbility>().SetAbility(NetworkObjectId, _abi);
	}
}
