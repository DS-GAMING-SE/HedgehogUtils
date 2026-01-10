using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Audio;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace HedgehogUtils.Voicelines
{
    public class NetworkVoiceline : INetMessage
    {
        NetworkedVoiceline networkedVoiceline;
        public NetworkVoiceline() { }
        public NetworkVoiceline(NetworkInstanceId netId, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority priority) 
        { 
            networkedVoiceline = default(NetworkedVoiceline); 
            networkedVoiceline.netId = netId;
            networkedVoiceline.soundIndex = networkSoundEventIndex;
            networkedVoiceline.priority = priority; 
        }
        public void OnReceived()
        {
            GameObject body = Util.FindNetworkObject(networkedVoiceline.netId);
            if (!body) { return; }
            if (body.TryGetComponent<VoicelineComponent>(out var voiceline) && Stage.instance)
            {
                voiceline.PlayVoiceline(networkedVoiceline.soundIndex, networkedVoiceline.priority);
            }

        }
        public void Serialize(NetworkWriter writer) { writer.WriteVoiceline(networkedVoiceline); }
        public void Deserialize(NetworkReader reader) { networkedVoiceline = reader.ReadVoiceline(); }
    }
    public static class Extensions
    {
        public static void WriteVoiceline(this NetworkWriter writer, NetworkInstanceId netId, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority priority)
        {
            writer.Write(netId);
            writer.WriteNetworkSoundEventIndex(networkSoundEventIndex);
            writer.Write((byte)priority);
        }
        public static void WriteVoiceline(this NetworkWriter writer, NetworkedVoiceline networkedVoiceline)
        {
            writer.WriteVoiceline(networkedVoiceline.netId, networkedVoiceline.soundIndex, networkedVoiceline.priority);
        }

        public static NetworkedVoiceline ReadVoiceline(this NetworkReader reader)
        {
            NetworkedVoiceline networkedVoiceline = default(NetworkedVoiceline);
            networkedVoiceline.netId = reader.ReadNetworkId();
            networkedVoiceline.soundIndex = reader.ReadNetworkSoundEventIndex();
            networkedVoiceline.priority = (VoicelinePriority)reader.ReadByte();
            return networkedVoiceline;
        }
    }
}
