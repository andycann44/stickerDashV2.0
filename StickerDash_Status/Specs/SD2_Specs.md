# StickerDash 2.0 — Working Specs
_Last updated: 2025-10-01 21:14:04_

## Menus (Unity)
- **Window → Aim2Pro → Track Creator → Track Gen V2** (NL → Canonical → Build)
- **Window → Aim2Pro → Track Creator → Run Last Canonical** (executes )
- **Window → Aim2Pro → Track Creator → Track Generator** (opens Track Gen V2)

## File Layout
- Code: 
- Canonical plan: 
- Specs & notes: , 

## NL → Canonical (meters by default)
Examples map to these canonical ops:

-  → 
-  → 
-  → 
-  → 
-  → 
-  → 
-  variants:
  - number detected from text (e.g., “a couple of” → 2, “several” → 3) → 
-  →  (min 2°, max 8°, ~10-row segments)

A  line is written first to stabilize randomness.

## Builder Ops (current)
- **buildAbs(L,W)**: grid of 1 m tiles (height 0.2), names 
- **randomHoles(pct)**: removes ~pct% tiles uniformly
- **insertJumpGaps(count)**: deletes evenly spaced full rows
- **deleteRows(a,b)**, **deleteTiles(csv, row=R)**
- **curveRows(start,end,dir,deg)**: lateral curve (x-offset) across row range
- **sBend(start,end,deg)** / **sBendAuto(count,deg)**: sine offset across ranges
- **slopesRandomAuto(minDeg,maxDeg,segmentLen)**: gentle y-slope segments

## Implementation Notes
- Coordinates: rows advance +Z, columns along +X. Tile names used for row/col parsing.
- Curves & S-bends adjust **X** only; slopes adjust **Y** only.
- Meters are default if “m” omitted.
- Errors/warnings appear in Console with  prefix.

## Workflow
1. Open **Track Gen V2** → type NL prompt
2. Click **Parse → Canonical** (writes )
3. Click **Rebuild Track** or use menu **Run Last Canonical**

## Quiet Mode
- Keep chat minimal. Prefer **single-command** updates that write files & push.
- Any update must also append to this spec if it changes behavior.

## GitHub (you already set up Work seamlessly with GitHub from the command line.

USAGE
  gh <command> <subcommand> [flags]

CORE COMMANDS
  auth:          Authenticate gh and git with GitHub
  browse:        Open repositories, issues, pull requests, and more in the browser
  codespace:     Connect to and manage codespaces
  gist:          Manage gists
  issue:         Manage issues
  org:           Manage organizations
  pr:            Manage pull requests
  project:       Work with GitHub Projects.
  release:       Manage releases
  repo:          Manage repositories

GITHUB ACTIONS COMMANDS
  cache:         Manage GitHub Actions caches
  run:           View details about workflow runs
  workflow:      View details about GitHub Actions workflows

ALIAS COMMANDS
  co:            Alias for "pr checkout"

ADDITIONAL COMMANDS
  agent-task:    Work with agent tasks (preview)
  alias:         Create command shortcuts
  api:           Make an authenticated GitHub API request
  attestation:   Work with artifact attestations
  completion:    Generate shell completion scripts
  config:        Manage configuration for gh
  extension:     Manage gh extensions
  gpg-key:       Manage GPG keys
  label:         Manage labels
  preview:       Execute previews for gh features
  ruleset:       View info about repo rulesets
  search:        Search for repositories, issues, and pull requests
  secret:        Manage GitHub secrets
  ssh-key:       Manage SSH keys
  status:        Print information about relevant issues, pull requests, and notifications across repositories
  variable:      Manage GitHub Actions variables

HELP TOPICS
  accessibility: Learn about GitHub CLI's accessibility experiences
  actions:       Learn about working with GitHub Actions
  environment:   Environment variables that can be used with gh
  exit-codes:    Exit codes used by gh
  formatting:    Formatting options for JSON data exported from gh
  mintty:        Information about using gh with MinTTY
  reference:     A comprehensive reference of all gh commands

FLAGS
  --help      Show help for command
  --version   Show gh version

EXAMPLES
  $ gh issue create
  $ gh repo clone cli/cli
  $ gh pr checkout 321

LEARN MORE
  Use `gh <command> <subcommand> --help` for more information about a command.
  Read the manual at https://cli.github.com/manual
  Learn about exit codes using `gh help exit-codes`
  Learn about accessibility experiences using `gh help accessibility`)
- Direct push: [main 86cb484] msg
 1 file changed, 3 insertions(+), 1 deletion(-)
- Branch+PR:
  On branch feat/x
nothing to commit, working tree clean
branch 'feat/x' set up to track 'origin/feat/x'.

