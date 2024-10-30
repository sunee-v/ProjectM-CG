using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Game.Barrels
{
	public class Barrel : NetworkBehaviour, IDamageable
	{
		public NetworkVariable<float> CurrentHealth { get; protected set; } = new(0);
		[field: SerializeField] public NetworkVariable<float> MaxHealth { get; protected set; } = new(800);
		public NetworkVariable<float> Shield { get; protected set; } = new(0);
		[SerializeField] protected float explosionRadius = 5;
		[SerializeField] protected List<BarrelCommand> commands = new();
		protected BarrelCommand myCommand;
		protected int commandIndex = 0;
		private MeshRenderer meshRenderer;
		private bool hasExploded = false;
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
			if (hasExploded) { return; }
			if (_damage <= 0) { return; }
			if (CurrentHealth.Value - _damage <= 0) { _damage = CurrentHealth.Value; }
			if (_damage <= 0) { return; }
			CurrentHealth.Value -= _damage;
			if (CurrentHealth.Value <= 0)
			{
				CurrentHealth.Value = 0;
				Explode();
			}

			InstantiateDamageCanvasClientRpc(_damage, _element);
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
		protected virtual void Explode()
		{
			if (!IsServer) { return; }
			hasExploded = true;
			Debug.Log("Explode");
			Collider[] _colliders = new Collider[10];
			int _numColliders = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, _colliders);
			List<EntityController> _controllers = new();
			for (int i = 0; i < _numColliders; ++i)
			{
				if (_colliders[i] == null) { continue; }
				if (!_colliders[i].TryGetComponent(out EntityController _controller)) { continue; }
				_controllers.Add(_controller);
			}
			if (myCommand == null) { return; }
			myCommand.Execute(_controllers.ToArray());
			NetworkObject.Despawn();
		}

		public override void OnNetworkSpawn()
		{
			meshRenderer = GetComponent<MeshRenderer>();
			if (!IsServer)
			{
				getMyCommandRpc();
				return;
			}
			CurrentHealth.Value = MaxHealth.Value;
			if (commands.Count == 0)
			{
				Debug.LogWarning("No commands assigned to the barrel");
				return;
			}
			setToRandomBarrel();
		}
		private void setToRandomBarrel()
		{
			commandIndex = Random.Range(0, commands.Count);
			myCommand = commands[commandIndex];
			setMyCommandRpc(commandIndex, RpcTarget.Everyone);
			initLocal();
		}
		[Rpc(SendTo.Server)]
		private void getMyCommandRpc(RpcParams _rpcParams = default)
		{
			setMyCommandRpc(commandIndex, RpcTarget.Single(_rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
		}
		[Rpc(SendTo.SpecifiedInParams)]
		private void setMyCommandRpc(int _index, RpcParams _rpcParams)
		{
			myCommand = commands[_index];
			initLocal();
		}
		private void initLocal()
		{
			if (myCommand == null) { return; }
			if (meshRenderer == null) { return; }
			myCommand.Init(out var _mat);
			meshRenderer.materials[1] = _mat;
		}

		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, explosionRadius);
		}
	}
}