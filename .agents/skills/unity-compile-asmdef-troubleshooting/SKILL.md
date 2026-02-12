# Unity Compile / asmdef Troubleshooting Skill

## Goal (DoD)
- Compile errors 0
- asmdef dependency graph is valid (no cycles, no missing refs)
- Platform constraints correct (Editor/Android)
- Domain asmdef has "No Engine References" honored (no UnityEngine usage)

## Procedure
1) Identify failing assembly + first error
- Read Console first error (top-most)
- Note: assembly name, file path, error code

2) asmdef sanity checks
- Check Assembly Definition References:
  - Missing reference? Add minimal needed asmdef
  - Cyclic reference? Break via Interface + Adapter
- Check "Auto Referenced" flags
- Check "No Engine References" for Domain

3) Platform constraints checks
- Verify asmdef Platforms include/exclude
- Ensure Editor-only code is in Editor assembly or Editor folder + asmdef

4) Third-party integration checks
- ThirdParty packages referenced only from Infrastructure/Legacy (not Domain)
- Wrap external libs with Port/Adapter when used from higher layers

5) Fix strategy
- Prefer: move code to correct layer > add interface > adapter
- Avoid: adding references that violate architecture (Domain -> UnityEngine, Domain -> Infra)

6) Verification checklist
- Reimport or recompile
- Enter Play Mode
- Run minimal smoke test scene
- Confirm no warnings from asmdef constraints

## Output format (IMPORTANT)
- Provide final full file contents for changed/created scripts
- Provide explicit "Added/Changed/Deleted" list
- Provide step-by-step Unity Inspector actions if needed
- Do NOT commit; user manually applies changes
