using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace RangedTools
{
	public abstract class RangeTool : ToolAttack
	{
		[SerializeField, Min(1)] protected int maxAmmo = 1;
		[SerializeField] protected float reloadTime = 1f;

		[Header("Primary Fire")]
		[SerializeField, Min(0.001f), Tooltip("Defined as per Bullets per Second")] protected float fireRate = 5f;
		[SerializeField, Range(0, 1)] protected float accuracy = 0.5f;
		[SerializeField] protected DamageFallOff damageFallOff;

		protected float delayBetweenShots;
		protected bool isReloading = false;
		protected bool isCooldown;
		protected float lastBulletTime;
		protected int currentAmmo;
		public override void SetTool(PlayerController _controller)
		{
			currentAmmo = maxAmmo;
			isReloading = false;
			isCooldown = false;
			delayBetweenShots = 1 / fireRate;
			lastBulletTime = 0;
			Debug.Log($"I reset the following: currentAmmo: {currentAmmo}, isReloading: {isReloading}, isCooldown: {isCooldown}, delayBetweenShots: {delayBetweenShots}, lastBulletTime: {lastBulletTime}");
		}
		public override void OnPrimaryAttack(bool _isPressed)
		{
			if (!_isPressed) { return; }

			if (!canShoot("Primary Fire")) { return; }
			shoot(Damage, damageFallOff, accuracy);
		}
		public override void OnSecondaryAttack(bool _isPressed)
		{
			if (!_isPressed) { return; }

			if (!canShoot("Alt Fire")) { return; }
			altFire();
		}
		protected bool canShoot(string _attackType)
		{
			if (currentAmmo <= 0)
			{
				Debug.Log("Cannot " + _attackType + " while out of ammo");
				reload();
				return false;
			}
			if (lastBulletTime + delayBetweenShots > Time.time)
			{
				Debug.Log("Cannot " + _attackType + " while on cooldown");
				return false;
			}
			if (isReloading)
			{
				if (currentAmmo > 0)
				{
					isReloading = false;
					return true;
				}
				Debug.Log("Cannot " + _attackType + " while reloading");
				return false;
			}

			return true;
		}
		public override void OnTertiaryAttack(bool _isPressed)
		{
			if (!_isPressed) { return; }
			if (isReloading)
			{
				Debug.Log("Cannot reload while reloading");
				return;
			}
			if (currentAmmo == maxAmmo)
			{
				Debug.Log("Cannot reload while full ammo");
				return;
			}
			reload();
		}
		protected abstract void shoot(float _damage, DamageFallOff _falloff, float _accuracy, bool _noBulletTime = false);
		protected abstract void altFire();
		protected virtual async void reload()
		{
			isReloading = true;
			Debug.Log("Reloading...");
			await UniTask.WaitForSeconds(reloadTime);
			if (isReloading == false)
			{
				//means reload was cancelled
				return;
			}
			currentAmmo = maxAmmo;
			isReloading = false;
		}
	}

	[Serializable]
	public class DamageFallOff
	{
		public FallOff[] FallOffs = new FallOff[3];
		public float GetFallOffDamage(float _distance, float _damage)
		{
			//Debug.Log($"I recieved Distance: {_distance} and Damage: {_damage}");
			for (int _i = FallOffs.Length - 1; _i >= 0; --_i)
			{
				if (_distance > FallOffs[_i].Distance)
				{
					//lerp between the two values
					if (_i + 1 < FallOffs.Length)
					{
						var _lerpDistanceScaled = Mathf.InverseLerp(FallOffs[_i + 1].Distance, FallOffs[_i].Distance, _distance);
						//Debug.Log($"Lerp Distance Scaled: {_lerpDistanceScaled} returning: {Mathf.Lerp(FallOffs[_i + 1].Damage, FallOffs[_i].Damage, _lerpDistanceScaled)}");
						return Mathf.Lerp(FallOffs[_i + 1].Damage, FallOffs[_i].Damage, _lerpDistanceScaled);
					}
					//Debug.Log($"returning fall off damage: {FallOffs[_i].Damage}");
					return FallOffs[_i].Damage;
				}
			}
			//Debug.Log($"Shooting {_damage}");
			return _damage;
		}
	}
	[Serializable]
	public class FallOff
	{
		public float Distance;
		public float Damage;
	}

}