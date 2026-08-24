#!/usr/bin/env bash
# Syntax/type check for ProceduralTerrain scripts using Unity's own bundled Roslyn.
# This is not a full project build.
set -euo pipefail

UNITY_ROOT="${UNITY_ROOT:-$(ls -d /Applications/Unity/Hub/Editor/* 2>/dev/null | sort -V | tail -1)}"
U="$UNITY_ROOT/Unity.app/Contents"
CSC="$U/Resources/Scripting/DotNetSdkRoslyn/csc.dll"
DOTNET="$U/Resources/Scripting/NetCoreRuntime/dotnet"
MG="$U/Resources/Scripting/Managed/UnityEngine"

[ -x "$DOTNET" ] || { echo "no Unity dotnet at $DOTNET"; exit 2; }

OUT="$(mktemp -d)"
RSP="$OUT/csc.rsp"
{
  echo "-nologo"; echo "-target:library"; echo "-nostdlib"
  echo "-langversion:9.0"; echo "-out:$OUT/check.dll"
  for d in "$MG"/UnityEngine*.dll; do echo "-r:$d"; done
  # Package and project assemblies Unity has already built (TextMeshPro, uGUI, AR
  # Foundation, and Assembly-CSharp itself for types that live outside this folder).
  if [ -d Library/ScriptAssemblies ]; then
    for d in Library/ScriptAssemblies/*.dll; do
      case "$(basename "$d")" in
        Assembly-CSharp.dll|Assembly-CSharp-Editor.dll) continue ;;
      esac
      echo "-r:$d"
    done
  fi
  NETDIR="$(dirname "$(find "$U/Resources/Scripting/NetCoreRuntime" -name System.Runtime.dll | head -1)")"
  for n in System.Runtime System.Private.CoreLib System.Collections netstandard System.Runtime.Extensions; do
    [ -f "$NETDIR/$n.dll" ] && echo "-r:$NETDIR/$n.dll"
  done
  if [ $# -gt 0 ]; then printf '%s\n' "$@"
  else
    # LivingEcosystem rides along: since the PR #43 merge, EnvironmentProfile holds an
    # Ecosystem.EcosystemSettings, and both live in Assembly-CSharp — so they have to be
    find Assets/ProceduralTerrain Assets/Scripts/LivingEcosystem \
         -name '*.cs' -not -path '*/Editor/*' 2>/dev/null
    # Loose Assembly-CSharp files the terrain UI calls into. Listed rather than globbed
    # over Assets/Scripts, which would drag in Firebase/Addressables and bury real errors
    for extra in Assets/Scripts/SceneLoaderBackend.cs; do
      [ -f "$extra" ] && echo "$extra"
    done
  fi
} > "$RSP"

echo "checking $(grep -c '\.cs$' "$RSP") file(s) against $(basename "$UNITY_ROOT")"
if "$DOTNET" "$CSC" "@$RSP"; then
  echo "✅ clean"
else
  echo "❌ errors above"; exit 1
fi
