using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
/// <summary>
/// @alex-memo 2023
/// </summary>
public class CameraManager : MonoBehaviour
{
	[SerializeField] private Volume deathVolume;
	[SerializeField, Header("Camera Components")] private CinemachineCamera cam;
	public Transform MainCameraTransform { get; private set; }
	public static CameraManager Instance;
	public bool HasAimAssist { get; private set; } = false;
	public RaycastHit[] RaycasterHits { get; private set; }
	[SerializeField] private InputActionReference playerLook;
	private float controllerSens = 200;
	private float kbmSens = .1f;

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;

		MainCameraTransform = Camera.main.transform;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
	private void OnDestroy()
	{
		Instance = null;
		Cursor.lockState = CursorLockMode.Confined;
		Cursor.visible = true;
	}
	public void OnCamTurned(InputAction.CallbackContext _ctx)
	{
		//why c# why, please let me use a switch for comparing types
		if (_ctx.control.device is Gamepad)
		{
			HasAimAssist = true;
			return;
		}
		HasAimAssist = false;
		playerLook.action.ApplyParameterOverride("ScaleVector2:x", kbmSens);
		playerLook.action.ApplyParameterOverride("ScaleVector2:y", kbmSens);
	}
	public void SetTarget(Transform _t)
	{
		cam.Follow = _t;
	}
	public void DeathCamera()
	{
		deathVolume.profile.TryGet(out ColorAdjustments _colourSaturation);
		_colourSaturation.saturation.value = -100;
	}
	public void RespawnCamera()
	{
		deathVolume.profile.TryGet(out ColorAdjustments _colourSaturation);
		_colourSaturation.saturation.value = 0;
	}
	private void Update()
	{
		cameraRaycaster();

		if (HasAimAssist)
		{
			aimAssist();
		}
	}
	private void cameraRaycaster()
	{
		RaycasterHits = Physics.RaycastAll(MainCameraTransform.position, MainCameraTransform.forward, 100f);
		//RaycasterHits = Physics.SphereCastAll(MainCameraTransform.position, .1f, MainCameraTransform.forward, 100f);
		System.Array.Sort(RaycasterHits, (_prev, _next) => _prev.distance.CompareTo(_next.distance));
	}
	public Vector3 GetCameraLookAt()
	{
		return MainCameraTransform.position + MainCameraTransform.forward * 10;
	}
	public RaycastHit? GetClosestHit(out float _distance, bool _ignoreTeammates = true)
	{
		_distance = float.MaxValue;
		if (RaycasterHits.Length == 0) { return null; }
		if (!_ignoreTeammates || MultiplayerGameManager.Instance.LocalController == null)
		{
			return getClosestHit(out _distance);
		}
		return getClosestNonTeammateHit(RaycasterHits, out _distance);
	}
	public RaycastHit? GetClosestUnintersectedHit(out float _distance, bool _ignoreTeammates = true)
	{
		_distance = float.MaxValue;
		var _playerPos = MultiplayerGameManager.Instance.LocalController.transform.position;
		_playerPos.y += 1.5f;
		RaycastHit? _cameraHit = null;

		if (RaycasterHits.Length == 0) { return _cameraHit; }

		if (!_ignoreTeammates || MultiplayerGameManager.Instance.LocalController == null)
		{
			_cameraHit = getClosestHit(out _distance);
		}
		else
		{
			_cameraHit = getClosestNonTeammateHit(RaycasterHits, out _distance);
		}
		if (_cameraHit == null) { return null; }
		hitSameTarget(ref _cameraHit, ref _distance);
		//Debug.Log("I hit: " + _cameraHit?.transform.name);
		return _cameraHit;
	}

