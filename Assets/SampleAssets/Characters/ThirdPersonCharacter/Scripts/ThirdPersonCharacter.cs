using System;
using System.Collections;
using UnityEngine;

namespace UnitySampleAssets.Characters.ThirdPerson
{
    public class ThirdPersonCharacter : MonoBehaviour
    {
        [Serializable]
        public class AdvancedSettings
        {
            public float stationaryTurnSpeed = 180;     // additional turn speed added when the player is stationary (added to animation root rotation)
            public float movingTurnSpeed = 360;         // additional turn speed added when the player is moving (added to animation root rotation)
            public float headLookResponseSpeed = 2;     // speed at which head look follows its target
            public float crouchHeightFactor = 0.6f;     // Collider height is multiplied by this when crouching
            public float crouchChangeSpeed = 4;         // speed at which capsule changes height when crouching/standing
            public float autoTurnThresholdAngle = 100;  // character auto turns towards camera direction if facing away by more than this angle
            public float autoTurnSpeed = 2;             // speed at which character auto-turns towards cam direction
            public float jumpRepeatDelayTime = 0.25f;   // amount of time that must elapse between landing and being able to jump again
            public float runCycleLegOffset = 0.2f;      // animation cycle offset (0-1) used for determining correct leg to jump off
            public float groundStickyEffect = 5f;       // power of 'stick to ground' effect - prevents bumping down slopes.
        }


        public Transform lookTarget { get; set; }                                   // The point where the character will be looking at
        public LayerMask groundCheckMask;
        public LayerMask crouchCheckMask;
        public float lookBlendTime;
        public float lookWeight;
        [SerializeField] private float m_JumpPower = 12;                              // determines the jump force applied when jumping (and therefore the jump height)
        [SerializeField] private float m_AirSpeed = 6;                                // determines the max speed of the character while airborne
        [SerializeField] private float m_AirControl = 2;                              // determines the response speed of controlling the character while airborne
        [Range(1, 4)] [SerializeField] private float m_GravityMultiplier = 2;         // gravity modifier - often higher than natural gravity feels right for game characters
        [SerializeField] [Range(0.1f, 3f)] private float m_MoveSpeedMultiplier = 1;   // how much the move speed of the character will be multiplied by
        [SerializeField] [Range(0.1f, 3f)] private float m_AnimSpeedMultiplier = 1;   // how much the animation of the character will be multiplied by
        [SerializeField] private AdvancedSettings m_AdvancedSettings;                 // Container for the advanced settings class , thiss allows the advanced settings to be in a foldout in the inspector


        private bool m_OnGround;              // Is the character on the ground
        private Vector3 m_CurrentLookPos;     // The current position where the character is looking
        private float m_OriginalHeight;       // Used for tracking the original height of the characters capsule Collider
        private Animator m_Animator;          // The animator for the character
        private float m_LastAirTime;          // Used for checking when the character was last in the air for controlling jumps
        private CapsuleCollider m_Capsule;    // The Collider for the character
        private const float k_Half = 0.5f;
        private Vector3 m_MoveInput;
        private bool m_CrouchInput;
        private bool m_JumpInput;
        private float m_TurnAmount;
        private float m_ForwardAmount;
        private Vector3 m_Velocity;
        private IComparer m_RayHitComparer;
        private Rigidbody m_Rigidbody;


        private void Start()
        {
            m_Animator = GetComponentInChildren<Animator>();
            m_Capsule = GetComponent<Collider>() as CapsuleCollider;
            m_Rigidbody = GetComponent<Rigidbody>();

            // as can return null so we need to make sure thats its not before assigning to it
            if (m_Capsule == null)
            {
                Debug.LogError(" Collider cannot be cast to CapsuleCollider");
            }
            else
            {
                m_OriginalHeight = m_Capsule.height;
                m_Capsule.center = Vector3.up*m_OriginalHeight*k_Half;
            }

            m_RayHitComparer = new RayHitComparer();

            SetUpAnimator();

            // give the look position a default in case the character is not under control
            m_CurrentLookPos = Camera.main.transform.position;
        }


        IEnumerator BlendLookWeight()
        {
            float t = 0f;
            while (t < lookBlendTime)
            {
                lookWeight = t / lookBlendTime;
                t += Time.deltaTime;
                yield return null;
            }
            lookWeight = 1f;
        }


        void OnEnable()
        {
            if (Mathf.Abs(lookWeight) < float.Epsilon)
            {
                StartCoroutine(BlendLookWeight());
            }
        }


