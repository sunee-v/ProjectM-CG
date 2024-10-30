using UnityEngine;

public abstract class ToolAttack : ScriptableObject
{
	[field: SerializeField] public float Damage { get; protected set; }
	[field: SerializeField] public Element Element { get; protected set; }
	protected int attackNumber = 0;
	protected int secondaryAttackNumber = 0;
	public abstract void SetTool(PlayerController _controller);
	/// <summary>
	/// Called when the primary attack button is pressed
	/// </summary>
	/// <param name="_isPressed">Recieves if is is a press or a release, if press true, if released false</param>
	public abstract void OnPrimaryAttack(bool _isPressed);
	public abstract void OnSecondaryAttack(bool _isPressed);
	public abstract void OnTertiaryAttack(bool _isPressed);
	public virtual GameObject GetPrefab() { return null; }
}