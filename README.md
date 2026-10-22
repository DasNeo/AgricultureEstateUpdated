# AgricultureEstate

## Release for a Bannerlord update

One-time setup:

1. Push the workflows and supporting files to the repository's default branch.
2. In **Settings → Actions → General → Workflow permissions**, enable **Allow GitHub Actions to create and approve pull requests**. Organization policy must permit this too. The workflow only creates PRs; it does not approve or merge them.
3. Ensure repository/organization policy permits the requested job permissions for creating branches/PRs, reporting commit statuses, and publishing tags/releases. No API keys or additional secrets are required.

Open **Actions → Prepare Bannerlord update PR → Run workflow**, select the default branch, enter a version, and run it.

The workflow creates a branch and PR that pins the exact NuGet reference version in `BannerlordVersion.props`. It then builds and tests that PR commit and attaches a **Bannerlord initial compatibility** status with a link to the logs. The PR remains open even if compilation fails. Fix any API changes manually on the PR branch, review the changes, and merge when ready. There are no AI actions or automatic source patches.

**Check Bannerlord compatibility** checks subsequent PR updates using the pinned references. Depending on GitHub's policy, a bot-created PR's automatic checks may require approval; the manual preparation workflow performs its own initial validation regardless.

Merging the generated version PR into the default branch triggers **Release merged Bannerlord update**. It checks that the PR changed `BannerlordVersion.props`, checks out the exact merge commit, builds and tests again, then publishes a GitHub release. Closing without merging does not publish. Ordinary PRs, direct pushes, and merges to other branches do not publish through this workflow. Keep the generated `codex/bannerlord-...` branch name until merging. Merge using your GitHub account; merges performed with a workflow's `GITHUB_TOKEN` do not trigger the closed-PR release event.

Accepted inputs:

- `1.4.8` or `v1.4.8`: select the highest published stable reference build for that game version.
- `1.4.8.119303`: select exactly that reference package.
- `1.5.4-beta` or `1.5.4.123627-beta`: select a beta build. The release is marked as a prerelease.

Versions must be available in [Bannerlord.ReferenceAssemblies on NuGet](https://www.nuget.org/packages/Bannerlord.ReferenceAssemblies). This targets Windows Steam/GOG/Epic game builds. Early-access `e...` packages and Xbox/Game Pass builds are not supported by this workflow.

The reference package must exist before a PR can be created. Requesting the version already pinned on the default branch stops without creating another PR. A version PR changes only the reference selection; it does not claim game compatibility until you review and test it.

Successful merged builds publish a ZIP and SHA-256 checksum. The tag points directly to the reviewed merge commit. Tags include the target version, workflow run number, and attempt number, so reruns do not overwrite existing releases. If publishing fails after the merge, rerun the failed release workflow from Actions. Build/test logs are retained as workflow artifacts.

The ZIP installs as `Modules/AgricultureEstate/` and contains all module assets plus the mod's three DLLs and debug symbols. Install Harmony and MCM v5 separately. Game assemblies, reference assemblies, and third-party mod DLLs are not bundled.

Failing builds/tests and infrastructure failures stop publication. Compilation and the existing domain/application tests do not establish in-game, Harmony patch, or save compatibility. Release notes state this limitation. GitHub actions are pinned to commit SHAs, build jobs have read-only repository permissions, and only the publishing job receives repository write permission for releases.

## Build locally

Before a version PR is merged, local builds default to the existing game/version-switcher references. Once `BannerlordVersion.props` exists, it also pins local builds to NuGet references. An explicit command-line version overrides the pinned version:

```powershell
dotnet build AgricultureEstate.sln -c Release -p:Platform=x64 -p:BannerlordVersion=1.4.8.119303
dotnet test AgricultureEstate.Tests/AgricultureEstate.Tests.csproj -c Release --no-build
```

To reproduce a tagged release, check out the tag and build with its `BannerlordVersion.props`. Reference assemblies are compile-time metadata; running the mod still requires the corresponding installed game.

On Windows, create the same release ZIP locally in a fresh `artifacts/package` directory:

```powershell
./scripts/Read-ReleaseTarget.ps1
./scripts/Build-Release.ps1 -BannerlordVersion 1.4.8.119303 -Package
```