        // The Move function is designed to be called from a separate component
        // based on User input, or an AI control script
        public void Move(Vector3 move, bool crouch, bool jump, Vector3 lookPos)
        {
            if (move.magnitude > 1)
            {
                move.Normalize();
            }

            // transfer input parameters to member variables.
            m_MoveInput = move;
            m_CrouchInput = crouch;
            m_JumpInput = jump;
            m_CurrentLookPos = lookPos;

            // grab current velocity, we will be changing it.
            m_Velocity = m_Rigidbody.velocity;

            // converts the relative move vector into local turn & fwd values
            ConvertMoveInput();

            // makes the character face the way the camera is looking
            TurnTowardsCameraForward();

            // so the character's head doesn't penetrate a low ceiling
            PreventStandingInLowHeadroom();

            // so you can fit under low areas when crouching
            ScaleCapsuleForCrouching();

            // this is in addition to root rotation in the animations
            ApplyExtraTurnRotation();

            // detect and stick to ground
            GroundCheck();

            // use low or high friction values depending on the current state
            SetFriction(); 

            // control and velocity handling is different when grounded and airborne:
            if (m_OnGround)
            {
                HandleGroundedVelocities();
            }
            else
            {
                HandleAirborneVelocities();
            }

            // send input and other state parameters to the animator
            UpdateAnimator(); 

            // reassign velocity, since it will have been modified by the above functions.
            if (!m_Rigidbody.IsSleeping())
            {
                m_Rigidbody.velocity = m_Velocity;
            }
        }


        private void ConvertMoveInput()
        {
            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            Vector3 localMove = transform.InverseTransformDirection(m_MoveInput);
            m_TurnAmount = Mathf.Atan2(localMove.x, localMove.z);
            m_ForwardAmount = localMove.z;
        }


        private void TurnTowardsCameraForward()
        {
            // automatically turn to face camera direction,
            // when not moving, and beyond the specified angle threshold
            if (Mathf.Abs(m_ForwardAmount) < .01f)
            {
                Vector3 lookDelta = transform.InverseTransformDirection(m_CurrentLookPos - transform.position);
                float lookAngle = Mathf.Atan2(lookDelta.x, lookDelta.z)*Mathf.Rad2Deg;

                // are we beyond the threshold of where need to turn to face the camera?
                if (Mathf.Abs(lookAngle) > m_AdvancedSettings.autoTurnThresholdAngle)
                {
                    m_TurnAmount += lookAngle*m_AdvancedSettings.autoTurnSpeed*.001f;
                }
            }
        }


        private void PreventStandingInLowHeadroom()
        {
            // prevent standing up in crouch-only zones
            if (!m_CrouchInput)
            {
                Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up*m_Capsule.radius*k_Half, Vector3.up);
                float crouchRayLength = m_OriginalHeight - m_Capsule.radius*k_Half;
                if (Physics.SphereCast(crouchRay, m_Capsule.radius*k_Half, crouchRayLength, crouchCheckMask))
                {
                    m_CrouchInput = true;
                }
            }
        }


        private void ScaleCapsuleForCrouching()
        {
            // scale the capsule Collider according to
            // if crouching ...
            if (m_OnGround && m_CrouchInput && (Mathf.Abs(m_Capsule.height - m_OriginalHeight*m_AdvancedSettings.crouchHeightFactor) > float.Epsilon))
            {
                m_Capsule.height = Mathf.MoveTowards(m_Capsule.height, m_OriginalHeight*m_AdvancedSettings.crouchHeightFactor,
                                                   Time.deltaTime*4);
                m_Capsule.center = Vector3.MoveTowards(m_Capsule.center,
                                                     Vector3.up*m_OriginalHeight*m_AdvancedSettings.crouchHeightFactor*k_Half,
                                                     Time.deltaTime*2);
            }
                // ... everything else
            else if (Mathf.Abs(m_Capsule.height - m_OriginalHeight) > float.Epsilon && m_Capsule.center != Vector3.up*m_OriginalHeight*k_Half)
            {
                m_Capsule.height = Mathf.MoveTowards(m_Capsule.height, m_OriginalHeight, Time.deltaTime*4);
                m_Capsule.center = Vector3.MoveTowards(m_Capsule.center, Vector3.up*m_OriginalHeight*k_Half, Time.deltaTime*2);
            }
        }


        private void ApplyExtraTurnRotation()
        {
            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(m_AdvancedSettings.stationaryTurnSpeed, m_AdvancedSettings.movingTurnSpeed,
                                         m_ForwardAmount);
            transform.Rotate(0, m_TurnAmount*turnSpeed*Time.deltaTime, 0);
        }


        private void GroundCheck()
        {
            Ray ray = new Ray(transform.position + Vector3.up*.1f, -Vector3.up);
            var hits = Physics.RaycastAll(ray, .2f , groundCheckMask);
            Array.Sort(hits, m_RayHitComparer);

            if (m_Velocity.y < m_JumpPower*.5f)
            {
                m_OnGround = false;
                m_Rigidbody.useGravity = true;
                foreach (var hit in hits)
                {
                    // check whether we hit a non-trigger Collider (and not the character itself)
                    if (!hit.collider.isTrigger)
                    {
                        // this counts as being on ground.

                        // stick to surface - helps character stick to ground - specially when running down slopes
                        if (m_Velocity.y <= 0)
                        {
                            m_Rigidbody.position = Vector3.MoveTowards(m_Rigidbody.position, hit.point,
                                                                     Time.deltaTime*m_AdvancedSettings.groundStickyEffect);
                        }

                        m_OnGround = true;
                        m_Rigidbody.useGravity = false;
                        break;
                    }
                }
            }

            // remember when we were last in air, for jump delay
            if (!m_OnGround)
            {
                m_LastAirTime = Time.time;
            }
        }


