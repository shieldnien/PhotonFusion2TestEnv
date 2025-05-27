using UnityEngine;
using Fusion;
using Fusion.Addons.KCC;

namespace Example.AdvancedMovement
{
	/// <summary>
	/// Advanced player implementation. Shows advanced approach of processing input and controlling KCC.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	public sealed class AdvancedPlayer : NetworkBehaviour
	{
		public KCC                 KCC;
		public AdvancedPlayerInput Input;
		public Transform           CameraPivot;
		public Transform           CameraHandle;

		public override void FixedUpdateNetwork()
		{
			// Apply look rotation delta. This propagates to Transform component immediately.
			KCC.AddLookRotation(Input.CurrentInput.LookRotationDelta);

			// Set world space input direction. This value is processed later when KCC executes its FixedUpdateNetwork().
			// By default the value is processed by EnvironmentProcessor - which defines base character speed, handles acceleration/friction, gravity and many other features.
			Vector3 inputDirection = KCC.Data.TransformRotation * new Vector3(Input.CurrentInput.MoveDirection.x, 0.0f, Input.CurrentInput.MoveDirection.y);
			KCC.SetInputDirection(inputDirection);

			// Comparing current input to previous input - this prevents glitches when input is lost.
			if (Input.CurrentInput.Actions.WasPressed(Input.PreviousInput.Actions, AdvancedInput.JUMP_BUTTON) == true)
			{
				if (KCC.Data.IsGrounded == true)
				{
					// Set world space jump vector. This value is processed later when KCC executes its FixedUpdateNetwork().
					KCC.Jump(Vector3.up * 6.0f);
				}
			}

			if (KCC.Data.IsGrounded == true)
			{
				// Sprint is a "user" feature. It is not a default KCC feature.
				// Please check SprintProcessor for more details.
				KCC.SetSprint(Input.CurrentInput.Actions.IsSet(AdvancedInput.SPRINT_BUTTON));
			}
		}

		private void LateUpdate()
		{
			// Only input authority needs to update camera.
			if (HasInputAuthority == false)
				return;

			// Update camera pivot and transfer properties from camera handle to Main Camera.
			// LateUpdate() is called after all Render() calls => data in KCC is correctly interpolated.

			Vector2 pitchRotation = KCC.Data.GetLookRotation(true, false);
			CameraPivot.localRotation = Quaternion.Euler(pitchRotation);

			Camera.main.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
		}
	}
}
