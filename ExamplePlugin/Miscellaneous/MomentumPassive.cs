using EntityStates;
using R2API;
using RoR2;
using RoR2.Audio;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HedgehogUtils.Miscellaneous
{
    public class MomentumPassive : MonoBehaviour
    {
        // Needs interaction for landing on a slope

        private CharacterBody body;
        private EntityStateMachine bodyStateMachine;
        
        private ICharacterFlightParameterProvider flight;

        public float momentum=0; // from -1 to 1

        private float desiredMomentum=0;
        private bool calced = false;

        private Vector3 prevVelocity = Vector3.zero;
        private static float cutoffAngle = 30f;

        private int frame= 0;
        private static int framesBetweenRecalc = 20;

        public virtual float maxSpeedMultiplier 
        {
            get { return 1f; }
        }
        public virtual float minSpeedMultiplier
        {
            get { return 1f / 3f; }
        }

        private static float aerialDecay = 0.2f;
        private static float groundDecay = 0.5f;
        private static float fastDecay = 0.8f;
        private static float slowDecay = 1.35f;

        private void Start()
        {
            this.body = GetComponent<CharacterBody>();
            this.bodyStateMachine = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
            this.flight = body.GetComponent<ICharacterFlightParameterProvider>();
        }

        private void FixedUpdate()
        {
            frame = (frame + 1) % framesBetweenRecalc;
            if (frame == 0)
            {
                CalculateMomentum();
            }
        }

        private void CalculateMomentum()
        {
            // Flying (Thank you Starstorm 2 Back Thrusters for inspiration)
            if (flight!=null && flight.isFlying)
            {
                if (body.characterMotor.velocity != Vector3.zero && (typeof(GenericCharacterMain).IsAssignableFrom(bodyStateMachine.state.GetType())))
                {
                    this.calced = false;
                    Vector3 velocity = Vector3.Normalize(body.characterMotor.velocity);
                    Vector3 prevVelocity = Vector3.Normalize(this.prevVelocity);
                    if (body.characterMotor.isGrounded)
                    {
                        velocity.y = 0;
                        prevVelocity.y = 0;
                        Vector3.Normalize(velocity);
                        Vector3.Normalize(prevVelocity);
                    }
                    if (body.inputBank.moveVector!=Vector3.zero)
                    {
                        desiredMomentum = Mathf.Lerp(1, -0.8f, Vector3.Angle(velocity, prevVelocity)/cutoffAngle);
                    }
                    else
                    {
                        desiredMomentum = 0f;
                    }
                    //Chat.AddMessage((Vector3.Angle(velocity, prevVelocity) / cutoffAngle).ToString());
                    MomentumCalculation(1.2f, 0.4f);
                }
                else
                {
                    momentum = 0;
                    if (!calced)
                    {
                        calced = true;
                        body.MarkAllStatsDirty();
                        //body.RecalculateStats();
                    }
                }
                this.prevVelocity = body.characterMotor.velocity;
                return;
            }
            


            // Not Flying
            if (body.characterMotor.velocity != Vector3.zero && body.characterMotor.isGrounded && (typeof(GenericCharacterMain).IsAssignableFrom(bodyStateMachine.state.GetType())))
            {
                calced = false;
                Vector3 forward = VelocityOnGround(body.characterMotor.velocity); //body.characterMotor.moveDirection.normalized;
                float dot = Vector3.Dot(forward, Vector3.down);
                desiredMomentum = Mathf.Clamp(dot * 2f, -1f, 1f);
                MomentumCalculation(slowDecay, fastDecay);
            }
            else
            {
                desiredMomentum = 0;
                float momentumDecay = body.characterMotor.isGrounded ? groundDecay : aerialDecay;
                MomentumCalculation(momentumDecay, momentumDecay);
            }
        }

        private Vector3 VelocityOnGround(Vector3 velocity)
        {
            velocity.y = 0;
            return Vector3.ProjectOnPlane(velocity, body.characterMotor.estimatedGroundNormal).normalized;
        }

        private void MomentumCalculation(float slowDecay, float fastDecay)
        {
            if (Mathf.Abs(desiredMomentum - momentum) > 0.1f)
            {
                if (desiredMomentum > momentum)
                {
                    momentum = Mathf.Clamp(momentum + ((desiredMomentum - momentum) * Time.fixedDeltaTime * slowDecay * framesBetweenRecalc), -1f, desiredMomentum);
                }
                else
                {
                    momentum = Mathf.Clamp(momentum + ((desiredMomentum - momentum) * Time.fixedDeltaTime * fastDecay * framesBetweenRecalc), desiredMomentum, 1f);
                }
                body.MarkAllStatsDirty();
                //Chat.AddMessage("mom " + momentum.ToString() + " des " + desiredMomentum.ToString() + " dot " + dot.ToString());
            }
            else
            {
                momentum = desiredMomentum;
                if (!calced)
                {
                    calced = true;
                    body.MarkAllStatsDirty();
                }
            }
        }
    }
}