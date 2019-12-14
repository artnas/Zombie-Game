﻿using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using Mapbox.Unity.Map;

namespace Mapbox.Examples
{
	public class AstronautMouseController : MonoBehaviour
	{
		[Header("Character")]
		[SerializeField]
		GameObject character;
		[SerializeField]
		float characterSpeed;
		[SerializeField]
		Animator characterAnimator;

		private PlayerCharacter _playerCharacter;
		public Animation animation;

		[Header("References")]
		[SerializeField]
		AstronautDirections directions;
		[SerializeField]
		Transform startPoint;
		[SerializeField]
		Transform endPoint;
		[SerializeField]
		AbstractMap map;
		[SerializeField]
		GameObject rayPlane;
		[SerializeField]
		Transform _movementEndPoint;

		[SerializeField]
		LayerMask layerMask;

		Ray ray;
		RaycastHit hit;
		LayerMask raycastPlane;
		float clicktime;
		bool characterDisabled;

		void Start()
		{
			_playerCharacter = GetComponent<PlayerCharacter>();
			characterAnimator = GetComponentInChildren<Animator>();
			animation = GetComponentInChildren<Animation>();
			if (!Application.isEditor)
			{
				this.enabled = false;
				return;
			}
		}

		void Update()
		{
			if (characterDisabled)
				return;

			bool click = false;

			if (Input.GetMouseButtonDown(0))
			{
				clicktime = Time.time;
			}
			if (Input.GetMouseButtonUp(0))
			{
				if (Time.time - clicktime < 0.15f)
				{
					click = true;
				}
			}

			if (click && !UISystem.Instance.EventSystem.IsPointerOverGameObject())
			{
				ray = cam.ScreenPointToRay(Input.mousePosition);

				if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
				{
					startPoint.position = transform.localPosition;
					endPoint.position = hit.point;
					MovementEndpointControl(hit.point, true);

					directions.Query(GetPositions, startPoint, endPoint, map);
				}
			}
		}

#region Character : Movement
		List<Vector3> futurePositions;
		bool interruption;
		void GetPositions(List<Vector3> vecs)
		{
			futurePositions = vecs;

			if (futurePositions != null && _playerCharacter.IsMoving)
			{
				interruption = true;
			}
			if (!_playerCharacter.IsMoving)
			{
				interruption = false;
				MoveToNextPlace();
			}
		}

		Vector3 nextPos;
		void MoveToNextPlace()
		{
			if (futurePositions.Count > 0)
			{
				nextPos = futurePositions[0];
				futurePositions.Remove(nextPos);

				_playerCharacter.IsMoving = true;
				animation.Play("Player-Gun-Walk");
				StartCoroutine(MoveTo());
			}
			else if (futurePositions.Count <= 0)
			{
				_playerCharacter.IsMoving = false;
				animation.Play("Player-Gun-Idle");
			}
		}

		Vector3 prevPos;
		IEnumerator MoveTo()
		{
			prevPos = transform.localPosition;

			float time = CalculateTime();
			float t = 0;

			StartCoroutine(LookAtNextPos());

			while (t < 1 && !interruption)
			{
				t += Time.deltaTime / time;

				transform.localPosition = Vector3.Lerp(prevPos, nextPos, t);

				yield return null;
			}

			interruption = false;
			MoveToNextPlace();
		}

		float CalculateTime()
		{
			float timeToMove = 0;

			timeToMove = Vector3.Distance(prevPos, nextPos) / characterSpeed;

			return timeToMove;
		}
		#endregion

		#region Character : Rotation
		IEnumerator LookAtNextPos()
		{
			var lookVector = nextPos - character.transform.position;

			if (lookVector != Vector3.zero)
			{
				Quaternion neededRotation = Quaternion.LookRotation(lookVector);
				Quaternion thisRotation = character.transform.localRotation;

				float t = 0;
				while (t < 1.0f)
				{
					t += Time.deltaTime / 0.25f;
					var rotationValue = Quaternion.Slerp(thisRotation, neededRotation, t);
					character.transform.rotation = Quaternion.Euler(0, rotationValue.eulerAngles.y, 0);
					yield return null;
				}
			}
		}
		#endregion
		
		[Header("CameraSettings")]
		[SerializeField]
		Camera cam;

		#region Utility
		public void DisableCharacter()
		{
			characterDisabled = true;
			_playerCharacter.IsMoving = false;
			StopAllCoroutines();
			character.SetActive(false);
		}

		public void EnableCharacter()
		{
			characterDisabled = false;
			character.SetActive(true);
		}

		public void LayerChangeOn()
		{
			Debug.Log("OPEN");
		}

		public void LayerChangeOff()
		{
			Debug.Log("CLOSE");
		}

		void MovementEndpointControl(Vector3 pos, bool active)
		{
			_movementEndPoint.position = new Vector3(pos.x, 0.2f, pos.z);
			_movementEndPoint.gameObject.SetActive(active);
		}
		#endregion
	}
}