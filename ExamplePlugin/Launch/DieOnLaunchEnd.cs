using RoR2;
using UnityEngine;

namespace HedgehogUtils.Launch
{
    public class DieOnLaunchEnd : MonoBehaviour
    {
        private VehicleSeat launchVehicle;
        private CharacterBody body;
        private bool die;
        private void Start()
        {
            body = GetComponent<CharacterBody>();
            if (body)
            {
                launchVehicle = body.currentVehicle;
                if (launchVehicle)
                {
                    launchVehicle.onPassengerExit += OnLaunchEnd;
                    return;
                }
            }
            Destroy(this);
        }

        private void OnLaunchEnd(GameObject self)
        {
            if (body.healthComponent && !body.healthComponent.alive)
            {
                die = true;
            }
            else
            {
                Destroy(this);
            }
        }

        private void FixedUpdate()
        {
            if (!body)
            {
                Destroy(this);
            }
            if (die && !body.HasBuff(Buffs.launchedBuff))
            {
                CharacterDeathBehavior death = body.gameObject.GetComponent<CharacterDeathBehavior>();
                Destroy(this);
                if (death)
                {
                    death.OnDeath();
                }
                die = false;
            }
        }

        private void OnDestroy()
        {
            if (launchVehicle)
            {
                launchVehicle.onPassengerExit -= OnLaunchEnd;
            }
        }
    }
}