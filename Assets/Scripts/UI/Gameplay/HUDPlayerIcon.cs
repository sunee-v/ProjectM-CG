using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HUDPlayerIcon : UIIcon
{
	[SerializeField] private Transform Icon;
	[SerializeField] private Transform HpBar;
	private Material hpMaterialInstance;
	public async void SetPlayer(PlayerController _playerController)
	{
		UserData _temp = _playerController.UserData.Value;
		await UniTask.WaitUntil(() => MultiplayerGameManager.Instance.LocalController != null);
		//check if ally
		if (MultiplayerGameManager.Instance.LocalController.GetEnemyTeam() == _temp.Team)
		{
			return;
		}
		var _hpBar = GetComponent<HpBarFactory<IHealthBar>>();
		_playerController.MaxHealth.OnValueChanged += _hpBar.SetMaxHealth;
		_playerController.CurrentHealth.OnValueChanged += _hpBar.SetHealth;
		_playerController.Shield.OnValueChanged += _hpBar.SetShield;
		_hpBar.SetMaxHealth(0, _playerController.MaxHealth.Value);
		_hpBar.SetHealth(0, _playerController.CurrentHealth.Value);
		_hpBar.SetShield(0, _playerController.Shield.Value);
		//UserData _client = MultiplayerGameManager.Instance.UserDataList.Find(x => x.ClientID == _playerController.GetComponent<NetworkObject>().OwnerClientId);
	}
	protected override void createMaterialInstance()
	{
		materialInstance = new Material(Icon.GetComponent<Image>().material);
		Icon.GetComponent<Image>().material = materialInstance;
		hpMaterialInstance = new Material(HpBar.GetComponent<Image>().material);
		HpBar.GetComponent<Image>().material = hpMaterialInstance;
	}
}
