namespace Example.MovingPlatform
{
	using System;
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Basic platform which moves the object by sampling AnimationClip. It must be executed first, before any player executes its movement.
	/// This script needs to be a KCC processor (deriving from NetworkTRSPProcessor) to be correctly tracked by PlatformProcessor.
	/// It also implements IMapStatusProvider - providing status text about animation progress shown in UI.
	/// </summary>
	[DefaultExecutionOrder(-1000)]
	[RequireComponent(typeof(Rigidbody))]
    public sealed unsafe class AnimatedPlatform : NetworkTRSPProcessor, IPlatform, IAfterClientPredictionReset, IBeforeAllTicks, IAfterTick, IMapStatusProvider
    {
		// PRIVATE MEMBERS

		[SerializeField]
		private AnimationClip _animation;
		[SerializeField]
		private float _speed = 1.0f;
		[SerializeField]
		private bool _loop = true;

		[Networked]
		private float _time { get; set; }
		[Networked]
		private Vector3 _position { get; set; }
		[Networked]
		private Quaternion _rotation { get; set; }

		private Transform _transform;
		private Rigidbody _rigidbody;
		private int       _lastRenderFrame;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			_lastRenderFrame = default;

			if (HasStateAuthority == true)
			{
				// Write initial values.
				StoreTransform();
			}
			else
			{
				// Read initial values.
				RestoreTransform();
			}

			// Enable prediction on this object.
			if (Runner.GameMode != GameMode.Shared)
			{
				Runner.SetIsSimulated(Object, true);
			}
		}

		public override void FixedUpdateNetwork()
		{
			_time += Runner.DeltaTime * _speed;
			_time = _loop == true ? _time % _animation.length : Mathf.Min(_time, _animation.length);

			_animation.SampleAnimation(gameObject, _time);
			_rigidbody.position = transform.position;
			_rigidbody.rotation = transform.rotation;

			StoreTransform();
		}

		public override void Render()
		{
			if (Runner.GameMode == GameMode.Shared && IsProxy == true)
			{
				InterpolateSharedTransform();
				return;
			}

			float time = _time + Runner.DeltaTime * Runner.LocalAlpha * _speed;
			time = _loop == true ? time % _animation.length : Mathf.Min(time, _animation.length);

			_animation.SampleAnimation(gameObject, time);
			_rigidbody.position = transform.position;
			_rigidbody.rotation = transform.rotation;
		}

		// IAfterClientPredictionReset INTERFACE

		void IAfterClientPredictionReset.AfterClientPredictionReset()
		{
			RestoreTransform();
		}

		// IBeforeAllTicks INTERFACE

		void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
		{
			// Skip resimulation, the state is already restored from AfterClientPredictionReset().
			if (resimulation == true)
				return;

			// Restore state only if a render update was executed previous frame.
			// Otherwise we continue with state from previous fixed tick or the state is already restored from AfterClientPredictionReset().
			int previousFrame = Time.frameCount - 1;
			if (previousFrame != _lastRenderFrame)
				return;

			RestoreTransform();
		}

		// IAfterTick INTERFACE

		void IAfterTick.AfterTick()
		{
			if (Runner.GameMode == GameMode.Shared && HasStateAuthority == false)
				return;

			StoreTransform();
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_transform = transform;
			_rigidbody = GetComponent<Rigidbody>();

			if (_rigidbody == null)
				throw new NullReferenceException($"GameObject {name} has missing Rigidbody component!");

			_rigidbody.isKinematic   = true;
			_rigidbody.useGravity    = false;
			_rigidbody.interpolation = RigidbodyInterpolation.None;
			_rigidbody.constraints   = RigidbodyConstraints.FreezeAll;
		}

		// PRIVATE METHODS

		private void InterpolateSharedTransform()
		{
			if (TryGetSnapshotsBuffers(out NetworkBehaviourBuffer from, out NetworkBehaviourBuffer to, out float alpha) == false)
				return;

			(Vector3, Vector3) positions = GetPropertyReader<Vector3>(nameof(_position)).Read(from, to);
			Vector3 position = Vector3.Lerp(positions.Item1, positions.Item2, alpha);

			(Quaternion, Quaternion) rotations = GetPropertyReader<Quaternion>(nameof(_rotation)).Read(from, to);
			Quaternion rotation = Quaternion.Slerp(rotations.Item1, rotations.Item2, alpha);

			_transform.SetPositionAndRotation(position, rotation);

			_rigidbody.position = position;
			_rigidbody.rotation = rotation;
		}

		// IMapStatusProvider INTERFACE

		bool IMapStatusProvider.IsActive(PlayerRef player)
		{
			return true;
		}

		string IMapStatusProvider.GetStatus(PlayerRef player)
		{
			return $"{name} - {Mathf.RoundToInt(_time / _animation.length * 100.0f)}%";
		}

		// PRIVATE METHODS

		private void StoreTransform()
		{
			_transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

			// Set full-precision properties.
			_position = position;
			_rotation = rotation;

			// Set position and rotation of NetworkTRSP (compressed).
			State.Position = position;
			State.Rotation = rotation;
		}

		private void RestoreTransform()
		{
			_transform.SetPositionAndRotation(_position, _rotation);
			_rigidbody.position = _position;
			_rigidbody.rotation = _rotation;
		}
	}
}
