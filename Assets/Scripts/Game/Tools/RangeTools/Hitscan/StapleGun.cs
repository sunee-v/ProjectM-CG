using System;
using UnityEngine;
namespace RangedTools
{
	[CreateAssetMenu(fileName = "StapleGun", menuName = "Game/Tools/01_StapleGun")]

	public class StapleGun : HitscanTool
	{
		[Header("Alt Fire")]
		[SerializeField, Min(1)] private float adsDamage = 200f;
		[SerializeField] protected DamageFallOff adsFallOff;
		[SerializeField, Range(0, 1)] protected float adsAccuracy = 1f;
		private bool isAdsing = false;
		public override void OnPrimaryAttack(bool _isPressed)
		{
			if (!_isPressed) { return; }
			if (!canShoot("Alt Fire")) { return; }
			switch (isAdsing)
			{
				case true:
					shoot(adsDamage, adsFallOff, adsAccuracy);
					break;
				case false:
					shoot(Damage, damageFallOff, accuracy);
					break;
			}
		}
		public override void OnSecondaryAttack(bool _isPressed)
		{
			isAdsing = _isPressed;
			//CameraController.Instance.OnChangeAdsState(_isPressed);
		}
	}
}