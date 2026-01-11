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
        public NetworkVoiceline(VoicelineComponent voicelineComponent, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority priority)
        {
            networkedVoiceline = default(NetworkedVoiceline);
            networkedVoiceline.voicelineComponent = voicelineComponent;
            networkedVoiceline.soundIndex = networkSoundEventIndex;
            networkedVoiceline.priority = priority;
        }
        public NetworkVoiceline(GameObject gameObject, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority priority) 
        { 
            networkedVoiceline = default(NetworkedVoiceline); 
            networkedVoiceline.voicelineComponent = gameObject.GetComponent<VoicelineComponent>();
            networkedVoiceline.soundIndex = networkSoundEventIndex;
            networkedVoiceline.priority = priority; 
        }
        public NetworkVoiceline(NetworkedVoiceline networkedVoiceline)
        {
            this.networkedVoiceline = networkedVoiceline;
        }
        public void OnReceived()
        {
            if (!networkedVoiceline.IsValid()) { return; }
            networkedVoiceline.voicelineComponent.PlayVoiceline(networkedVoiceline.soundIndex, networkedVoiceline.priority);

        }
        public void Serialize(NetworkWriter writer) { writer.WriteVoiceline(networkedVoiceline); }
        public void Deserialize(NetworkReader reader) { networkedVoiceline = reader.ReadVoiceline(); }
    }
    public static class Extensions
    {
        public static void WriteVoiceline(this NetworkWriter writer, VoicelineComponent voicelineComponent, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority priority)
        {
            writer.Write(voicelineComponent.gameObject);
            writer.WriteNetworkSoundEventIndex(networkSoundEventIndex);
            writer.Write((byte)priority);
        }
        public static void WriteVoiceline(this NetworkWriter writer, GameObject gameObject, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority priority)
        {
            writer.Write(gameObject);
            writer.WriteNetworkSoundEventIndex(networkSoundEventIndex);
            writer.Write((byte)priority);
        }
        public static void WriteVoiceline(this NetworkWriter writer, NetworkedVoiceline networkedVoiceline)
        {
            writer.WriteVoiceline(networkedVoiceline.voicelineComponent.gameObject, networkedVoiceline.soundIndex, networkedVoiceline.priority);
        }

        public static NetworkedVoiceline ReadVoiceline(this NetworkReader reader)
        {
            NetworkedVoiceline networkedVoiceline = default(NetworkedVoiceline);
            GameObject gameObject = reader.ReadGameObject();
            networkedVoiceline.voicelineComponent = gameObject ? gameObject.GetComponent<VoicelineComponent>() : null;
            networkedVoiceline.soundIndex = reader.ReadNetworkSoundEventIndex();
            networkedVoiceline.priority = (VoicelinePriority)reader.ReadByte();
            return networkedVoiceline;
        }
    }
}
