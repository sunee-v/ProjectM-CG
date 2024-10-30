using Unity.Netcode;
using UnityEngine;

public interface IDamageable
{
	public NetworkVariable<float> CurrentHealth { get; }
	public NetworkVariable<float> MaxHealth { get; }
	public NetworkVariable<float> Shield { get; }
	
	[ServerRpc]
	public void TakeDamageServerRpc(float _damage, Element _element, UserData _dmgDealerData, DamageSource _damageSource);
	public void ServerTakeDamage(float _damage, Element _element, UserData _dmgDealerData, DamageSource _damageSource);
}