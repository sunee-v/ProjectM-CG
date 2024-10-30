using UnityEngine;
namespace Game.Barrels
{
	public interface IBarrelCommand
	{
		void Execute(EntityController[] _collider);
	}
	public abstract class BarrelCommand : ScriptableObject, IBarrelCommand
	{
		[SerializeField] protected float effectAmount;
		[SerializeField] protected Material barrelLinesMat;
		public abstract void Execute(EntityController[] _collider);
		public virtual void Init(out Material _barrelMat)
		{
			_barrelMat = new(barrelLinesMat);
		}
	}
}