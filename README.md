# SetProbesVRC
Set light probe coefficients at runtime in VRChat.

# What is this
Udon does not expose LightmapSettings.lightProbes.bakedProbes, so we cannot set probe coefficients at runtime.

This is a proof of concept of a system that can achieve more or less this, but within the limitations of Udon.

# How to use
- Attach the Probe Group Manager to a gameobject that also has a Light Probe Group component.
- Press the initialize button.

The Probe Group Manager script exposes a few methods you can use to set coefficients via Udon. See the Assets/SetProbesVRC/Example folder for an example.

# How does it work
- Via an editor script, I encase each light probe in a renderer with a cube mesh.
- This renderer has a shader with a custom meta pass which evaluates SH and feeds it into realtime emission color.
- I feed the SH I want into the renderers material via a few shader properties.
- With Enlighten realtime GI enabled, the probes will automatically be updated with the emission color from the meta pass.
