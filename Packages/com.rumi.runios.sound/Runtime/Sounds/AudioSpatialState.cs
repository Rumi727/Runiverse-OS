#nullable enable
namespace RuniOS.Sounds
{
    /// <summary>
    /// Stores the position and velocity used for 3D spatial audio processing.<br/>
    /// 3D 공간 오디오 처리에 사용할 위치와 속도를 저장합니다.
    /// </summary>
    public struct AudioSpatialState(Vector3 position, Vector3 velocity, Quaternion rotation)
    {
        public AudioSpatialState(Vector3 position) : this(position, Vector3.zero) { }

        public AudioSpatialState(Vector3 position, Vector3 velocity) : this(position, velocity, Quaternion.identity) { }

        public AudioSpatialState(Vector3 position, Vector3 velocity, Vector3 forward, Vector3 up) : this(position, velocity, Quaternion.LookRotation(forward, up)) { }

        public AudioSpatialState(Transform transform) : this(transform.position, Vector3.zero, transform.rotation) { }

        public AudioSpatialState(Transform transform, Vector3 velocity) : this(transform.position, velocity, transform.rotation) { }

        /// <summary>
        /// The source position in world space.<br/>
        /// 월드 공간에서의 소스 위치입니다.
        /// </summary>
        public Vector3 position = position;

        /// <summary>
        /// The source velocity in world units per second.<br/>
        /// 초당 월드 단위로 나타낸 소스 속도입니다.
        /// </summary>
        public Vector3 velocity = velocity;

        public Quaternion rotation = rotation;
    }
}