        private void SetFriction()
        {
            if (m_OnGround && Mathf.Abs(m_MoveInput.magnitude) < float.Epsilon && !m_JumpInput)
            {
                m_Rigidbody.Sleep();
            }
        }


        private void HandleGroundedVelocities()
        {
            m_Velocity.y = 0;

            if (Mathf.Abs(m_MoveInput.magnitude) < float.Epsilon)
            {
                // when not moving this prevents sliding on slopes:
                m_Velocity.x = 0;
                m_Velocity.z = 0;
            }
            // check whether conditions are right to allow a jump:
            bool animationGrounded = m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded");
            bool okToRepeatJump = Time.time > m_LastAirTime + m_AdvancedSettings.jumpRepeatDelayTime;

            if (m_JumpInput && !m_CrouchInput && okToRepeatJump && animationGrounded)
            {
                // jump!
                m_OnGround = false;
                m_Velocity = m_MoveInput*m_AirSpeed;
                m_Velocity.y = m_JumpPower;
            }
        }


        private void HandleAirborneVelocities()
        {
            // we allow some movement in air, but it's very different to when on ground
            // (typically allowing a small change in trajectory)
            Vector3 airMove = new Vector3(m_MoveInput.x*m_AirSpeed, m_Velocity.y, m_MoveInput.z*m_AirSpeed);
            m_Velocity = Vector3.Lerp(m_Velocity, airMove, Time.deltaTime*m_AirControl);
            m_Rigidbody.useGravity = true;

            // apply extra gravity from multiplier:
            Vector3 extraGravityForce = (Physics.gravity*m_GravityMultiplier) - Physics.gravity;
            m_Rigidbody.AddForce(extraGravityForce);
        }


        private void UpdateAnimator()
        {
            // Here we tell the animator what to do based on the current states and inputs.

            // only use root motion when on ground:
            m_Animator.applyRootMotion = m_OnGround;

            // update the animator parameters
            m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
            m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
            m_Animator.SetBool("Crouch", m_CrouchInput);
            m_Animator.SetBool("OnGround", m_OnGround);
            if (!m_OnGround)
            {
                m_Animator.SetFloat("Jump", m_Velocity.y);
            }

            // calculate which leg is behind, so as to leave that leg trailing in the jump animation
            // (This code is reliant on the specific run cycle offset in our animations,
            // and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
            float runCycle =
                Mathf.Repeat(
                    m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_AdvancedSettings.runCycleLegOffset, 1);
            float jumpLeg = (runCycle < k_Half ? 1 : -1)*m_ForwardAmount;
            if (m_OnGround)
            {
                m_Animator.SetFloat("JumpLeg", jumpLeg);
            }

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
            // which affects the movement speed because of the root motion.
            if (m_OnGround && m_MoveInput.magnitude > 0)
            {
                m_Animator.speed = m_AnimSpeedMultiplier;
            }
            else
            {
                // but we don't want to use that while airborne
                m_Animator.speed = 1;
            }
        }


        private void OnAnimatorIK(int layerIndex)
        {
            // we set the weight so most of the look-turn is done with the head, not the body.
            m_Animator.SetLookAtWeight(lookWeight, 0.2f, 2.5f);

            // if a transform is assigned as a look target, it overrides the vector lookPos value
            if (lookTarget != null)
            {
                m_CurrentLookPos = lookTarget.position;
            }

            // Used for the head look feature.
            m_Animator.SetLookAtPosition(m_CurrentLookPos);
        }


        private void SetUpAnimator()
        {
            // this is a ref to the animator component on the root.
            m_Animator = GetComponent<Animator>();

            // we use avatar from a child animator component if present
            // (this is to enable easy swapping of the character model as a child node)
            foreach (var childAnimator in GetComponentsInChildren<Animator>())
            {
                if (childAnimator != m_Animator)
                {
                    m_Animator.avatar = childAnimator.avatar;
                    Destroy(childAnimator);
                    break;
                }
            }
        }


        public void OnAnimatorMove()
        {
            // we implement this function to override the default root motion.
            // this allows us to modify the positional speed before it's applied.
            if (m_OnGround && Time.deltaTime > 0)
            {
                Vector3 v = (m_Animator.deltaPosition*m_MoveSpeedMultiplier)/Time.deltaTime;

                // we preserve the existing y part of the current velocity.
                v.y = m_Rigidbody.velocity.y;
                m_Rigidbody.velocity = v;
            }
        }


        void OnDisable()
        {
            lookWeight = 0f;
        }

        //used for comparing distances
        private class RayHitComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                return ((RaycastHit) x).distance.CompareTo(((RaycastHit) y).distance);
            }
        }
    }
}
