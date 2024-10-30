using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
/// <summary>
/// @alex-memo 2023
/// </summary>
public static class Extensions
{
	public static void SetConsistentScale(this Transform _transform, float _scale)
	{
		_transform.localScale = new Vector3(_scale, _scale, _scale);
	}
	public static void SetHexColour(this ref Color _colour, string _hexColour)
	{
		if (ColorUtility.TryParseHtmlString(_hexColour, out Color _tempCol))
		{
			_colour = _tempCol;
		}
	}

	/// <summary>
	/// This method returns the client rpc params for the given client id
	/// </summary>
	/// <param name="_clientId">The client ID for which you want to get params for</param>
	/// <returns></returns>
	public static ClientRpcParams GetClientParams(this ulong _clientId)
	{
		ClientRpcParams _clientRpcParams = new()
		{
			Send = new ClientRpcSendParams
			{
				TargetClientIds = new ulong[] { _clientId }
			}
		};
		return _clientRpcParams;
	}
	/// <summary>
	/// Returns true if the collider is enemy team or explicitly enemy
	/// </summary>
	/// <param name="_coll"></param>
	/// <param name="_ownerController"></param>
	/// <returns></returns>
	public static bool IsEnemy(this Collider _coll, EntityController _ownerController)
	{
		return _coll.CompareTag(_ownerController.GetEnemyTeam()) || _coll.CompareTag("Enemy");
	}
	public static Vector3 ClampVector3(this Vector3 _vector, float _min, float _max)
	{
		_vector.x = Mathf.Clamp(_vector.x, _min, _max);
		_vector.y = Mathf.Clamp(_vector.y, _min, _max);
		_vector.z = Mathf.Clamp(_vector.z, _min, _max);
		return _vector;
	}
	/// <summary>
	/// This method returns the first child of the transform that contains the given name
	/// </summary>
	/// <param name="_transform"></param>
	/// <param name="_name"></param>
	/// <returns></returns>
	public static Transform FindContains(this Transform _transform, string _name)
	{
		if (_transform == null) { return null; }
		foreach (Transform _child in _transform)
		{
			if (_child.name.Contains(_name))
			{
				return _child;
			}
		}
		return null;
	}
	/// <summary>
	/// Returns true if any of the tags are attached to the gameobject
	/// </summary>
	/// <param name="_gameobject"></param>
	/// <param name="_tags">Tags to compare object tag to</param>
	/// <returns></returns>
	public static bool CompareTags(this GameObject _gameobject, params string[] _tags)
	{
		if (_gameobject == null) { return false; }
		foreach (string _tag in _tags)
		{
			if (string.IsNullOrEmpty(_tag)) { continue; }
			if (_gameobject.CompareTag(_tag))
			{
				return true;
			}
		}
		return false;
	}
	/// <summary>
	/// Returns true if the object layer is the same as the sent one
	/// </summary>
	/// <param name="_gameobject"></param>
	/// <param name="_layer">Layer to compare object layer to</param>
	/// <returns></returns>
	public static bool CompareLayer(this GameObject _gameobject, string _layer)
	{
		if (string.IsNullOrEmpty(_layer)) { return false; }
		if (_gameobject.layer == LayerMask.NameToLayer(_layer)) { return true; }
		return false;
	}
	public static void GetAbilitySpawnTransform(this Transform _transform, out Vector3 _spawnPosition, out Quaternion _rotation)
	{
		_spawnPosition = _transform.position + _transform.forward / 10;
		_spawnPosition.y += 1.5f;

		var _camForward = Camera.main.transform.forward;
		var _camPos = Camera.main.transform.position;

		Vector3 _direction;
		if (CameraManager.Instance == null)
		{
			_rotation = _transform.rotation;
			return;
		}
		RaycastHit? _hit = CameraManager.Instance.GetClosestHit(out var _);
		Vector3 _closestHit = _hit?.point ?? Vector3.zero;
		if (_closestHit != Vector3.zero)
		{
			_direction = _closestHit - _spawnPosition;
		}
		else
		{
			_direction = (_camPos + _camForward * 100f) - _spawnPosition;
		}
		_rotation = Quaternion.LookRotation(_direction);
	}
	public static bool Contains(this string _string, params string[] _contains)
	{
		foreach (string _contain in _contains)
		{
			if (_string.Contains(_contain))
			{
				return true;
			}
		}
		return false;
	}
	public static bool Equals<T>(this T _object, params T[] _comparisons)
	{
		foreach (T _otherObj in _comparisons)
		{
			if (_object.Equals(_otherObj))
			{
				return true;
			}
		}
		return false;
	}
	public static string ToString<T>(this IEnumerable<T> _list)
	{
		System.Text.StringBuilder _sb = new("{\n");
		foreach (T _item in _list)
		{
			_sb.Append(_item.ToString() + "\n");
		}
		_sb.Append("}");
		return _sb.ToString();
	}
	public static bool IsNullOrEmpty(this string _string)
	{
		return string.IsNullOrEmpty(_string);
	}
	#region UIExtensions
	/// <summary>
	/// @memo 2023
	/// Fades away a canvas group
	/// </summary>
	/// <param name="_canvas"></param>
	/// <returns></returns>
	public static async Awaitable FadeOut(this CanvasGroup _canvas)
	{
		if (_canvas == null) { return; }

		_canvas.alpha = 1;
		while (_canvas.alpha > .01)
		{
			_canvas.alpha -= Time.deltaTime;
			await Awaitable.EndOfFrameAsync();
		}
		_canvas.alpha = 0;
		_canvas.gameObject.SetActive(false);

	}
	/// <summary>
	/// @memo 2023
	/// Fades in a canvas group
	/// </summary>
	/// <param name="_canvas"></param>
	/// <returns></returns>
	public static async UniTask FadeIn(this CanvasGroup _canvas, CancellationToken _cts = default)
	{
		if (_canvas == null) { return; }

		_canvas.gameObject.SetActive(true);
		_canvas.alpha = 0;
		while (_canvas.alpha < .99)
		{
			_canvas.alpha += Time.deltaTime;
			await Awaitable.EndOfFrameAsync(_cts);
		}
		_canvas.alpha = 1;
	}
	/// <summary>
	/// @memo 2023
	/// Fades a canvas group
	/// </summary>
	/// <param name="_canvas"></param>
	/// <returns></returns>
	public static async UniTask Fade(this CanvasGroup _canvas, float _desiredAlpha, float _lerpTime = 1, CancellationToken _cts = default)
	{
		if (_canvas == null) { return; }
		_desiredAlpha = Mathf.Clamp(_desiredAlpha, 0, 1);

		_canvas.gameObject.SetActive(true);
		float _startAlpha = _canvas.alpha;
		if (_startAlpha == _desiredAlpha) { return; }
		float _time = 0;
		while (_time < _lerpTime)
		{
			if (_canvas == null) { return; }
			_canvas.alpha = Mathf.Lerp(_startAlpha, _desiredAlpha, _time / _lerpTime);
			_time += Time.deltaTime;
			await Awaitable.EndOfFrameAsync(_cts);
		}
		_canvas.alpha = _desiredAlpha;
		_canvas.gameObject.SetActive(_desiredAlpha > 0);
	}

	#endregion
}