# Task: 083 - Stage run reset service and default state restoration
- Status: backlog

## Summary
`SessionStateSystem` currently hardcodes run reset behavior using literal baseline values for player movement, attack, health, XP, and cleanup rules. That approach will drift from profile/config-derived defaults as progression systems grow and makes restarts fragile.

## Goals
- Replace hardcoded reset literals with a single source of truth for run-scoped defaults
- Separate entity cleanup from player-state restoration
- Ensure stage starts and restarts rebuild valid run state from configuration/profile data
- Make reset behavior observable through targeted tests

## Non Goals
- Rebalancing progression numbers
- Redesigning death/game-over UX
- Introducing backward compatibility for obsolete run-state formats

## Acceptance criteria
- [ ] Session restart and new-stage reset paths do not hardcode player baseline values inside `SessionStateSystem`
- [ ] A dedicated reset service or equivalent rebuild path restores run-scoped player/session state from config/profile sources
- [ ] Enemy/orb cleanup and player stat reset responsibilities are separated clearly
- [ ] Tests cover restart/new-stage reset behavior for player stats, XP, status effects, and session counters

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Identify all run-scoped components reset during restart/new-stage flow
- Step 2: Introduce a reset/restoration service based on progression/profile/config inputs
- Step 3: Update session/state systems to call the new path and add regression tests

## Notes / Risks / Blockers
- Must preserve the distinction between hub-scoped state, slot-scoped state, and run-scoped state
- Hidden dependencies may exist in systems that currently assume restart side effects from `SessionStateSystem`
