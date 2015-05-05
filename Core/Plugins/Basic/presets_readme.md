Tags: plugin
Bundle: free
Edition: free
Tagline:  Created named settings groups and reference them with ?preset=name instead of specifying them all in the URL.
Aliases: /plugins/presets

# Presets plugin (v3.1+)

Allows you to define sets of settings in Web.config and reference them by name.

Also offers a method for restricting commands to predefined presets, for the truly paranoid.

The current version only works with the URL API.

## Installation

1. Add `<add name="Presets" />` to the `<plugins />` section.

## Configuration Syntax

A named preset can specify default settings (these can be overridden in the URL), or normal settings (which override corresponding URL settings) - or both. 

    <resizer>
    ...
      <presets onlyAllowPresets="false">
        <preset name="thumb-defs" defaults="width=100;height=100" />
        <preset name="thumb" settings="width=100;height=100" />
        <preset name="thumb-width" defaults="height=100" settings="width=100" /><!-- The height can be overriden, but not the width -->
      </presets>
    ...
    </resizer>

## onlyAllowPresets

When onlyAllowPresets="true", all other querystring pairs will be stripped from the URL. 

Naturally, this will break the RemoteReader plugin if you're using the signed URLs. 

onlyAllowPresets does not apply to the managed API, only to the URL API.

## Usage syntax

Use a default settings group, then override the width:

   image.jpg?preset=thumb-defs&width=120

Referencing a normal preset

     image.jpg?preset=thumb

Referencing a preset with both default and normal settings, and overriding the default height

    image.jpg?preset=thumb-width&height=200

Referencing two presets (although they're kind of redundant in this example)

    image.jpg?preset=thumb,thumb-defs
