using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace HedgehogUtils.Forms
{
    public class NetworkTransformation : INetMessage
    {
        NetworkInstanceId netId;

        FormIndex formIndex;

        public NetworkTransformation()
        {

        }

        public NetworkTransformation(NetworkInstanceId netId, FormIndex formIndex)
        {
            this.netId = netId;
            this.formIndex = formIndex;
        }

        public void OnReceived()
        {
            GameObject body = Util.FindNetworkObject(netId);
            if (!body) { return; }
            if (Forms.formHandlers[(int)formIndex])
            {
                Forms.formHandlers[(int)formIndex].OnTransform(body.GetComponent<FormComponent>());
            }

        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(netId);
            writer.Write(formIndex);
        }

        public void Deserialize(NetworkReader reader)
        {
            netId = reader.ReadNetworkId();
            formIndex = reader.ReadFormIndex();
        }
    }

    public static class Extensions
    {
        public static void Write(this NetworkWriter writer, FormIndex formIndex)
        {
            writer.WritePackedIndex32((int)formIndex);
        }

        public static FormIndex ReadFormIndex(this NetworkReader reader)
        {
            return (FormIndex)reader.ReadPackedIndex32();
        }
    }
}
