# Changelog

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