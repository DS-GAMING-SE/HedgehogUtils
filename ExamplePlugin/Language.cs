using RoR2;
using System;
using System.Text;
using UnityEngine;
using R2API;

namespace HedgehogUtils
{
    public static class Language
    {
        //maybe go back to what the mod had by default with the separate lang file
        public static void Initialize()
        {
            #region Super Form
            string superSonicColor = "<color=#ffee00>";
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "SUPER_FORM_PREFIX", "Super {0}");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "SUPER_FORM", "Super");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "SUPER_FORM_ANNOUNCE_TEXT", superSonicColor + "<size=110%>{0} has transformed into their {1} form!</color></size>");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "SUPER_FORM_ANNOUNCE_TEXT_2P", superSonicColor + "<size=110%>You transformed into your {1} form!</color></size>");

            #endregion

            #region Emeralds
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "ARTIFACT_CHAOS_EMERALD_NAME", "Artifact of Chaos Emeralds");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "ARTIFACT_CHAOS_EMERALD_DESCRIPTION", "The Chaos Emeralds are scattered across the planet.");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_NAME", "Chaos Emerald");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_CONTEXT", "Receive Emerald");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_INSPECT", "When activated by a survivor the Chaos Emerald will be dropped. Once all seven are collected, survivors can transform into their Super form.");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_TITLE", "Chaos Emerald");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_YELLOW", "Chaos Emerald: Yellow");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_BLUE", "Chaos Emerald: Blue");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_RED", "Chaos Emerald: Red");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_GRAY", "Chaos Emerald: Gray");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_GREEN", "Chaos Emerald: Green");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_CYAN", "Chaos Emerald: Cyan");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_PURPLE", "Chaos Emerald: Purple");
            // Couldn't figure out how to change the tokens at runtime to match the keybind config but ehhhhh whatever
            string chaosEmeraldDesc = $" of the <style=cIsUtility>seven</style> Chaos Emeralds." + Environment.NewLine + $"When all <style=cIsUtility>seven</style> are collected by you and/or other players, press {superSonicColor}V</color> to transform into your {superSonicColor}Super form</color> for {superSonicColor}{Forms.SuperForm.StaticValues.superSonicDuration}</color> seconds. Transforming increases <style=cIsDamage>damage</style> by <style=cIsDamage>+{100f * Forms.SuperForm.StaticValues.superSonicBaseDamage}%</style>. Increases <style=cIsDamage>attack speed</style> by <style=cIsDamage>+{100f * Forms.SuperForm.StaticValues.superSonicAttackSpeed}%</style>. Increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>+{100f * Forms.SuperForm.StaticValues.superSonicMovementSpeed}%</style>. Grants <style=cIsHealing>complete invincibility</style> and <style=cIsUtility>flight</style>. For <style=cIsUtility>Sonic</style>, {superSonicColor}all of his skills are upgraded</color>." + Environment.NewLine + Environment.NewLine + "This will <style=cIsUtility>consume</style> all seven Chaos Emeralds.";
            string chaosEmeraldPickup = $"One out of seven. When all are collected, transform into your Super form by pressing V, granting invincibility, flight, and incredible power for {Forms.SuperForm.StaticValues.superSonicDuration} seconds. Consumed on use.";

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "YELLOW_EMERALD", "Chaos Emerald: <style=cIsDamage>Yellow</style>");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "YELLOW_EMERALD_PICKUP", chaosEmeraldPickup);
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "YELLOW_EMERALD_DESC", $"<style=cIsDamage>One</style>" + chaosEmeraldDesc);

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "BLUE_EMERALD", "Chaos Emerald: <color=#2b44d6>Blue</color>");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "BLUE_EMERALD_PICKUP", chaosEmeraldPickup);
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "BLUE_EMERALD_DESC", $"<color=#2b44d6>One</color>" + chaosEmeraldDesc);

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "RED_EMERALD", "Chaos Emerald: <style=cDeath>Red</style>");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "RED_EMERALD_PICKUP", chaosEmeraldPickup);
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "RED_EMERALD_DESC", $"<style=cDeath>One</style>" + chaosEmeraldDesc);

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "GRAY_EMERALD", "Chaos Emerald: <color=#b8c5d6>Gray</color>");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "GRAY_EMERALD_PICKUP", chaosEmeraldPickup);
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "GRAY_EMERALD_DESC", "<color=#b8c5d6>One</color>" + chaosEmeraldDesc);

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "GREEN_EMERALD", "Chaos Emerald: <style=cIsHealing>Green</style>");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "GREEN_EMERALD_PICKUP", chaosEmeraldPickup);
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "GREEN_EMERALD_DESC", $"<style=cIsHealing>One</style>" + chaosEmeraldDesc);

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "CYAN_EMERALD", "Chaos Emerald: <style=cIsUtility>Cyan</style>");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "CYAN_EMERALD_PICKUP", chaosEmeraldPickup);
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "CYAN_EMERALD_DESC", $"<style=cIsUtility>One</style>" + chaosEmeraldDesc);

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "PURPLE_EMERALD", "Chaos Emerald: <color=#c437c0>Purple</color>");
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "PURPLE_EMERALD_PICKUP", chaosEmeraldPickup);
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "PURPLE_EMERALD_DESC", "<color=#c437c0>One</color>" + chaosEmeraldDesc);

            #region Emerald Lore

            string translationError = "\n\n<style=cMono>Translation Errors:</style>\n";

            int defaultAdjust = 3;
            // The logbooks are transcriptions of information spoken by one of the Ancients (Sonic Frontiers). These texts are stored in Eggman's database
            // The Ancient is telling about the events of them and their people as they happen, starting from leaving their destroyed home planet and ending with The End being sealed in Cyber Space
            // The Ancient is actually a Koco aka a consciousness stored in cyber space that thinks it is currently living through a moment of regret/failure from its past
            // The Chaos Emeralds are always at the core of the events surrounding the Ancients. Maybe htis log isn't focused on the emeralds enough
            // Chaos Emeralds are powered by thoughts and emotions, so lean into emotion and thought
            // Ancients are all about archiving and remembering people. They're driven by emotion, like the emeralds themselves. How can sentimentality play into the Chaos Emeralds?
            // I think The End is a stupid name for the big bad from Sonic Frontiers so I'm making it one of those "translation errors" in the logbook, like how Providence is just called a "hero" or whatever
            // The Ancients is also a stupid name
            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "BLUE_EMERALD_LORE", EggFace( //DONE Leaving home planet after it got destroyed
                "<style=cMono>Welcome to the Egg-Net archives.", defaultAdjust - 13,
                "$ Combing for data relevant to", defaultAdjust,
                "$ \"Ancients and Chaos Emeralds\"...", defaultAdjust, 
                "$ Results found in folder: Starfall", defaultAdjust,
                "$ Translating audio file...", defaultAdjust) + "$ Displaying result [1/7].</style>\n\n" +
                //"Only a fraction of us were able to make it off world before it attacked. It was as if death itself had claimed our homeworld, leaving nothing but smoldering rock where our home planet once was.\n\nThe emeralds powered our engines. It was only with their power that any of us managed to escape.\n\nAll we could do then was move forward into the darkness with only the glimmering light of the emeralds to guide us.");
                "That sight is burned into my memory. The sight of the fiery rift burned straight through the planet's surface. Our entire homeworld crumbled to the sheer power of that entity mere moments after its appearance eclipsed our sun. I'm one of the few that made it off the planet before the attack.\n\nSeven Chaos Emeralds divided amongst our ships provided enough power for us to escape, flying blindly into outer space. The Chaos Emeralds continuously produce energy. One emerald is enough to power a small fleet of ships, all flying fast enough to outrun that thing that destroyed our world. With the power of the emeralds, we could flee indefinitely.\n\nWe could continue furthering the gap between us such that it could never reach us again. We could keep on running. Never turn back.\n\nWho am I kidding? Running endlessly would be unbearable. Who could possibly live like that forever?");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "CYAN_EMERALD_LORE", EggFace( //DONE Arriving on Earth, clearly show how they don't want to hurt people
                "<style=cMono>Welcome to the Egg-Net archives.", defaultAdjust - 13,
                "$ Combing for data relevant to", defaultAdjust,
                "$ \"Ancients and Chaos Emeralds\"...", defaultAdjust,
                "$ Results found in folder: Starfall", defaultAdjust,
                "$ Translating audio file...", defaultAdjust) + "$ Displaying result [2/7].</style>\n\n" +
                //"There's far more to these gems than we know about. It couldn't have just been random chance that drew us to this world. The emeralds reacted to something.. no.. something took control of the emeralds, and by extension, our ships. Whatever it is, it's connected to the emeralds in some way. In the end, it doesn't really matter why it brought us here anyway. I had long since gotten used to the chaos.\n\nThe world that strange force had brought us to was a primitive one, many millenia behind us. We chose to isolate ourselves on an uninhabited archipelago to avoid interfering too much with this world's inhabitants. With our numbers so slim, these islands had plenty of room for us. Besides, we are no conquerors.\n\nNo one should have their home taken away from them.");
                "There's far more to the Chaos Emeralds than we know about. It couldn't have just been random chance that drew us to this world. The emeralds reacted to something.. no.. something took control of the emeralds, and by extension, our ships. We were forced to land on this planet by that force.\n\nWe landed not far from its source, a lone emerald much like our Chaos Emeralds, but far larger. Fascinating how things from different worlds have such a deep connection to eachother. This connection needs to be studied further, I believe it can lead to a breakthrough in our understanding of these emeralds. Imagine what miracles could come from further harnessing the emeralds' power.\n\nSince our arrival on this planet, we have been scanning this planet. With this, we'll know everything we need to know about this planet, including information on all of its native life.\n\nThe initial results of our scans show no signs of developed civilizations. We will continue gathering data as time goes on, but these initial results paint a clear picture of what this planet's native inhabitants are like. This world is primitive, many millenia behind us. Hypothetically speaking, there would be nothing on this planet that could resist an incursion from us.\n\nWe will isolate ourselves on this uninhabited archipelago to avoid interfering too much with this world's life. With our numbers so slim, these islands have plenty of room for us. Besides, we are no conquerors.\n\nNo one should have their home taken away from them.");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "GRAY_EMERALD_LORE", EggFace( //DONE Context for Cyber Space and the infinite memory loop that hte koco get trapped in in Sonic Frontiers
                "<style=cMono>Welcome to the Egg-Net archives.", defaultAdjust - 13,
                "$ Combing for data relevant to", defaultAdjust,
                "$ \"Ancients and Chaos Emeralds\"...", defaultAdjust,
                "$ Results found in folder: Starfall", defaultAdjust,
                "$ Translating audio file...", defaultAdjust) + "$ Displaying result [3/7].</style>\n\n" +
                "Thoughts are power. The power of the Chaos Emeralds is closely tied to the user's thoughts. Our hearts enrich the power of the Chaos Emeralds, which in turn, grant us protection in our dire moments. To preserve our thoughts is to preserve the power to keep moving. To preserve our past is to preserve our future. That is the foundation on which Cyber Space was created.\n\nCyber Space is a dimension which stored information on everything about us. Our history, our memories, our desires. Every aspect of us as individuals can be uploaded and stored in Cyber Space at any time. The digital copies of us can think, feel, and speak just as their true selves can. It's too late for those we lost from our homeworld. But with this, we can \"save people\". We will never truly lose someone ever again.\n\nMaybe that's a bit too optimistic. As much I want to truly be able to save people, I can't ignore that the copies stored in Cyber Space are not perfect recreations of their true selves.\n\nStrange results have repeatedly appeared in stress tests of the system. When left unchecked for too long, the functioning minds of people within Cyber Space tend to trap themselves within a loop. Memories with strong emotions tied to them, especially those of regret, fester until they gradually take over the mind's ability to process information. Their perception of time and the world around them start to degrade, confusing past with present.\n\nThese issues can be managed with maintenance, but the existence of these problems is concerning. Is it a flaw in Cyber Space's data storage? Or, is the problem not with Cyber Space, but something that originates from much deeper within the mind? Memory truly is a finicky thing.");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "GREEN_EMERALD_LORE", EggFace( //DONE The End returns to destroy the planet
                "<style=cMono>Welcome to the Egg-Net archives.", defaultAdjust -13,
                "$ Combing for data relevant to", defaultAdjust,
                "$ \"Ancients and Chaos Emeralds\"...", defaultAdjust,
                "$ Results found in folder: Starfall", defaultAdjust,
                "$ Translating audio file...", defaultAdjust) + "$ Displaying result [4/7].</style>\n\n" +
                //"[The End] is what we began to call the entity that destroyed our home. Call it pessimism, but it's only natural most of us would still be shaken by the memory of what it did to us.\n\n-----\n\nBefore, we had run away and lost almost everything. Now, not only was what little we had left in danger once more, we had also dragged a planet that's not our own into this conflict.\n\nWe could've run again, we could've tried to hide on another world, we could've left this world to die like ours.\n\nHow much would we lose in our rushed and desperate escape? Was there any guarantee it wouldn't find us again? How many more worlds would be in danger from this... thing?\n\nWe could've run away.\n\nWe didn't.\n\nWith the few days we have until its arrival, we will prepare. When [The End] reaches us, we will be ready. With the power of the Chaos Emeralds, we'll stand before its unthinkable might and save this world." + translationError + "# [The End] could not be fully translated.");
                "Sorry for the danger we've brought upon this world.\n\nThat entity has been detected quickly approaching this planet. It must've followed us after we escaped its last attack. I had thought it to be a mindless destructive force, but these actions imply a level of intelligence and determination. Running away will only delay its inevitable return while putting more lives in harm's way.\n\nThe Chaos Emeralds bring our thoughts to life. Thoughts of fear had manifested as the speed that let us escape its previous attack.\n\nLet the emeralds sense that there are no longer thoughts of cowardice among our warriors.\n\nLet them sense our resolve to protect this planet we now call home.\n\nLet our hearts draw out their true power. Only that power could stand before that monster's unthinkable might.");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "PURPLE_EMERALD_LORE", EggFace( //DONE The End sealed in cyber space. "Why can i still hear it speak" hints at the speaker being a memory in cyber space and not a real person
                "<style=cMono>Welcome to the Egg-Net archives.", defaultAdjust - 13,
                "$ Combing for data relevant to", defaultAdjust,
                "$ \"Ancients and Chaos Emeralds\"...", defaultAdjust,
                "$ Results found in folder: Starfall", defaultAdjust,
                "$ Translating audio file...", defaultAdjust) + "$ Displaying result [5/7].</style>\n\n" +
                //"[The End] had followed us and brought with it the threat of destroying another world full of life. The Chaos Emeralds powered our newest weapons in this last fight against it. Even with our finest technology fueled by the emeralds, we could not match [The End]'s overwhelming power. As a last resort, we sealed it within cyber space.\n\n[The End] is far more terrifying than I imagined. It's intelligent. It's.. clever. It can speak our language. It knows who we are on a personal level, as if it has witnessed our past.\n\nHow does a being, with no purpose other than indiscriminately destroying all, know so much? What worries me even more is why it knows all this.\n\nEven managing to subdue its destructive power by trapping it in cyber space, [The End] cannot be underestimated. Even when it's powerless, that ability to understand others makes it dangerously manipulative." +translationError+"# [The End] could not be fully translated.");
                "Before now, I believed the power of the Chaos Emeralds to be limitless. That entity has made the limits of their power very clear. Our emerald-fueled weapons, with enough force to destroy the planet we stand on, seemed to do nothing against it.\n\nAs it overpowered our best efforts at destroying it, it spoke to us.\n\nIt called itself [The End]. It spoke in our language. It spoke with intelligence and conviction. It spoke of moments of our past that it should never have had a chance to witness.\n\nIt knows every one of us on a personal level. Why would a being with no purpose other than to indiscriminately destroy need to know so much? Is it just another means to an end for it? [The End] is more than just powerful. It's cunning.\n\nWith any attempts at destroying it failing, it was decided we would contain it. Cyber Space turned into [The End]'s prison, completely cut off from the outside world. No matter how great its power, it cannot tear down the walls between dimensions through brute force alone.\n\n[The End] is sealed within Cyber Space. It has no means of escaping on its own. It has no way of interacting with anything outside of Cyber Space.\n\nSo why...\n\nWhy can I still hear it speak?" + translationError + "# [The End] could not be fully translated.");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "RED_EMERALD_LORE", EggFace( //think about how previous logbook has "why can I still hear it speak?" how to transition that into this logbook where they know why. realizes that they're a koco reliving these moments of their past that have long gone
                "<style=cMono>Welcome to the Egg-Net archives.", defaultAdjust - 13,
                "$ Combing for data relevant to", defaultAdjust,
                "$ \"Ancients and Chaos Emeralds\"...", defaultAdjust,
                "$ Results found in folder: Starfall", defaultAdjust,
                "$ Translating audio file...", defaultAdjust) + "$ Displaying result [6/7].</style>\n\n" +
                //"All that remains of us are our memories. I cling to these memories of my home and my people. Even though I have long since passed away, the memory of me stored in cyberspace remains. I feel as if I'll never forget these tainted memories. Memories of our downfall repeating over and over in my head. Keeping the memories and teachings of our people known for generations, that was the foundation on which cyberspace was created. How ironic.\n\nA design as grand as cyber space was made to make sure our knowledge would be preserved and remembered, and yet I hope it is forgotten. I hope no one finds us, lest they release the very thing that reduced our civilization to this sad state.\n\nAfter everything thats happened, life moves on, with or without us. We had no place in this world to begin with.\n\nEven though we'll be long gone with time, I know the emeralds will still remain. They were our greatest treasure. They will be our parting gift to this planet. Its brilliant shine shall pierce through the chaos.");
                //"It has all come back to me now.\n\nIt feels as if all of these events were just yesterday, but I know now that's not the case. The past is long gone. This is all just a dream.\n\nNo, not a dream. To call it a dream is to imply that I am more than what I am. What remains of me is not able to dream.\n\nI am just a memory. Data stored within Cyber Space. The real me must've faded away many millenia ago.\n\nI am just a memory. A memory of my regrets, replaying over and over again. A memory of my failure to protect anyone.\n\nI was in an endless cycle of reliving my memories of the fall of our people, broken free by this sudden moment of clarity. ----moment will end----.");
                //"Looking back on it objectively, things had gone quite well. We triumphed over [The End] without bringing ruin to this new planet. With that great threat gone, the remainders of our people were able to fade away in peace.\n\nAccording to the environmental data stored in Cyber Space, thousands of years have passed since back then. It's hard for me to remember how long it's been. Minds within Cyber Space don't perceive time in the same way as normal. . All I have left is my past. The future is for the people of this planet.\n\nLife on this planet moves on, with or without us. We're leaving our our greatest treasure to them, the Chaos Emeralds. They are our parting gift to this world. Should the people of this planet ever encounter a great danger like we had, the emeralds will grant them the power to protect the planet as we would have.\n\nAs for what's left of us, we'll remain in seclusion so [The End]'s prison can never be opened. Not much different from what we've been doing. I've needed some time alone to think anyway." + translationError + "# [The End] could not be fully translated.");
                "I keep forgetting things. No... \"forgetting\" isn't the right word. These memories were never mine to begin with. It's all just environmental data being pulled into my mind. It never stays long. That outside information never meshes well with my head. My real memories end at that moment from thousands of years ago. The last time my consciousness was uploaded here into Cyber Space.\n\nLooking back on it objectively, things had gone quite well. We put a stop to [The End] without bringing destruction to this planet. With that great threat gone, the remainders of our people were able to fade away in peace.\n\nI'm... not exactly sure what happened to us after our battle with [The End]. What happened to the real me? No information was recorded by our people after the battle. All I know is that we didn't last long. If there was a danger, I'm sure there would be some hint as to what it was. I can only assume our last moments were in peace. That's the best conclusion anyone could hope for.\n\nJust escaping our home planet and stopping [The End] from attacking further has been a miracle. Every second we've had since this began has been a gift from the Chaos Emeralds. Now the Chaos Emeralds are our parting gift to this planet. Should they face more great dangers in the future, the Chaos Emeralds will protect them as they had protected us.\n\nAs for me and what's left of our civilization, we'll remain in seclusion so [The End]'s prison can never be found. Not much different from what we've been doing. I've needed some time alone to think anyway..." + translationError + "# [The End] could not be fully translated.");

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "YELLOW_EMERALD_LORE", EggFace( //DONE saving someone is what will make the speaker's koco spirit pass on. reference Sonic helping koco pass on in frontiers
                "<style=cMono>Welcome to the Egg-Net archives.", defaultAdjust - 13,
                "$ Combing for data relevant to", defaultAdjust,
                "$ \"Ancients and Chaos Emeralds\"...", defaultAdjust,
                "$ Results found in folder: Starfall", defaultAdjust,
                "$ Translating audio file...", defaultAdjust) + "$ Displaying result [7/7].</style>\n\n" +
                //"If anyone can hear this, know that I wish for nothing but your safety. There's much I'd like to share with you, but this isn't a safe place to talk. Any interaction with Cyber Space risks giving [The End] an opportunity to reach towards the outside world.\n\nIf you find anything of use from within our ruins, it is yours to take. The Chaos Emeralds saved us in our moment of crisis. Perhaps it can save you from suffering a fate like ours.\n\nMaybe this time the events in my memories will go differently. Maybe I'll be able to save someone...\n\nTo save you, whoever you may be.\n\nMaybe that will be enough for me to move on." + translationError + "# [The End] could not be fully translated.");
                "If you were able, would you go change the past? I know of our future and I am satisfied with how everything went. Even if I could return to the past, there's nothing I could do to improve the final outcome of our battle. Yet, I can't stop imagining how things could have gone differently... How I... could have done more than I did.\n\nIt's stupid. I don't know why I keep thinking about this. Going back in time is a delusion. Theoretically, the Chaos Emeralds could warp time and space, but there's no way a living being could withstand it. Only something carefully designed to interface with the emeralds' power would be able to harness their power to such an extent. It would be a dream to go back into my past, even if only to put my mind at ease. Then again, dreams are all I have left. What's the harm in forgetting the present and dreaming once again? The present is for them, not for us.\n\nMaybe this time my dreams will take a different path. Maybe someone will come to save me. Maybe I'll be able to save someone else.\n\nMaybe that will be enough for me to move on.");

            #endregion

            #endregion

            LanguageAPI.Add(HedgehogUtilsPlugin.Prefix + "LAUNCH_KEYWORD", "<style=CKeywordName>Launch</style><style=cSub>Turn the hit enemy into a projectile that <style=cIsUtility>flys in the direction hit</style> and <style=cIsDamage>deals damage</style> to other enemies it hits equal to the damage that launched it.");
        }

        /*
         * 0      /
         * 1  X\ /  O___O
         * 2   \H\L=_/ \_=
         * 3   _+X/\| | |/
         * 4      \
         */
        public const string EGGFACE_LINE_0 = "    /";
        public const string EGGFACE_LINE_1 = "X\\ /  O___O";
        public const string EGGFACE_LINE_2 = " \\H\\L=_/ \\_=";
        public const string EGGFACE_LINE_3 = " _+X/\\| | |/";
        public const string EGGFACE_LINE_4 = "    \\";
        public static string EggFace(string line0, int adjust0, string line1, int adjust1, string line2, int adjust2, string line3, int adjust3, string line4, int adjust4)
        {
            int highestLength = line0.Length + adjust0;
            string[] lines = { line0, line1, line2, line3, line4 };
            int[] adjusts = { adjust0, adjust1, adjust2, adjust3, adjust4 };
            for (int i=1;i<4;i++)
            {
                if (lines[i].Length + adjusts[i] > highestLength) { highestLength = lines[i].Length + adjusts[i]; }
            }
            StringBuilder sb = new StringBuilder();
            string[] eggFaces = { EGGFACE_LINE_0, EGGFACE_LINE_1, EGGFACE_LINE_2, EGGFACE_LINE_3, EGGFACE_LINE_4 };
            for (int face=0;face<eggFaces.Length;face++)
            {
                sb.Append(lines[face]);
                for (int i = 0; i < (highestLength - (lines[face].Length + adjusts[face])) + 1; i++)
                {
                    sb.Append(" ");
                }
                sb.Append(eggFaces[face]);
                sb.Append("\n");
            }
            return sb.ToString();
        }
    }
}