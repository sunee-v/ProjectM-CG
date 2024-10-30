using UnityEngine;
namespace RangedTools
{
	public abstract class HitscanTool : RangeTool
	{
		protected bool isShooting = false;
		public override void SetTool(PlayerController _controller)
		{
			base.SetTool(_controller);
			isShooting = false;
		}
		protected override void altFire()
		{

		}

		protected override void shoot(float _damage, DamageFallOff _falloff, float _accuracy, bool _noBulletTime = false)
		{
			--currentAmmo;
			Transform _hit = CameraManager.Instance.GetBloomHit(_accuracy, out var _distance)?.transform;

			if (_hit == null) { return; }
			//spawn hit vfx
			if (_hit.TryGetComponent(out IDamageable _damageable))
			{
				var _falloffDamage = _falloff.GetFallOffDamage(_distance, _damage);
				_damageable.TakeDamageServerRpc(_falloffDamage, Element, MultiplayerGameManager.Instance.LocalController.UserData.Value, new DamageSource(_damage, Element, true, StaticListManager.Instance.GetToolId(this)));
			}
			if (_noBulletTime) { return; }
			lastBulletTime = Time.time;
		}
	}
}