	/// <summary>
	/// Returns the unintersected hit with bloom applied
	/// </summary>
	/// <param name="_accuracy"></param>
	/// <param name="_distance"></param>
	/// <returns></returns>
	public RaycastHit? GetBloomHit(float _accuracy, out float _distance)
	{
		var _playerPos = MultiplayerGameManager.Instance.LocalController.transform.position;
		_playerPos.y += 1.5f;
		const float _deviation = 0.12f;

		float _bloomRangeX = (1 - _accuracy) * Random.Range(-_deviation, _deviation); //Deviation (_deviation% of the screen range based on accuracy)
		float _bloomRangeY = (1 - _accuracy) * Random.Range(-_deviation, _deviation);

		Vector3 _bloomOffset = (MainCameraTransform.right * _bloomRangeX) + (MainCameraTransform.up * _bloomRangeY);
		Vector3 _shootingDirection = (MainCameraTransform.forward + _bloomOffset).normalized;

		RaycastHit[] _hits = new RaycastHit[10];
		Physics.RaycastNonAlloc(MainCameraTransform.position + _bloomOffset, _shootingDirection, _hits, 100f);
		Debug.DrawRay(MainCameraTransform.position + _bloomOffset, _shootingDirection * 100, Color.green, 10f);
		RaycastHit? _cameraHit = getClosestNonTeammateHit(_hits, out _distance);
		if (_cameraHit == null) { return null; }

		hitSameTarget(ref _cameraHit, ref _distance);
		return _cameraHit;
	}
	private void hitSameTarget(ref RaycastHit? _cameraHit, ref float _distance)
	{
		var _playerPos = MultiplayerGameManager.Instance.LocalController.transform.position;
		_playerPos.y += 1.5f;
		if (Physics.Raycast(_playerPos, (Vector3)_cameraHit?.point - _playerPos, out var _playerHit, 100f))
		{
			if (_playerHit.transform != _cameraHit?.transform)
			{
				//Debug.Log($"I hit: {_playerHit.transform.name} instead of {_cameraHit?.transform.name}");
				_cameraHit = _playerHit;
			}
			_distance = _playerHit.distance;
			//Debug.Log($"At distance: {_distance}");
		}
	}
	/// <summary>
	/// Returns the closest hit no matter what it is
	/// </summary>
	/// <returns></returns>
	private RaycastHit? getClosestHit(out float _distance)
	{
		_distance = float.MaxValue;
		if (RaycasterHits.Length == 0) { return null; }
		_distance = RaycasterHits[0].distance;
		return RaycasterHits[0];
	}
	/// <summary>
	/// Returns the closest hit that is not a teammate
	/// </summary>
	/// <param name="_distance"></param>
	/// <param name="_point"></param>
	/// <returns></returns>
	private RaycastHit? getClosestNonTeammateHit(RaycastHit[] _hits, out float _distance)
	{
		_distance = float.MaxValue;
		for (int _i = 0; _i < _hits.Length; ++_i)
		{
			if (_hits[_i].transform == null) { continue; }
			if (!_hits[_i].transform.CompareTag(MultiplayerGameManager.Instance.LocalController.tag))//if they have diff tag then return that one
			{
				//Debug.Log("I hit: " + _hits[i].transform.name + " at index: " + i);
				_distance = _hits[_i].distance;
				return _hits[_i];
			}
		}
		return null;
	}
	private void aimAssist()
	{
		RaycastHit? _hit = getClosestNonTeammateHit(RaycasterHits, out var _distance);
		if (_hit == null) { return; }

		if (!_hit.Value.collider.IsEnemy(MultiplayerGameManager.Instance.LocalController))
		{
			playerLook.action.ApplyParameterOverride("ScaleVector2:x", -controllerSens);
			playerLook.action.ApplyParameterOverride("ScaleVector2:y", controllerSens);
			return;
		}
		float _aimAssistStrength = Mathf.Lerp(.4f, .1f, Mathf.InverseLerp(0, 60, _distance));
		Debug.Log($"Aim Assist Strength: {_aimAssistStrength}");
		playerLook.action.ApplyParameterOverride("ScaleVector2:x", -controllerSens * .4f);
		playerLook.action.ApplyParameterOverride("ScaleVector2:y", controllerSens * .4f);
	}
	private void OnDrawGizmos()
	{
		if (MainCameraTransform == null) { return; }
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(MainCameraTransform.position, MainCameraTransform.position + MainCameraTransform.forward * 100);
		if (MultiplayerGameManager.Instance == null) { return; }
		if (MultiplayerGameManager.Instance.LocalController == null) { return; }
		Gizmos.color = Color.red;
		var _playerPos = MultiplayerGameManager.Instance.LocalController.transform.position;
		_playerPos.y += 1.5f;
		if (RaycasterHits.Length > 0)
		{
			Gizmos.DrawLine(_playerPos, RaycasterHits[0].point);
		}
	}
}