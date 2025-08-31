# Changelog

## v1.1.0

 - (Assets) Redone the design for the Chaos Emeralds. The emeralds have a new model, texture, shader, item icons, and artifact icons. The new Chaos Emerald model and shader look much better than the old one

 - (Internal) Moved the material for the Chaos Emerald interactable's ring into the Assets file so it can be reused easily
 - (Internal) Cleaned up slightly redundant InstantiateEntityState related code in BoostSkillDef
 - (Internal) Added a new overload for BoostSkillDef's DetermineNextBoostState method that lets you use a unique EntityState for air boosting
 - (Internal) Added an easy reference to the language tokens for the launch keyword and the momentum passive in Language.cs
 - (Internal) Added a Helpers.cs method that colors text to be the Super form color for Super skill overrides
 - (Internal) Added an overload to the Helpers.cs Flying method that outs an ICharacterFlightParameterProvider so you don't have to do GetComponent yourself
 - (Internal) LaunchManager's AngleAwayFromGround and AngleTowardsEnemies now returns a Vector of the same magnitude as the one it was given
 - (Internal) Added missing NetworkServer.active checks to ensure that Launches are only run on host

## v1.0.1

 - (Bug Fix) Blacklisted [Sandswept's](https://thunderstore.io/package/SandsweptTeam/Sandswept) Delta Construct from being launched to prevent framerate killing error spam on death
 
 - (Internal) The check for whether an enemy is unable to be launched has been moved into its own static method
 - (Internal) The check for whether a damagetype launch attack is able to launch a given enemy has been moved into its own static method
 - (Internal) Added a new CanBeOverridden method to FormStateBase that determines whether the form can be cancelled by trying to transform into a different form
 
### Known Issues

 - Launch projectiles' values aren't properly networked and don't update after the projectile is spawned. Things like the unique vfx of a crit launch projectile won't update to clients if the values are updated during the launch, such as if you launch a launch projectile
 - Some enemies become invisible in their death animations after being killed by a launch
 
## v1.0.0

 - Initial Release