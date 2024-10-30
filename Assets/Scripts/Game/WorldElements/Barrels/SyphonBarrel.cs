using UnityEngine;
namespace Game.Barrels
{
	[CreateAssetMenu(fileName = "SyphonBarrel", menuName = "Game/Barrels/SyphonBarrel")]
	public class SyphonBarrel : BarrelCommand
	{
		public override void Execute(EntityController[] _collider)
		{
			foreach (var _entityController in _collider)
			{
				_entityController.HealOrShield(effectAmount);
			}
		}
	}
}