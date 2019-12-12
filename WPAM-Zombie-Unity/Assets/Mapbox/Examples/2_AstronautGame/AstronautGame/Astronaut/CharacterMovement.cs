using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.Examples
{
	public class CharacterMovement : MonoBehaviour
	{
		// public Material[] Materials;
		public Transform Target;
		public Animator CharacterAnimator;
		public Animation Animation;
		public float Speed;
		AstronautMouseController _controller;
		private PlayerCharacter _playerCharacter;
		void Start()
		{
			_playerCharacter = GetComponent<PlayerCharacter>();
			_controller = GetComponent<AstronautMouseController>();
		}

		void Update()
		{
			
			if (_controller.enabled)// Because the mouse control script interferes with this script
			{
				return;
			}

			// foreach (var item in Materials)
			// {
			// 	item.SetVector("_CharacterPosition", transform.position);
			// }

			var distance = Vector3.Distance(transform.position, Target.position);
			if (distance > 0.01f)
			{
				transform.LookAt(Target.position);
				transform.position = Vector3.Lerp(transform.position, Target.position, Time.deltaTime * 2f);
				// transform.Translate(Vector3.forward * (Speed * Time.deltaTime));
				// CharacterAnimator.SetBool("IsWalking", true);
				_playerCharacter.IsMoving = true;
				Animation.Play("Player-Gun-Walk");
			}
			else
			{
				// var currentClipName = Animation.clip.name;
				// Debug.Log(currentClipName);
				// // CharacterAnimator.SetBool("IsWalking", false);
				_playerCharacter.IsMoving = false;
				Animation.Play("Player-Gun-Idle");
			}
		}
	}
}