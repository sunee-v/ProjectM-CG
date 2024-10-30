using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
	public static HUDManager Instance { get; private set; }
	[Header("Top Bar")]
	[SerializeField] private GameObject TopPlayerIconPrefab;
	[SerializeField] private Transform TopBarTeamAHolder;
	[SerializeField] private Transform TopBarTeamBHolder;
	[SerializeField] private TMP_Text timerText;
	private async void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}
#if UNITY_STANDALONE || UNITY_EDITOR
		await UniTask.WaitUntil(() => MultiplayerGameManager.Instance != null);
		MultiplayerGameManager.Instance.UpdateHUD += UpdateHUD;
		MultiplayerGameManager.Instance.GameTimer.OnValueChanged += OnGameTimeChanged;
		GetComponent<Canvas>().worldCamera = Camera.main;
#endif
		Instance = this;//In this case we do it at the end so that multiplayergamemanager can wait for everything in awake to be done
	}
	private void UpdateHUD()
	{
		Debug.Log("Updating HUD");
	}
	public async void SetUpPlayerTopBar(GameObject _playerGo)
	{
		var _controller = _playerGo.GetComponent<PlayerController>();
		await UniTask.WaitUntil(() => _controller.UserData.Value != null);
		var _userData = _controller.UserData.Value;

		GameObject _playerIcon = Instantiate(TopPlayerIconPrefab, _userData.Team == "TeamA" ? TopBarTeamAHolder : TopBarTeamBHolder);
		_playerIcon.GetComponent<HUDPlayerIcon>().SetPlayer(_controller);
	}
	public void SetPlayerHpBar(PlayerController _controller)
	{
		var _hpbar = GetComponentInChildren<UIHealthBar>();
		_controller.MaxHealth.OnValueChanged += _hpbar.SetMaxHealth;
		_controller.CurrentHealth.OnValueChanged += _hpbar.SetHealth;
		_controller.Shield.OnValueChanged += _hpbar.SetShield;
		_hpbar.SetMaxHealth(0, _controller.MaxHealth.Value);
		_hpbar.SetHealth(0, _controller.CurrentHealth.Value);
	}
	private void OnDestroy()
	{
		Instance = null;
	}
	private void OnGameTimeChanged(int _old, int _new)
	{
		//Debug.LogError($"Game Time: {_new}");
		timerText.text = TimeSpan.FromSeconds(_new).ToString(@"mm\:ss");
	}
}
