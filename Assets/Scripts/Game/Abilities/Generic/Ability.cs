using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
public abstract class Ability : ScriptableObject//, IInventoryItem
{
	[field: Header("Ability Stats")]
	[field: SerializeField] public string Name { get; set; }
	[field: SerializeField, TextArea(10, 99)] public string Description { get; set; }
	public float Price { get; set; }
	[field: SerializeField] public Sprite Icon { get; set; }

	[Header("Ability VFX")]
	[field: SerializeField, Header("Ability Hit VFX")] public GameObject AbilityHitVFX { get; private set; }
	[Header("Ability SFX")]
	[field: SerializeField] public AudioClip[] AbilityCastSFX { get; private set; }
	[field: SerializeField] public AudioClip[] AbilitySpawnedSFX { get; private set; }
	[field: SerializeField] public AudioClip[] AbilityHitSFX { get; private set; }

	[field: Header("Ability Scalings:"), Space(2)]
	[field: SerializeField] public Element Element { get; private set; } = Element.Water;
	[field: SerializeField] public float Damage { get; private set; }
	[field: SerializeField] public float Cooldown { get; private set; }
	public float ActiveCooldown { get; private set; }
	public bool IsOnCooldown { get; set; }
	protected PlayerController controller;
	protected int index;
	private CancellationTokenSource cts;
	public virtual void OnCast()
	{
		AbilityCooldown(cts.Token).Forget();
	}
	public T Create<T>(int _index, PlayerController _controller, T _abilityOriginal) where T : Ability
	{
		T _ability = Instantiate(_abilityOriginal);
		_ability.SetAbility(_index, _controller);
		return _ability;
	}
	protected void changeAnimation(string _animationName, int _layer = 1, float _crossFade = .1f)
	{
		controller.SendMessage("ChangeAnimation", new AnimationData(_animationName, _layer, _crossFade));
	}
	public void SetAbility(int _index, PlayerController _controller)
	{
		index = _index;
		controller = _controller;
		cts = new();
	}
	/// <summary>
	/// @alex-memo 2023
	/// Starts the cooldown of the ability
	/// </summary>
	/// <returns></returns>
	private async UniTask AbilityCooldown(CancellationToken _token)
	{
		ActiveCooldown = 0;
		IsOnCooldown = true;
		while (ActiveCooldown < Cooldown)
		{
			ActiveCooldown += Time.deltaTime;
			await UniTask.Yield(_token);
		}
		IsOnCooldown = false;
	}
	public void SetActiveCooldown(float _amount)
	{
		ActiveCooldown = _amount;
	}
	public void SetRemainingCooldown(float _amount)
	{
		ActiveCooldown = Cooldown - _amount;
	}
	public void ReduceCooldown(float _amount)
	{
		if (ActiveCooldown > Cooldown) { return; }
		ActiveCooldown += _amount;
	}
	public void ResetCooldown()
	{
		ActiveCooldown = Cooldown;
		IsOnCooldown = false;
	}
	private void OnDestroy()
	{
		if (cts != null) { cts.Cancel(); cts.Dispose(); }
	}
}

