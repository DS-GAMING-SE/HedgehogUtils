using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Audio;
using RoR2.SurvivorMannequins;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace HedgehogUtils.Voicelines
{
    public struct NetworkedVoiceline
    {
        public SimpleVoicelineComponent voicelineComponent;
        public NetworkSoundEventIndex soundIndex;
        public VoicelinePriority priority;

        public NetworkedVoiceline(SimpleVoicelineComponent voicelineComponent, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority voicelinePriority)
        {
            this.voicelineComponent = voicelineComponent;
            this.soundIndex = networkSoundEventIndex;
            this.priority = voicelinePriority;
        }

        public bool IsValid()
        {
            return soundIndex != NetworkSoundEventIndex.Invalid && voicelineComponent;
        }
    }
    public class NetworkVoiceline : INetMessage
    {
        NetworkedVoiceline networkedVoiceline;
        public NetworkVoiceline() { }
        public NetworkVoiceline(SimpleVoicelineComponent voicelineComponent, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority priority)
        {
            networkedVoiceline = default(NetworkedVoiceline);
            networkedVoiceline.voicelineComponent = voicelineComponent;
            networkedVoiceline.soundIndex = networkSoundEventIndex;
            networkedVoiceline.priority = priority;
        }
        public NetworkVoiceline(GameObject gameObject, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority priority) 
        { 
            networkedVoiceline = default(NetworkedVoiceline); 
            networkedVoiceline.voicelineComponent = gameObject.GetComponent<SimpleVoicelineComponent>();
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
    public class NetworkLobbyVoiceline : INetMessage
    {
        NetworkUser networkuser;
        NetworkSoundEventIndex networkSoundEventIndex;
        public NetworkLobbyVoiceline() { }
        public NetworkLobbyVoiceline(NetworkUser networkUser, NetworkSoundEventIndex networkSoundEventIndex)
        {
            this.networkuser = networkUser;
            this.networkSoundEventIndex = networkSoundEventIndex;
        }
        public void OnReceived()
        {
            if (!networkuser) { return; }
            GameObject diorama = GameObject.Find("SurvivorMannequinDiorama");
            if (diorama && diorama.TryGetComponent<SurvivorMannequinDioramaController>(out var dioramaController))
            {
                for (int i = 0; i < dioramaController.sortedNetworkUsers.Count; i++)
                {
                    if (dioramaController.sortedNetworkUsers[i] == networkuser && dioramaController.mannequinSlots[i] && 
                        dioramaController.mannequinSlots[i].mannequinInstanceTransform && dioramaController.mannequinSlots[i].mannequinInstanceTransform.TryGetComponent<VoicelineDisplayComponent>(out var voicelineComponent))
                    {
                        voicelineComponent.PlayVoiceline(NetworkSoundEventCatalog.GetEventNameFromNetworkIndex(networkSoundEventIndex), VoicelinePriority.PriorityDialogue);
                        return;
                    }
                }
            }

        }
        public void Serialize(NetworkWriter writer) 
        { 
            writer.Write(networkuser.netId);
            writer.WriteNetworkSoundEventIndex(networkSoundEventIndex);
        }
        public void Deserialize(NetworkReader reader) 
        { 
            reader.ReadNetworkIDAndGameObject(out _, out var netUserGameObject);
            networkuser = netUserGameObject.GetComponent<NetworkUser>();
            networkSoundEventIndex = reader.ReadNetworkSoundEventIndex();
        }
    }
    public static class Extensions
    {
        public static void WriteVoiceline(this NetworkWriter writer, SimpleVoicelineComponent voicelineComponent, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority priority)
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
            networkedVoiceline.voicelineComponent = gameObject ? gameObject.GetComponent<SimpleVoicelineComponent>() : null;
            networkedVoiceline.soundIndex = reader.ReadNetworkSoundEventIndex();
            networkedVoiceline.priority = (VoicelinePriority)reader.ReadByte();
            return networkedVoiceline;
        }
    }
}
